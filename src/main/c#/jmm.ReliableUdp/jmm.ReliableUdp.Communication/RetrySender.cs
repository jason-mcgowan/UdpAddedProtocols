using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace jmm.ReliableUdp.Communication
{
  internal class RetrySender
  {
    private UdpClient xmitClient;
    private int retryNumber;
    private IPEndPoint remoteEp;
    private RetryOptions options;
    private bool running;
    private byte[] dgram;
    private Thread sleepyThread;

    internal RetrySender(UdpClient xmitClient, IPEndPoint remoteEp, RetryOptions options, byte[] dgram)
    {
      this.xmitClient = xmitClient;
      this.remoteEp = remoteEp;
      this.options = options;
      this.dgram = dgram;
      retryNumber = 0;
      running = true;
    }

    internal void SendRetries(Action timeoutCallback)
    {
      sleepyThread = Thread.CurrentThread;
      while (retryNumber <= options.MaxRetries)
      {
        if (!running)
          return;
        xmitClient.Send(dgram, dgram.Length, remoteEp);
        retryNumber++;
        try
        {
          Thread.Sleep((int)options.RetryWaitTime);
        }
        catch (ThreadInterruptedException e)
        {
          if (running)
            throw e;
        }
      }
      if (!running)
        return;

      Close();
      timeoutCallback?.Invoke();
    }

    internal void Close()
    {
      running = false;
      //sleepyThread.Interrupt();
    }

    internal void ResetTimer()
    {
      retryNumber = 0;
    }
  }
}
