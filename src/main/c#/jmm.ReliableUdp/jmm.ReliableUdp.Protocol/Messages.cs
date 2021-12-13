using System;
using System.Text;

namespace jmm.ReliableUdp.Protocol
{
  public static class Messages
  {
    public static byte[] CONNECT_HANDSHAKE_PAYLOAD { get; private set; }
    public static byte[] SWITCH_ACK_PAYLOAD { get; private set; }

    static Messages()
    {
      InitializeConnectionRequestPayload();
      SWITCH_ACK_PAYLOAD = Encoding.UTF8.GetBytes(Constants.SWITCH_ACK_UTF8);
    }

    private static void InitializeConnectionRequestPayload()
    {
      byte[] requestToken = Encoding.UTF8.GetBytes(Constants.CONNECTION_REQUEST_UTF8);
      Version ver = typeof(Messages).Assembly.GetName().Version;
      byte[] majorVer = BitConverter.GetBytes(ver.MajorRevision);
      byte[] minorVer = BitConverter.GetBytes(ver.MinorRevision);
      
      // Both version byte arrays are size 2
      int length = 4 + requestToken.Length;

      CONNECT_HANDSHAKE_PAYLOAD = new byte[length];
      Array.Copy(majorVer, 0, CONNECT_HANDSHAKE_PAYLOAD, 0, 2);
      Array.Copy(minorVer, 0, CONNECT_HANDSHAKE_PAYLOAD, 2, 2);
      Array.Copy(requestToken, 0, CONNECT_HANDSHAKE_PAYLOAD, 4, requestToken.Length);
    }

    public static bool IsConnectionRequest(byte[] payload)
    {
      return ArrayMatch(payload, CONNECT_HANDSHAKE_PAYLOAD);
    }

    public static byte[] SwitchToPortPayload(ushort port)
    {
      byte[] payload = new byte[CONNECT_HANDSHAKE_PAYLOAD.Length + 2];
      byte[] portBytes = BitConverter.GetBytes(port);
      Array.Copy(CONNECT_HANDSHAKE_PAYLOAD, 0, payload, 0, CONNECT_HANDSHAKE_PAYLOAD.Length);
      Array.Copy(portBytes, 0, payload, CONNECT_HANDSHAKE_PAYLOAD.Length, 2);
      return payload;
    }

    public static bool IsSwitchAck(byte[] payload)
    {
      return ArrayMatch(payload, SWITCH_ACK_PAYLOAD);
    }

    public static bool ArrayMatch(byte[] arr1, byte[] arr2)
    {
      if (arr1.Length != arr2.Length)
        return false;

      for (int i = 0; i < arr1.Length; i++)
      {
        if (arr1[i] != arr2[i])
          return false;
      }
      return true;

    }
  }
}
