using jmm.ReliableUdp.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jmm.ReliableUdp.Communication
{
public  class SwitchClient
  {
    public event EventHandler EstablishedConnection;
    public event EventHandler FailedConnection;

    public Channel SwitchChannel { get; private set; }

    private IPEndPoint serverEp;
    private bool receivedSwitch;
    private RetryOptions retryOptions;
    private UdpClient client;
    private Channel handshakeChannel;

    public SwitchClient(IPEndPoint serverEp, int localPort)
    {
      this.serverEp = serverEp;
      retryOptions = RetryOptions.CreateDefault();
      client = new UdpClient(localPort);
      handshakeChannel = new Channel(client, serverEp);
      handshakeChannel.MessageReceived += OnMessageReceived;
    }

    public void Connect()
    {
      handshakeChannel.Start();
      receivedSwitch = false;
      Console.WriteLine("Starting handshakes");
      Task.Run(() => SendHandshakes());
    }

    public void Stop()
    {
      handshakeChannel.MessageReceived -= OnMessageReceived;
    }

    private void SendHandshakes()
    {
      int retry = 0;
      while (!receivedSwitch && retry++ <= retryOptions.MaxRetries)
      {
        Console.WriteLine("Sending handshake request " + retry);
        handshakeChannel.SendDgram(Messages.CONNECT_HANDSHAKE_PAYLOAD);
        Thread.Sleep((int)retryOptions.RetryWaitTime);
      }
      if (!receivedSwitch)
      {
        FailedConnection?.Invoke(this, EventArgs.Empty);
      }
    }

    private void OnMessageReceived(object sender, MsgArgs e)
    {
      byte[] dgram = e.Dgram;
      ushort port = Messages.GetSwitchPort(dgram);
      if (port == 0)
        return;      

      if (!receivedSwitch)
      {
        receivedSwitch = true;
        IPEndPoint switchEp = new IPEndPoint(serverEp.Address, port);
        SwitchChannel = new Channel(client, switchEp);
        EstablishedConnection?.Invoke(this, EventArgs.Empty);
      }
      handshakeChannel.SendDgram(Messages.SWITCH_ACK_PAYLOAD);
    }
  }
}
