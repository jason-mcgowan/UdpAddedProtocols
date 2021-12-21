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
      //IPEndPoint ep1 = new IPEndPoint(IPAddress.Loopback, 9000);
      //IPEndPoint ep2 = new IPEndPoint(IPAddress.Loopback, 9001);
      //UdpClient c1 = new UdpClient(9000);
      //UdpClient c2 = new UdpClient(9001);

      ThreadPool.SetMaxThreads(10, 100);
      int workerThreads;
      int completionPortThreads;
      ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
      Console.WriteLine("Max worker and completion port threads: " + workerThreads + ", " + completionPortThreads);
      for (int i = 0; i < 20; i++)
      {
        Task.Run(() => WasteTime(2000));
      }

      ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
      Console.WriteLine("Max worker and completion port threads: " + workerThreads + ", " + completionPortThreads);

      ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
      Console.WriteLine("Available worker and completion port threads: " + workerThreads + ", " + completionPortThreads);

      Console.ReadKey();
    }

    private static void WasteTime(int ms)
    {
      Console.WriteLine("Wasting time on thread " + Thread.CurrentThread.ManagedThreadId);
      Thread.Sleep(ms);
    }

    private static void PrintReceiptsAsync(UdpClient c1)
    {
      c1.BeginReceive(PrintReceiptCallback, new object());
    }

    private static void PrintReceiptCallback(IAsyncResult ar)
    {

    }

    private static void RunSwitchServer()
    {
      SwitchServer server = new SwitchServer(9000);
      server.ConnectionEstablished += Server_ConnectionEstablished;
      Task.Run(() => server.Start());
    }

    private static void TwoClientsConnect()
    {
      SwitchServer server = new SwitchServer(9000);
      server.ConnectionEstablished += Server_ConnectionEstablished;
      Task.Run(() => server.Start());

      IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 9000);
      SwitchClient sc1 = new SwitchClient(serverEp, 8888);
      sc1.EstablishedConnection += Client_EstablishedConnection;
      sc1.Connect();
      SwitchClient sc2 = new SwitchClient(serverEp, 9999);
      sc2.EstablishedConnection += Client_EstablishedConnection;
      sc2.Connect();
    }

    private static void Server_ConnectionEstablished(object sender, ChannelArgs e)
    {
      Console.WriteLine("Established new connection, listening for messages on separate channel");
      e.ConnectedChannel.MessageReceived += PrintChannelMessageStrings;
    }

    private static void Client_EstablishedConnection(object sender, EventArgs e)
    {
      SwitchClient client = (SwitchClient)sender;
      Channel switchChannel = client.SwitchChannel;
      Console.WriteLine("Established connection!");
    }

    private static void ChannelListenAndPrint()
    {
      Console.WriteLine("Starting server");
      IPEndPoint unityEp = new IPEndPoint(IPAddress.Loopback, 8888);
      UdpClient udpc1 = new UdpClient(9000);
      Channel c1 = new Channel(udpc1, unityEp);
      c1.MessageReceived += PrintChannelMessageStrings;
      c1.Start();
    }

    private static void PrintChannelMessageStrings(object sender, MsgArgs e)
    {
      byte[] dgram = e.Dgram;
      string payload;
      if (dgram.Length <= 3)
      {
        payload = "";
      }
      else
      {
        int payloadLength = dgram.Length - 3;
        payload = Encoding.UTF8.GetString(dgram, 3, payloadLength);
      }

      Console.WriteLine("Received message, id: " + e.Id + " payload as UTF8: " + payload);
    }

    private static void TalkToSelf()
    {
      IPEndPoint ep1 = new IPEndPoint(IPAddress.Loopback, 9000);
      IPEndPoint ep2 = new IPEndPoint(IPAddress.Loopback, 9001);
      UdpClient udpc1 = new UdpClient(9000);
      UdpClient udpc2 = new UdpClient(9001);
      Channel c1 = new Channel(udpc1, ep2);
      Channel c2 = new Channel(udpc2, ep1);
      c1.MessageReceived += OnMessageReceived;
      c2.MessageReceived += OnMessageReceived;
      c1.Start();
      c2.Start();
      byte[] payload = new byte[] { 1, 2, 3 };
      Console.WriteLine("Sending from c1 to c2");
      c1.SendRetries(1, payload, () => OnAckCallback("c1"), () => FailCallback("c1"));
    }

    private static void OnAckCallback(string input)
    {
      Console.WriteLine("Ack received: " + input);
    }

    private static void FailCallback(string input)
    {
      Console.WriteLine("Fail callback: " + input);
    }

    private static void OnMessageReceived(object sender, MsgArgs e)
    {
      Console.WriteLine("Received message, flags: " + (byte)e.Flags + " id: " + e.Id);
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
