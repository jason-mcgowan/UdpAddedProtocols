using jmm.ReliableUdp.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace jmm.ReliableUdp.Communication
{
  public class Channel
  {
    public UdpClient Client { get; private set; }
    public RetryOptions RetryOpt { get; set; }

    public event EventHandler<MsgArgs> MessageReceived;

    private Dictionary<int, Action> ackCbs = new Dictionary<int, Action>();
    private Dictionary<int, Action> failCbs = new Dictionary<int, Action>();
    private Dictionary<int, RetrySender> retrySenders = new Dictionary<int, RetrySender>();
    private IPEndPoint remoteEp;
    private bool running;
    private Thread listenThread;

    public Channel(UdpClient udpClient, IPEndPoint remoteEp)
    {
      Client = udpClient;
      RetryOpt = RetryOptions.CreateDefault();
      this.remoteEp = remoteEp;
      running = false;
    }

    public void Start()
    {
      lock (this)
      {
        if (running == true)
          return;
        running = true;
      }

      Task.Run(() => Listen());
    }

    public void Stop()
    {
      running = false;
      listenThread.Abort();
    }

    public void SendRetries(int id, byte[] payload, Action ackCb, Action failCb)
    {
      ackCbs.Add(id, ackCb);
      failCbs.Add(id, failCb);

      byte[] dgram = new byte[payload.Length + 3];
      dgram[0] = (byte)HeaderFlag.ACK_REQUEST;
      Array.Copy(BitConverter.GetBytes(id), 0, dgram, 1, 2);
      Array.Copy(payload, 0, dgram, 3, payload.Length);

      RetrySender rs = new RetrySender(Client, remoteEp, RetryOpt, dgram);
      retrySenders.Add(id, rs);
      rs.SendRetries(() => OnRetriesTimeout(id));
    }

    private void Listen()
    {
      listenThread = Thread.CurrentThread;
      while (running)
      {
        byte[] payload;
        try
        {
          payload = Client.Receive(ref remoteEp);
          HandleMessage(payload);
        }
        catch (Exception e)
        {
          if (!running)
            return;
          throw e;
        }
      }
    }

    private void HandleMessage(byte[] payload)
    {
      if (payload.Length < 3)
      {
        // FUTURE: some kind of logging or event
        return;
      }
      int id = BitConverter.ToInt32(payload, 1);
      HeaderFlag flags = (HeaderFlag)payload[0];
      if (flags.HasFlag(HeaderFlag.ACK_RESPONSE))
      {
        // Check if we are waiting for the response, then complete it
        if (!ackCbs.TryGetValue(id, out Action ackCb))
          return;
        StopTracking(id);
        Task.Run(ackCb);
        return;
      }
      else if (flags.HasFlag(HeaderFlag.ACK_REQUEST))
      {
        SendAck(id);
      }
      MsgArgs msgArgs = new MsgArgs(flags, id, payload);
      Task.Run(() => MessageReceived?.Invoke(this, msgArgs));
    }

    private void StopTracking(int id)
    {
      ackCbs.Remove(id);
      failCbs.Remove(id);
      retrySenders.Remove(id);
    }

    private void SendAck(int id)
    {
      byte[] payload = new byte[3];
      payload[0] = (byte)HeaderFlag.ACK_RESPONSE;
      Array.Copy(BitConverter.GetBytes(id), 0, payload, 1, 2);
      Client.Send(payload, payload.Length, remoteEp);
    }
    private void OnRetriesTimeout(int id)
    {
      if (!failCbs.TryGetValue(id, out Action failCb))
        return;

      StopTracking(id);
      failCb?.Invoke();
    }
  }
}
