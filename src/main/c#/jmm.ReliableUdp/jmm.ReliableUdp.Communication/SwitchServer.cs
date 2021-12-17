using jmm.ReliableUdp.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace jmm.ReliableUdp.Communication
{
  /// <summary>
  /// Listens for requests and sets up multiple local 2-way UDP channels with remote SwitchClients.
  /// </summary>
  public class SwitchServer
  {
    public event EventHandler<ChannelArgs> ConnectionEstablished;    

    public RetryOptions RetryOpts { get; set; }

    private IPEndPoint listenEp;
    private UdpClient udpClient;
    private int localPort;
    private bool running;
    private Dictionary<IPEndPoint, Channel> channels = new Dictionary<IPEndPoint, Channel>();
    private Dictionary<IPEndPoint, RetrySender> retrySenders = new Dictionary<IPEndPoint, RetrySender>();

    public SwitchServer(int port)
    {
      RetryOpts = RetryOptions.CreateDefault();
      listenEp = new IPEndPoint(IPAddress.Any, 0);
      localPort = port;
      running = false;
    }

    /// <summary>
    /// Blocks while receiving connection requests. Recommend running in a foreground <see cref="Thread"/>. Use <see cref="Stop"/> to stop.
    /// </summary>
    /// <remarks>
    /// Note, this routine will create <see cref="ThreadPool"/> <see cref="Task"/> for each connection exchange. These live your Route Transit Time (50-1000ms typical over the internet).<br/>
    /// Modify <see cref="RetryOpts"/> to change retry attempts and timings.<br/>
    /// Listen to <see cref="ConnectionEstablished"/> to invoke when a fully switched connection is established.
    /// </remarks>
    public void Start()
    {
      if (running)
        return;
      running = true;
      udpClient = new UdpClient(localPort);
      while (running)
      {
        byte[] payload;
        IPEndPoint remoteEp;
        try
        {
          payload = udpClient.Receive(ref listenEp);
          remoteEp = new IPEndPoint(listenEp.Address, listenEp.Port);
          HandleMessage(payload, remoteEp);
        }
        catch (SocketException e)
        {
          if (!running)
            return;
          throw e;
        }
      }
    }

    /// <summary>
    /// Closes the underlying <see cref="UdpClient"/> and halts listening.
    /// </summary>
    public void Stop()
    {
      running = false;
      udpClient.Close();
    }

    private void HandleMessage(byte[] payload, IPEndPoint remoteEp)
    {
      if (Messages.IsConnectionRequest(payload))
      {
        if (retrySenders.TryGetValue(remoteEp, out RetrySender retrySender))
        {
          retrySender.ResetRetryCount();
        }
        else
        {
          if (channels.ContainsKey(remoteEp))
            return;

          UdpClient switchConn = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
          ushort switchPort = (ushort)((IPEndPoint)switchConn.Client.LocalEndPoint).Port;
          byte[] sendPayload = Messages.SwitchToPortPayload(switchPort);
          channels.Add(remoteEp, new Channel(switchConn, remoteEp));

          retrySender = new RetrySender(udpClient, remoteEp, RetryOpts, sendPayload);
          retrySenders.Add(remoteEp, retrySender);
          Task.Run(() => retrySender.SendRetriesAsync(() => OnResponseRetriesComplete(remoteEp)));
        }
      }
      else if (Messages.IsSwitchAck(payload))
      {
        if (retrySenders.TryGetValue(remoteEp, out RetrySender responder))
        {
          responder.Close();
          retrySenders.Remove(remoteEp);
          Channel connectedChannel = channels[remoteEp];
          connectedChannel.Start();
          Task.Run(() => ConnectionEstablished?.Invoke(this, new ChannelArgs(connectedChannel)));
        }
      }
    }

    private void OnResponseRetriesComplete(IPEndPoint remoteEp)
    {
      channels.Remove(remoteEp);
      retrySenders.Remove(remoteEp);
    }
  }
}
