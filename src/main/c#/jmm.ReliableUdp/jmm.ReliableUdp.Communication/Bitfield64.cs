using System;
using System.Text;

namespace jmm.ReliableUdp.Communication
{
  /// <summary>
  /// Contains an array of "words" of type <see cref="System.UInt64"/> for various bitwise operations.
  /// </summary>
  public class Bitfield64
  {
    private const int WORD_LENGTH = 64;

    public ulong BitLength { get; private set; }
    public int WordCount { get; private set; }

    private ulong[] words;

    /// <summary>
    /// Creates a buffer with underlying array of <see cref="System.UInt64"/> words as bitfields.
    /// </summary>
    /// <param name="wordCount">The number of 64-bit words</param>
    /// <param name="initVal">Initialize all bits to 1 (true) or 0 (false)</param>
    public Bitfield64(int wordCount = 1, bool initVal = true)
    {
      if (wordCount < 1)
      {
        throw new ArgumentOutOfRangeException("Word count must be >= 1");
      }

      WordCount = wordCount;
      words = new ulong[wordCount];
      BitLength = (ulong)(WORD_LENGTH * wordCount);

      if (!initVal)
      {
        return;
      }
      for (int i = 0; i < words.Length; i++)
      {
        words[i] = ~(ulong)0;
      }
    }

    /// <summary>
    /// Sets the bit to 0
    /// </summary>
    public void ZeroBit(ulong position)
    {
      // Find where it falls in the buffer, which word that corresponds to, and which index in that word
      WordIndices wi = new WordIndices(position, BitLength);

      // Line up a 1 with the index in the word, flip the bits so now there's a single 0, store the AND
      words[wi.lowWordInd] &= ~((ulong)1 << wi.bitShiftAmount);
    }

    public void Or(ulong lsbPos, ulong bits)
    {
      WordIndices wi = new WordIndices(lsbPos, BitLength);
      words[wi.lowWordInd] |= bits << wi.bitShiftAmount;
      words[wi.highWordInd] |= bits >> (WORD_LENGTH - wi.bitShiftAmount);
    }

    /// <summary>
    /// Returns the 64-bits starting at and including the provided least significant bit position
    /// </summary>
    public ulong GetBits(ulong lsbPos)
    {
      return GetBits(new WordIndices(lsbPos, BitLength));
    }

    private ulong GetBits(WordIndices wi)
    {
      ulong lowBits = words[wi.lowWordInd] >> wi.bitShiftAmount;
      ulong highBits = words[wi.highWordInd] << (WORD_LENGTH - wi.bitShiftAmount);
      return lowBits | highBits;
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      for (int i = words.Length - 1; i >= 0; i--)
      {
        sb.Append(Convert.ToString((long)words[i], 2).PadLeft(WORD_LENGTH, '0'));
      }

      return sb.ToString();
    }

    private struct WordIndices
    {
      public int lowWordInd;
      public int highWordInd;
      public int bitShiftAmount;

      public WordIndices(ulong position, ulong bitLength)
      {
        ulong lowLetterInd = (position % bitLength);
        ulong wordCount = bitLength / WORD_LENGTH;
        lowWordInd = (int)(lowLetterInd / WORD_LENGTH);
        bitShiftAmount = (int)(lowLetterInd % WORD_LENGTH);
        highWordInd = (int)((lowLetterInd + WORD_LENGTH - 1) % wordCount);
      }

      public override string ToString()
      {
        return $"{nameof(lowWordInd)}:{lowWordInd}, {nameof(highWordInd)}:{highWordInd}, {nameof(bitShiftAmount)}:{bitShiftAmount}";
      }
    }
  }
}
