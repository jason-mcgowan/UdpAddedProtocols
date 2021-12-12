using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace jmm.ReliableUdp.Communication
{
  public class Bus
  {

    private Listener listener;
    private int port;
    private ConcurrentDictionary<IPEndPoint, Channel> channels;

    public Bus(int port)
    {
      this.port = port;
      listener = new Listener(port);
    }

    public void Start()
    {
      listener.DatagramReceived += OnDatagramReceived;
    }

    public void Stop()
    {
      listener.DatagramReceived -= OnDatagramReceived;
    }

    private void OnDatagramReceived(object sender, MsgArgs msgArgs)
    {
      //todo
      // If no channel open, start one, and send ack
      // if channel open, send to channel and return
      IPEndPoint ep = new IPEndPoint(msgArgs.Address, msgArgs.Port);
      Channel channel = channels.GetOrAdd(ep, new Channel(ep));
      Task.Run(()=> channel.Handle(msgArgs.Payload));      
    }
  }
}
