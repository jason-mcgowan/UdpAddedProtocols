using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace jmm.ReliableUdp.Communication
{
  public class Channel
  {
    private IPEndPoint remoteEp;
    public int RTT { get; private set; }


    public Channel(IPEndPoint remoteEp)
    {
      this.remoteEp = remoteEp;
    }

    public void Handle(byte[] payload)
    {
      // todo
    }
  }
}
