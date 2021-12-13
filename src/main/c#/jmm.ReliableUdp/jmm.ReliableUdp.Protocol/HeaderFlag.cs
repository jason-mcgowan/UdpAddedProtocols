using System;

namespace jmm.ReliableUdp.Protocol
{
  [Flags]
  public enum HeaderFlag
  {
    ACK_REQUEST = 1,
    ACK_RESPONSE = 2,
    MESSAGE = 4
  }
}
