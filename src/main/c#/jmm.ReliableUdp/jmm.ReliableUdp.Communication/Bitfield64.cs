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
    private const int WORD_LENGTH_LG_2 = 6; // 2^6 = 64

    public int WordCount { get; private set; }

    private ulong[] _words;
    private int _wordCountLg2;

    /// <summary>
    /// Creates an unchecked (wrapping) bitfield as underlying array of <see cref="System.UInt64"/> words.
    /// </summary>
    /// <remarks>
    /// <paramref name="wordCountLg2"/> is the power of 2 to use for count of words. Example, 0 would give 2^0 words, or a 64-bit field. 1, 2, 3 would yield 128-, 256-, and 512-bit fields, etc.
    /// </remarks>
    /// <param name="wordCountLg2">Must be [0, 25]. The power of 2 for count of 64-bit words</param>
    /// <param name="initVal">Initialize all bits to 1 (true) or 0 (false)</param>
    public Bitfield64(int wordCountLg2 = 0, bool initVal = false)
    {
      if (wordCountLg2 < 0)
      {
        throw new ArgumentOutOfRangeException($"{nameof(wordCountLg2)} must be non-negative");
      }
      if (wordCountLg2 > 25)
      {
        throw new ArgumentOutOfRangeException($"{nameof(wordCountLg2)} must be <26");
      }

      WordCount = (int)Math.Pow(2.0, wordCountLg2);
      _words = new ulong[WordCount];
      _wordCountLg2 = wordCountLg2;

      if (!initVal)
      {
        return;
      }
      for (int i = 0; i < _words.Length; i++)
      {
        _words[i] = ~0ul;
      }
    }

    /// <summary>
    /// Sets the bit to 0
    /// </summary>
    public void ZeroBit(ulong position)
    {
      // Find where it falls in the buffer, which word that corresponds to, and which index in that word
      WordIndices wi = WordIndices.CreateByPow2(position, _wordCountLg2);

      // Line up a 1 with the index in the word, flip the bits so now there's a single 0, store the AND
      _words[wi.lowWordInd] &= ~(1ul << wi.bitShiftAmount);
    }

    /// <summary>
    /// Performs bitwise OR operation lined up at the LSB with the provided <see cref="UInt64"/> bitfield
    /// </summary>
    public void Or(ulong lsbPos, ulong bits)
    {
      WordIndices wi = WordIndices.CreateByPow2(lsbPos, _wordCountLg2);
      _words[wi.lowWordInd] |= bits << wi.bitShiftAmount;
      _words[wi.highWordInd] |= bits >> (WORD_LENGTH - wi.bitShiftAmount);
    }

    /// <summary>
    /// Returns the 64-bits starting at and including the provided least significant bit position
    /// </summary>
    public ulong GetBits(ulong lsbPos)
    {
      return GetBits(WordIndices.CreateByPow2(lsbPos, _wordCountLg2));
    }

    private ulong GetBits(WordIndices wi)
    {
      ulong lowBits = _words[wi.lowWordInd] >> wi.bitShiftAmount;
      ulong highBits = _words[wi.highWordInd] << (WORD_LENGTH - wi.bitShiftAmount);
      return lowBits | highBits;
    }

    public override string ToString()
    {
      // Keeping with convention, MSB and word first (left) working down to the LSB on word 0, padded to full field size
      StringBuilder sb = new StringBuilder();
      for (int i = _words.Length - 1; i >= 0; i--)
      {
        sb.Append(Convert.ToString((long)_words[i], 2).PadLeft(WORD_LENGTH, '0'));
      }

      return sb.ToString();
    }

    private struct WordIndices
    {
      public int lowWordInd;
      public int highWordInd;
      public int bitShiftAmount;

      public static WordIndices CreateByPow2(ulong position, int wordCountLg2)
      {
        int modBitShift = wordCountLg2 + WORD_LENGTH_LG_2;
        ulong modMask = (1ul << modBitShift) - 1ul;
        ulong lowLetterInd = position & modMask;

        int divideBitShift = modBitShift - 1;

        return new WordIndices()
        {
          lowWordInd = (int)(lowLetterInd >> divideBitShift),
          highWordInd = (int)(((lowLetterInd + WORD_LENGTH - 1) & modMask) >> divideBitShift),
          bitShiftAmount = (int)(lowLetterInd & (WORD_LENGTH - 1))
        };
      }

      public override string ToString()
      {
        return $"{nameof(lowWordInd)}:{lowWordInd}, {nameof(highWordInd)}:{highWordInd}, {nameof(bitShiftAmount)}:{bitShiftAmount}";
      }
    }
  }
}
