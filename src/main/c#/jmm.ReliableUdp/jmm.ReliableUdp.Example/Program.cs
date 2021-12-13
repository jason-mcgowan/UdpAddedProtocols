using jmm.ReliableUdp.Communication;
using jmm.ReliableUdp.Protocol;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jmm.ReliableUdp.Example
{
  internal class Program
  {
    private static void Main(string[] args)
    {

      ushort test = 0;
      Console.WriteLine("Max uint16: " + test);
      byte[] maxBytes = BitConverter.GetBytes(test);
      foreach (var num in maxBytes)
      {
        Console.WriteLine(num);
      }
      Console.WriteLine("maxBytes length: " + maxBytes.Length);
      Console.WriteLine("Press any key to exit");
      Console.ReadKey();
    }

    private static byte[] TestConnectionHandshakeToken()
    {
      byte[] CONNECTION_REQUEST = Encoding.UTF8.GetBytes("de8567de606e4a6db9c8292b703d8f6d");
      Version ver = typeof(Messages).Assembly.GetName().Version;
      byte[] majorVer = BitConverter.GetBytes(ver.MajorRevision);
      byte[] minorVer = BitConverter.GetBytes(ver.MinorRevision);
      Console.WriteLine("Version: " + ver);
      Console.WriteLine("Major revision version: " + ver.Major);
      Console.WriteLine("Minor revision version: " + ver.Minor);
      Console.WriteLine("Major version byte array length: " + majorVer.Length);
      int length = majorVer.Length + minorVer.Length + CONNECTION_REQUEST.Length;
      Console.WriteLine("Total length: " + length);
      byte[] qualifiedConnectionPayload = new byte[length];
      Array.Copy(majorVer, 0, qualifiedConnectionPayload, 0, majorVer.Length);
      Array.Copy(minorVer, 0, qualifiedConnectionPayload, majorVer.Length, minorVer.Length);
      Array.Copy(CONNECTION_REQUEST, 0, qualifiedConnectionPayload, majorVer.Length + minorVer.Length, CONNECTION_REQUEST.Length);
      return qualifiedConnectionPayload;
    }

    private static void MultiConnect()
    {
      UdpClient broadListener = new UdpClient(8080);
      IPEndPoint broadEp = new IPEndPoint(IPAddress.Any, 0);
      Task.Run(() => ReceiveMsg(broadListener, broadEp, "Broad"));

      IPEndPoint targetEp = new IPEndPoint(IPAddress.Loopback, 9000);
      Task.Run(() => ReceiveMsg(broadListener, targetEp, "Target"));

      UdpClient udpClient = new UdpClient(9000);
      Thread.Sleep(1000);
      for (int i = 0; i < 100; i++)
      {
        Console.WriteLine("Message number " + i);
        SendMessage(udpClient, "Test Message");
      }
      Thread.Sleep(1000);
    }

    private static void ReceiveMsg(UdpClient client, IPEndPoint ep, string owner)
    {
      while (true)
      {
        byte[] bytes = client.Receive(ref ep);
        Console.WriteLine(owner + " received message from port: " + ep.Port);
      }
    }

    private static void TestListener()
    {
      Listener conn = new Listener(8080);
      Task connListenerTask = Task.Run(() => conn.Start());
      conn.DatagramReceived += Conn_DatagramReceived;
      UdpClient udpClient = new UdpClient(9000);
      Thread.Sleep(50);
      SendMessage(udpClient, "Test Message");
      Thread.Sleep(50);
      SendMessage(udpClient, "Message 2");
      Thread.Sleep(50);
      SendMessage(udpClient, "And a third");
      Thread.Sleep(50);
      UdpClient udpClient2 = new UdpClient(9090);
      SendMessage(udpClient2, "Does this get through?");
      Thread.Sleep(1000);
      conn.Stop();
      Console.ReadKey();
    }

    private static void Conn_DatagramReceived(object sender, MsgArgs e)
    {
      String message = Encoding.UTF8.GetString(e.Payload);
      Console.WriteLine(e.Address + ":" + e.Port + " sent " + message);
    }

    private static void SendMessage(UdpClient udpClient, string message)
    {
      byte[] messageBytes = Encoding.UTF8.GetBytes(message);
      int count = udpClient.Send(messageBytes, messageBytes.Length, "127.0.0.1", 8080);
    }
  }
}
