using System;
using System.Collections.Generic;
using System.Text;

namespace jmm.ReliableUdp.Communication
{
  class SwitchServerOptions
  {
    public uint RetryWaitTime { get; set; }
    public uint MaxRetries { get; set; }

    public SwitchServerOptions(uint retryWaitTime, uint maxRetries)
    {
      RetryWaitTime = retryWaitTime;
      MaxRetries = maxRetries;
    }

    public static SwitchServerOptions CreateDefault()
    {
      return new SwitchServerOptions(1000, 5);
    }
  }
}
