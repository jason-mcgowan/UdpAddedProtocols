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
    /// <summary>
    /// Underlying <see cref="UdpClient"/> used for sending and receiving
    /// </summary>
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

    /// <summary>
    /// Listens on a background thread for messages and invokes the <see cref="MessageReceived"/> event on new messages.
    /// </summary>
    /// <remarks>
    /// Automatically replies to any messages requesting acknowledgement, and stops auto-resending messages which are acknowledged
    /// </remarks>
    public void Start()
    {
      if (running)
        return;
      running = true;
      // FUTURE: Change the scheme to run on an external foreground thread
      Task.Run(() => Listen());
    }

    /// <summary>
    /// Halts listening
    /// </summary>
    public void Stop()
    {
      running = false;
      listenThread.Abort();
      // FUTURE: Change to another listening cancellation method. Stop outgoing retry senders. Maybe close the udpclient?
    }

    /// <summary>
    /// Prepends 3 bytes (header+id) to the payload and sends with acknowledge requested, auto-resend per options.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="payload"></param>
    /// <param name="ackCb">Action to take on acknowledge received</param>
    /// <param name="failCb">Action to take when retries complete without acknowledgement</param>
    public void SendRetries(short id, byte[] payload, Action ackCb, Action failCb)
    {
      ackCbs.Add(id, ackCb);
      failCbs.Add(id, failCb);

      byte[] dgram = new byte[payload.Length + 3];
      dgram[0] = (byte)HeaderFlag.ACK_REQUEST;
      Array.Copy(BitConverter.GetBytes(id), 0, dgram, 1, 2);
      Array.Copy(payload, 0, dgram, 3, payload.Length);

      RetrySender rs = new RetrySender(Client, remoteEp, RetryOpt, dgram);
      retrySenders.Add(id, rs);
      rs.SendRetriesAsync(() => OnRetriesTimeout(id));
    }

    /// <summary>
    /// Sends a raw datagram, no header or id, so no acknowledgement is requested or retries enabled.
    /// </summary>
    /// <param name="dgram"></param>
    public void SendDgram(byte[] dgram)
    {
      Client.Send(dgram, dgram.Length, remoteEp);
    }

    private void Listen()
    {
      listenThread = Thread.CurrentThread;
      while (running)
      {
        byte[] dgram;
        try
        {
          dgram = Client.Receive(ref remoteEp);
          HandleMessage(dgram);
        }
        catch (Exception e)
        {
          if (!running)
            return;
          throw e;
        }
      }
    }

    private void HandleMessage(byte[] dgram)
    {
      if (dgram.Length < 3)
      {
        // FUTURE: some kind of logging or event
        return;
      }
      HeaderFlag flags = (HeaderFlag)dgram[0];
      short id = BitConverter.ToInt16(dgram, 1);
      if (flags.HasFlag(HeaderFlag.ACK_RESPONSE))
      {
        // Check if we are waiting for the response, then complete it
        if (!ackCbs.TryGetValue(id, out Action ackCb))
          return;
        retrySenders[id].Close();
        RemoveTrackers(id);
        Task.Run(ackCb);
        return;
      }
      else if (flags.HasFlag(HeaderFlag.ACK_REQUEST))
      {
        SendAck(id);
      }
      MsgArgs msgArgs = new MsgArgs(flags, id, dgram);
      Task.Run(() => MessageReceived?.Invoke(this, msgArgs));
    }

    private void RemoveTrackers(int id)
    {
      ackCbs.Remove(id);
      failCbs.Remove(id);
      retrySenders.Remove(id);
    }

    private void SendAck(short id)
    {
      byte[] dgram = new byte[3];
      dgram[0] = (byte)HeaderFlag.ACK_RESPONSE;
      Array.Copy(BitConverter.GetBytes(id), 0, dgram, 1, 2);
      Client.Send(dgram, dgram.Length, remoteEp);
    }
    private void OnRetriesTimeout(int id)
    {
      if (!failCbs.TryGetValue(id, out Action failCb))
        return;

      RemoveTrackers(id);
      failCb?.Invoke();
    }
  }
}
