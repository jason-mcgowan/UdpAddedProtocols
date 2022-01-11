using System;
using System.Collections.Generic;
using System.Threading;

namespace jmm.ReliableUdp.Communication
{
  public class AckTrackerUInt32
  {
    private Bitfield64 sentSeqAcks;
    private ReaderWriterLockSlim acksLock = new ReaderWriterLockSlim();

    public AckTrackerUInt32(int minBitCount = 64)
    {
      int wordCount = MinBitCountToWordCount(minBitCount);
      sentSeqAcks = new Bitfield64(wordCount, false);
    }

    private int MinBitCountToWordCount(int minBitCount)
    {
      // Get the integer division ceiling for number of 64-bit words
      int minWordCount = (minBitCount + 64 - 1) / 64;
      // Now get the power of 2 (inspired by: https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2)
      if (minWordCount == 0)
        return 0;

      // Decrement by 1 so that we don't round up if already a power of 2
      minWordCount--;
      // We are now guaranteed the bit (msb-1) is a 1. We OR with itself as shifting right until all bits <msb are 1
      minWordCount |= minWordCount >> 1;
      minWordCount |= minWordCount >> 2;
      minWordCount |= minWordCount >> 4;
      minWordCount |= minWordCount >> 8;
      minWordCount |= minWordCount >> 16;
      // Add 1 back to get the power of 2. This is checked to force an exception on overflow.
      return checked(++minWordCount);
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


    public ICollection<uint> ReceiveAcks(uint seq, uint ackBits)
    {
      uint newAcks;
      acksLock.EnterUpgradeableReadLock();
      try
      {
        uint currAcks = (uint)sentSeqAcks.GetBits(seq);
        // Flip the current ack bits so 0 is now 1, AND with the new acks, so any 1s are newly-acknowledged
        newAcks = ~currAcks & ackBits;
        if (newAcks == 0)
        {
          return null;
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

      return GetNewAcks(seq, ref newAcks);
    }

    private static ICollection<uint> GetNewAcks(uint seq, ref uint newAcks)
    {
      ICollection<uint> result = new LinkedList<uint>();
      // Typically the lower bits will be 0, so we save some compute cycles by reading the highest bit, shifting, then checking for 0
      uint msb = (uint)1 << 31;
      uint i = 31;
      while (newAcks != 0)
      {
        if ((newAcks & msb) != 0)
        {
          result.Add(seq + i--);
        }
        newAcks <<= 1;
      }
      return result;
    }

    public override string ToString()
    {
      return sentSeqAcks.ToString();
    }
  }
}
