using System.Net;
using System.Net.Sockets;

namespace jmm
{
  public class UdpConnectionListener
  {

    private IPEndPoint listenRange;
    private UdpClient udpClient;
    private int port;
    private bool running;


    public UdpConnectionListener(int port)
    {
      listenRange = new IPEndPoint(IPAddress.Any, 0);
      this.port = port;
      running = false;
    }

    public void Start()
    {
      if (running)
        return;

      udpClient = new UdpClient(port);
      byte[] payload;
      running = true;
      while (running)
      {
        try
        {
          payload = udpClient.Receive(ref listenRange);
        }
        catch (SocketException e)
        {
          if (!running)
            return;

          throw e;
        }
        // todo 
      }
    }

    public void Stop()
    {
      running = false;
      udpClient.Close();
    }
  }
}
