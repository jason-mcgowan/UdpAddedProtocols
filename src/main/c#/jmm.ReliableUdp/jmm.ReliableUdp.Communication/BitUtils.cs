using System;
using System.Collections.Generic;
using System.Text;

namespace jmm.ReliableUdp.Communication
{
  internal static class BitUtils
  {
    internal static int RoundUpToPow2(int input)
    {
      // Get the integer division ceiling for number of 64-bit words
      int minWordCount = (input + 64 - 1) / 64;
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
  }
}
