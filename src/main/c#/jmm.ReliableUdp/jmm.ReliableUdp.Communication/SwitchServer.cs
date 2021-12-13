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
  /// Listens for incoming connection requests. Runs synchronously. On receiving a message: If there is already an active connection, ignores the request. If the connection is new, sends SWITCH message to meet on another port (default chooses any available port in the application environment). Will continue to sent SWITCH statements until an ACK is received.
  /// </summary>
  public class SwitchServer
  {
    private SwitchServerOptions options;
    private IPEndPoint listenEp;
    private UdpClient udpClient;
    private int listenPort;
    private bool running;
    private Dictionary<IPEndPoint, Channel> channels;
    private Dictionary<IPEndPoint, Responder> responders;

    public event EventHandler<MsgArgs> DatagramReceived;

    public SwitchServer(int port)
    {
      listenEp = new IPEndPoint(IPAddress.Any, 0);
      listenPort = port;
      running = false;
      options = SwitchServerOptions.CreateDefault();
    }

    public void Start()
    {
      if (running)
        return;
      running = true;
      udpClient = new UdpClient(listenPort);
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
      if (channels.ContainsKey(remoteEp))
        return;

      if (Messages.IsConnectionRequest(payload))
      {
        if (responders.TryGetValue(remoteEp, out Responder responder))
        {
          responder.ResetTimer();
        }
        else
        {
          responder = new Responder(remoteEp, options);
          responders.Add(remoteEp, responder);
          Task.Run(() => responder.SendRetries(() => responders.Remove(remoteEp)));
        }
      }
      else if (Messages.IsSwitchAck(payload))
      {
        if (responders.TryGetValue(remoteEp, out Responder responder))
        {
          responder.Close();
          responders.Remove(remoteEp);
          // TODO Add the new channel
        }
      }
    }

    public void Stop()
    {
      running = false;
      udpClient.Close();
    }

    private class Responder
    {
      internal UdpClient UdpConnection { get; private set; }

      private int localPort;
      private int retryNumber;
      private IPEndPoint remoteEp;
      private SwitchServerOptions options;
      private bool running;
      private byte[] sendPayload;
      private Thread sleepyThread;

      internal Responder(IPEndPoint remoteEp, SwitchServerOptions options)
      {
        this.remoteEp = remoteEp;
        this.options = options;
        UdpConnection = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        localPort = ((IPEndPoint)UdpConnection.Client.LocalEndPoint).Port;
        retryNumber = 0;
        running = true;
        sendPayload = Messages.SwitchToPortPayload((ushort)localPort);
      }

      internal void SendRetries(Action completeCallback)
      {
        sleepyThread = Thread.CurrentThread;
        while (retryNumber < options.MaxRetries)
        {
          if (!running)
            return;
          if (retryNumber > options.MaxRetries)
            return;
          UdpConnection.Send(sendPayload, sendPayload.Length, remoteEp);
          Thread.Sleep((int)options.RetryWaitTime);
        }
        completeCallback?.Invoke();
      }

      internal void Close()
      {
        running = false;
        sleepyThread.Interrupt();
      }

      internal void ResetTimer()
      {
        retryNumber = 0;
      }
    }
  }
}
