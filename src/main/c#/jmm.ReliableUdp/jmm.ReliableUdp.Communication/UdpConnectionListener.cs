using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace jmm.ReliableUdp.Communication
{
  public class UdpConnectionListener
  {

    private IPEndPoint remoteEp;
    private UdpClient udpClient;
    private int port;
    private bool running;

    public event EventHandler<MsgArgs> DatagramReceived;


    public UdpConnectionListener(int port)
    {
      remoteEp = new IPEndPoint(IPAddress.Any, 0);
      this.port = port;
      running = false;
    }

    public void Start()
    {
      if (running)
        return;

      udpClient = new UdpClient(port);
      running = true;
      while (running)
      {
        byte[] payload;
        MsgArgs msgArgs;
        try
        {
          payload = udpClient.Receive(ref remoteEp);
          msgArgs = new MsgArgs(remoteEp.Address, remoteEp.Port, payload);
        }
        catch (SocketException e)
        {
          if (!running)
            return;

          throw e;
        }
        if (DatagramReceived != null)
        {
          Task.Run(() => DatagramReceived.Invoke(this, msgArgs));
        }
      }
    }

    public void Stop()
    {
      running = false;
      udpClient.Close();
    }
  }
}
