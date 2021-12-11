using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace jmm.ReliableUdp.Communication
{
  public class MsgArgs : EventArgs
  {
    public IPAddress Address { get; }
    public int Port { get; }
    public byte[] Payload { get; }

    public MsgArgs(IPAddress address, int port, byte[] message)
    {
      Address = address;
      Port = port;
      Payload = message;
    }
  }
}
