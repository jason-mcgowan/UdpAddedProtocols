using System;
using System.Collections.Generic;
using System.Text;

namespace jmm.ReliableUdp.Communication
{
  public class RetryOptions
  {
    public uint RetryWaitTime { get; set; }
    public uint MaxRetries { get; set; }

    public RetryOptions(uint retryWaitTime, uint maxRetries)
    {
      RetryWaitTime = retryWaitTime;
      MaxRetries = maxRetries;
    }

    public static RetryOptions CreateDefault()
    {
      return new RetryOptions(1000, 5);
    }
  }
}
