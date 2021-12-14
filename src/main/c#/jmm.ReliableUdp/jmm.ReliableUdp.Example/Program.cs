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


      Thread.Sleep(1000);
    }

    private static void TestServerReturnsSwitches()
    {
      SwitchServer server = new SwitchServer(8080);
      Task serverTask = Task.Run(() => server.Start());

      IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 8080);
      Console.WriteLine("Server EP: " + serverEp);
      UdpClient client9000 = new UdpClient(new IPEndPoint(IPAddress.Loopback, 9000));
      client9000.Connect(IPAddress.Loopback, 8080);
      UdpClient client9001 = new UdpClient(new IPEndPoint(IPAddress.Loopback, 9001));
      client9001.Connect(IPAddress.Loopback, 8080);
      byte[] reqPayload = Messages.CONNECT_HANDSHAKE_PAYLOAD;
      byte[] ackPayload = Messages.SWITCH_ACK_PAYLOAD;
      client9000.Send(reqPayload, reqPayload.Length);      
      client9001.Send(reqPayload, reqPayload.Length);
      Thread.Sleep(2000);
      Console.WriteLine("Trying to receive from: " + serverEp);
      client9000.Receive(ref serverEp);
      Console.WriteLine("Recieved from: " + serverEp);
      client9000.Send(ackPayload, ackPayload.Length);    
      Thread.Sleep(1000);
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

    private static void SendMessage(UdpClient udpClient, string message)
    {
      byte[] messageBytes = Encoding.UTF8.GetBytes(message);
      int count = udpClient.Send(messageBytes, messageBytes.Length, "127.0.0.1", 8080);
    }
  }
}
