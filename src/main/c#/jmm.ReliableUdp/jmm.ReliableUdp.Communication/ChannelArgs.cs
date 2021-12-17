using System;
using System.Collections.Generic;
using System.Text;

namespace jmm.ReliableUdp.Communication
{
  public class ChannelArgs
  {
    public Channel ConnectedChannel { get; private set; }

    public ChannelArgs(Channel connectedChannel)
    {
      ConnectedChannel = connectedChannel;
    }
  }
}
