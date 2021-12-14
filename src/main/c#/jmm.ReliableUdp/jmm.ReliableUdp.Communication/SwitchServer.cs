using jmm.ReliableUdp.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace jmm.ReliableUdp.Communication
{
  /// <summary>
  /// Listens for incoming connection requests. Runs synchronously. On receiving a message: If there is already an active connection, ignores the request. If the connection is new, sends SWITCH message to meet on another port (default chooses any available port in the application environment). Will continue to send SWITCH statements until an ACK is received or retry limit reached.
  /// </summary>
  public class SwitchServer
  {
    private RetryOptions options;
    private IPEndPoint listenEp;
    private UdpClient udpClient;
    private int localPort;
    private bool running;
    private Dictionary<IPEndPoint, Channel> channels = new Dictionary<IPEndPoint, Channel>();
    private Dictionary<IPEndPoint, RetrySender> retrySenders = new Dictionary<IPEndPoint, RetrySender>();

    public SwitchServer(int port)
    {
      options = RetryOptions.CreateDefault();
      listenEp = new IPEndPoint(IPAddress.Any, 0);
      localPort = port;
      running = false;
    }

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

    private void HandleMessage(byte[] payload, IPEndPoint remoteEp)
    {
      if (Messages.IsConnectionRequest(payload))
      {
        if (retrySenders.TryGetValue(remoteEp, out RetrySender retrySender))
        {
          retrySender.ResetTimer();
        }
        else
        {
          if (channels.ContainsKey(remoteEp))
            return;

          UdpClient switchConn = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
          ushort switchPort = (ushort)((IPEndPoint)switchConn.Client.LocalEndPoint).Port;
          byte[] sendPayload = Messages.SwitchToPortPayload(switchPort);
          channels.Add(remoteEp, new Channel(switchConn, remoteEp));

          retrySender = new RetrySender(udpClient, remoteEp, options, sendPayload);
          retrySenders.Add(remoteEp, retrySender);
          Task.Run(() => retrySender.SendRetries(() => OnResponseRetriesComplete(remoteEp)));
        }
      }
      else if (Messages.IsSwitchAck(payload))
      {
        if (retrySenders.TryGetValue(remoteEp, out RetrySender responder))
        {
          responder.Close();
          retrySenders.Remove(remoteEp);
        }
      }
    }

    private void OnResponseRetriesComplete(IPEndPoint remoteEp)
    {
      channels.Remove(remoteEp);
      retrySenders.Remove(remoteEp);
    }

    public void Stop()
    {
      running = false;
      udpClient.Close();
    }
  }
}
