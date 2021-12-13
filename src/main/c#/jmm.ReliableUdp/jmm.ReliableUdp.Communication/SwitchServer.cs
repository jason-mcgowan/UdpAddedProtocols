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
    private int localPort;
    private bool running;
    private Dictionary<IPEndPoint, Channel> channels = new Dictionary<IPEndPoint, Channel>();
    private Dictionary<IPEndPoint, Responder> responders = new Dictionary<IPEndPoint, Responder>();

    public SwitchServer(int port)
    {
      options = SwitchServerOptions.CreateDefault();
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
        if (responders.TryGetValue(remoteEp, out Responder responder))
        {
          responder.ResetTimer();
        }
        else
        {
          if (channels.ContainsKey(remoteEp))
            return;

          UdpClient switchConn = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
          ushort switchPort = (ushort)((IPEndPoint)switchConn.Client.LocalEndPoint).Port;
          byte[] sendPayload = Messages.SwitchToPortPayload(switchPort);
          channels.Add(remoteEp, new Channel(remoteEp)); // todo get channel up and running

          responder = new Responder(udpClient, remoteEp, options, sendPayload);
          responders.Add(remoteEp, responder);
          Task.Run(() => responder.SendRetries(() => OnResponseRetriesComplete(remoteEp)));
        }
      }
      else if (Messages.IsSwitchAck(payload))
      {
        if (responders.TryGetValue(remoteEp, out Responder responder))
        {
          responder.Close();
          responders.Remove(remoteEp);
        }
      }
    }

    private void OnResponseRetriesComplete(IPEndPoint remoteEp)
    {
      channels.Remove(remoteEp);
      responders.Remove(remoteEp);
    }

    public void Stop()
    {
      running = false;
      udpClient.Close();
    }

    private class Responder
    {
      UdpClient xmitClient;
      private int retryNumber;
      private IPEndPoint remoteEp;
      private SwitchServerOptions options;
      private bool running;
      private byte[] payload;
      private Thread sleepyThread;

      internal Responder(UdpClient xmitClient, IPEndPoint remoteEp, SwitchServerOptions options, byte[] payload)
      {
        this.xmitClient = xmitClient;
        this.remoteEp = remoteEp;
        this.options = options;
        this.payload = payload;
        retryNumber = 0;
        running = true;
      }

      internal void SendRetries(Action timeoutCallback)
      {
        sleepyThread = Thread.CurrentThread;
        while (retryNumber <= options.MaxRetries)
        {
          if (!running)
            return;
          xmitClient.Send(payload, payload.Length, remoteEp);
          retryNumber++;
          try
          {
            Thread.Sleep((int)options.RetryWaitTime);
          }
          catch (ThreadInterruptedException e)
          {
            if (running)
              throw e;
          }
        }
        if (!running)
          return;

        Close();
        timeoutCallback?.Invoke();
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
