using jmm.ReliableUdp.Protocol;
using System;

namespace jmm.ReliableUdp.Communication
{
  public class MsgArgs : EventArgs
  {
    public HeaderFlag Flags { get; private set; }
    public int Id { get; private set; }
    public byte[] Dgram { get; private set; }

    public MsgArgs(HeaderFlag flags, int id, byte[] payload)
    {
      Flags = flags;
      Id = id;
      Dgram = payload;
    }
  }
}
