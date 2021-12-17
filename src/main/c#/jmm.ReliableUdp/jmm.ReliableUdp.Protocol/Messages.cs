using System;
using System.Text;

namespace jmm.ReliableUdp.Protocol
{
  /// <summary>
  /// Contains the standard Messages used in this protocol
  /// </summary>
  public static class Messages
  {
    /// <summary>
    /// The message payload used in connection request handshakes
    /// </summary>
    public static byte[] CONNECT_HANDSHAKE_PAYLOAD { get; private set; }
    /// <summary>
    /// The message payload used to acknowledge a switching port
    /// </summary>
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

    /// <summary>
    /// Returns whether the payload is a valid connection request
    /// </summary>
    public static bool IsConnectionRequest(byte[] payload)
    {
      return ArrayMatch(payload, CONNECT_HANDSHAKE_PAYLOAD);
    }

    /// <summary>
    /// Returns the payload for a message on which server port the channel is switching to
    /// </summary>
    public static byte[] SwitchToPortPayload(ushort port)
    {
      byte[] payload = new byte[CONNECT_HANDSHAKE_PAYLOAD.Length + 2];
      byte[] portBytes = BitConverter.GetBytes(port);
      Array.Copy(CONNECT_HANDSHAKE_PAYLOAD, 0, payload, 0, CONNECT_HANDSHAKE_PAYLOAD.Length);
      Array.Copy(portBytes, 0, payload, CONNECT_HANDSHAKE_PAYLOAD.Length, 2);
      return payload;
    }

    /// <summary>
    /// Returns the SWITCH message new channel port if valid, otherwise returns 0.
    /// </summary>
    /// <param name="dgram">The datagram to verify and pull from</param>
    /// <returns>0 on invalid message and the server channel SWITCH port number if valid</returns>
    public static ushort GetSwitchPort(byte[] dgram)
    {
      if (dgram.Length != CONNECT_HANDSHAKE_PAYLOAD.Length + 2)      
        return 0;
      
      bool arraysMatch = ArrayMatch(dgram, 0, CONNECT_HANDSHAKE_PAYLOAD, 0, CONNECT_HANDSHAKE_PAYLOAD.Length);
      if (!arraysMatch)
        return 0;

      return BitConverter.ToUInt16(dgram, CONNECT_HANDSHAKE_PAYLOAD.Length);
    }

    /// <summary>
    /// Returns whether the message is a valid SWITCH acknowledge.
    /// </summary>
    public static bool IsSwitchAck(byte[] payload)
    {
      return ArrayMatch(payload, SWITCH_ACK_PAYLOAD);
    }

    /// <summary>
    /// Returns whether the two arrays contain equal contents
    /// </summary>
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

    /// <summary>
    /// Compares portions of two arrays and returns if their elements are equal value or not
    /// </summary>
    /// <remarks>Checks array index bounds and will not throw exception</remarks>
    /// <param name="arr1">The first array</param>
    /// <param name="i1">First array starting index to compare</param>
    /// <param name="arr2">The other array</param>
    /// <param name="i2">Comparison starting index for other array</param>
    /// <param name="length">Number of array elements to compare</param>
    /// <returns>True on match, False otherwise. Returns False on any index out of bound issues.</returns>
    public static bool ArrayMatch(byte[] arr1, int i1, byte[] arr2, int i2, int length)
    {
      if (arr1.Length < i1 + length)
        return false;
      if (arr2.Length < i2 + length)
        return false;

      for (int i = 0; i < length; i++)
      {
        if (arr1[i1 + i] != arr2[i2 + i])
          return false;
      }
      return true;
    }
  }
}
