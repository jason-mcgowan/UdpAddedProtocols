using System;

namespace jmm.ReliableUdp.Communication
{
  /// <summary>
  /// Contains a 64-bit field as <see cref="System.UInt64"/> for fast bitwise operations to track sequence message acknowledgements
  /// </summary>
  /// <remarks>
  /// Implementing class must track the acceptable ranges of sequences and ack bitfields. The underlying mechanics cycle through the sequence numbers modulo 64. If a sequence ack is presented for sequence number 128 with its 32 previous ack messages, and another is presented for sequence 96 with another 32 previous messages, the second sequence will overwrite a portion of the buffer for the first sequence.
  /// </remarks>
  public class SequenceAckBuffer64
  {
    private const int LONG_BITS = 64;
    private const int INT32_BITS = 32;
    private int _bufferWordCount;
    private ulong _ackBits;
    private object _ackBitsLock;

    public SequenceAckBuffer64(int bufferWordCount = 1)
    {
      if (bufferWordCount < 1)
      {
        throw new ArgumentOutOfRangeException();
      }

      _bufferWordCount = bufferWordCount;
      _ackBits = ulong.MaxValue;
    }

    /// <summary>
    /// Sets the corresponding sequence acknowledge buffer bit to 0 (not acknowledged)
    /// </summary>
    public void SequenceUnack(ulong seq)
    {
      lock (_ackBitsLock)
      {
        _ackBits &= ~((ulong)1 << (int)(seq % LONG_BITS));
      }
    }

    /// <summary>
    /// Sets the sequence buffer ack bit to 0, returns true if it was already 0 (was not acknowledged)
    /// </summary>
    /// <returns>True if the overriden bit was 0 (meaning prior sequence was not acknowledged)</returns>
    public bool SequenceUnackCheckUnacked(ulong seq)
    {
      ulong mask = (ulong)1 << (int)(seq % LONG_BITS);
      bool result;
      lock (_ackBitsLock)
      {
        result = (mask & _ackBits) == 0;
        _ackBits &= ~mask;
      }
      return result;
    }

    /// <summary>
    /// Given the sequence number, adds any acknowledge messages to the bit field.
    /// </summary>
    /// <param name="seq">The most recently acknowledged sequence number</param>
    /// <param name="seqAckBits">The bitmask of acknowledged prior messages where the most significant bit is the immediately previous sequence number, while the least significant bit is the message of sequence number-32</param>
    /// <returns></returns>
    public ulong Acknowledge(ulong seq, uint seqAckBits)
    {
      ulong mask = BuildAckMask(seq, seqAckBits);
      ulong result;
      lock (_ackBitsLock)
      {
        result = ~_ackBits & mask;
        _ackBits ^= mask;
      }
      return result;
    }

    private static ulong BuildAckMask(ulong seq, uint seqAckBits)
    {
      // Ack the sequence number itself with the first bits up to the buffer size and the remaining bits wrapped around
      int seqInd = (int)(seq % LONG_BITS);
      return
        ((ulong)1 << seqInd) |
        ((ulong)seqAckBits >> (INT32_BITS - seqInd)) |
        ((ulong)seqAckBits << (INT32_BITS + seqInd));
    }
  }
}
