using System;
using System.Collections.Generic;
using System.Threading;

namespace jmm.ReliableUdp.Communication
{
  public class AckTrackerUInt32
  {
    private Bitfield64 sentSeqAcks;
    private ReaderWriterLockSlim acksLock = new ReaderWriterLockSlim();

    public AckTrackerUInt32(int wordCountLg2 = 0)
    {
      sentSeqAcks = new Bitfield64(wordCountLg2, false);
    }    

    public void SequenceSent(uint seq)
    {
      acksLock.EnterWriteLock();
      try
      {
        sentSeqAcks.ZeroBit(seq);
      }
      finally
      {
        acksLock.ExitWriteLock();
      }
    }

    public void ReceiveAcks(uint seq, uint ackBits, Action<uint> onNewAck)
    {
      uint newAcks;
      acksLock.EnterUpgradeableReadLock();
      try
      {
        uint currAcks = (uint)sentSeqAcks.GetBits(seq);
        // Flip the current ack bits so 0 is now 1, AND with the new acks, so any 1s are newly-acknowledged
        newAcks = ~currAcks & ackBits;
        if (newAcks == 0u)
        {
          return;
        }

        acksLock.EnterWriteLock();
        try
        {
          sentSeqAcks.Or(seq, ackBits);
        }
        finally
        {
          acksLock.ExitWriteLock();
        }
      }
      finally
      {
        acksLock.ExitUpgradeableReadLock();
      }

      GetNewAcks(seq, ref newAcks, onNewAck);
    }

    private static void GetNewAcks(uint seq, ref uint newAcks, Action<uint> newAckCallback)
    {
      // Typically the lower bits will be 0, so we save some compute cycles by reading the highest bit, shifting, then checking for 0
      uint msb = 1u << 31;
      uint i = 31u;
      while (newAcks != 0u)
      {
        if ((newAcks & msb) != 0u)
        {
          newAckCallback?.Invoke(seq + i--);
        }
        newAcks <<= 1;
      }
    }

    public override string ToString()
    {
      return sentSeqAcks.ToString();
    }
  }
}
