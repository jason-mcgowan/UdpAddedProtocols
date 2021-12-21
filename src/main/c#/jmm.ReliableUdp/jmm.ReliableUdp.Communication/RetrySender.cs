using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace jmm.ReliableUdp.Communication
{
  /// <summary>
  /// Continually sends datagrams until end conditions are met or halted
  /// </summary>
  internal class RetrySender
  {
    private UdpClient xmitClient;
    private int retryNumber;
    private IPEndPoint remoteEp;
    private RetryOptions options;
    private bool running;
    private byte[] dgram;
    private CancellationTokenSource delayCancel;

    /// <param name="xmitClient">The <see cref="UdpClient"/> used to send</param>
    /// <param name="remoteEp">The target remote endpoint</param>
    /// <param name="options">Options for retrying</param>
    /// <param name="dgram">The datagram to send</param>
    internal RetrySender(UdpClient xmitClient, IPEndPoint remoteEp, RetryOptions options, byte[] dgram)
    {
      this.xmitClient = xmitClient;
      this.remoteEp = remoteEp;
      this.options = options;
      this.dgram = dgram;
      retryNumber = 0;
      running = true;
      delayCancel = new CancellationTokenSource();
    }

    /// <summary>
    /// Blocks while sending retries until max retry count reached or process is interrupted with <see cref="Close"/>
    /// </summary>
    /// <remarks>Recommend running as a background task on the thread</remarks>
    /// <param name="timeoutCallback">Invoked if all retries have been sent and the final wait time passed without being closed</param>
    internal async Task SendRetriesAsync(Action timeoutCallback)
    {
      while (retryNumber <= options.MaxRetries)
      {
        if (!running)
          return;
        xmitClient.Send(dgram, dgram.Length, remoteEp);
        retryNumber++;
        await Task.Delay((int)options.RetryWaitTime, delayCancel.Token);
      }
      if (!running)
        return;

      Close();
      timeoutCallback?.Invoke();
    }

    /// <summary>
    /// Stops sending datagrams and halts the currently waiting thread
    /// </summary>
    internal void Close()
    {
      running = false;
      delayCancel.Cancel();
      delayCancel.Dispose();
    }

    internal void ResetRetryCount()
    {
      retryNumber = 0;
    }
  }
}
