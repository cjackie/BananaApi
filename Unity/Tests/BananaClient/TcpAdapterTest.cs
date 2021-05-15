using System;
using System.Threading;
using System.IO;
using UnityEngine;
using BananaClient;
using NUnit.Framework;

public class TcpAdapterTest : MonoBehaviour
{
    // A Test behaves as an ordinary method
    [Test]
    public void StartTcpAdapter()
    {
        MemoryStream test_stream = new MemoryStream();
        TcpAdapter tcpAdapter = new TcpAdapter(test_stream, null);
        tcpAdapter.Start();
        Thread.Sleep(100);
        Assert.AreEqual(tcpAdapter.Error, "");
    }

    class ReadonlyMemoryStream : MemoryStream
    {
        public byte[] buffer = new byte[NetworkMessage.MaxMessageSize];
        private int offset = 0;
        public override int ReadByte()
        {
            return buffer[offset++];
        }
        public override void WriteByte(byte value)
        {
            while (true)
                Thread.Sleep(1000);
        }
    }

    class WriteOnlyMemoryStream : MemoryStream
    {
        public byte[] buffer = new byte[NetworkMessage.MaxMessageSize];
        private int offset = 0;
        public override int ReadByte()
        {
            while (true)
                Thread.Sleep(1000);
        }
        public override void WriteByte(byte value)
        {
            buffer[offset++] = value;
        }
    }

    [Test]
    public void ReceivingNetworkMessage()
    {
        ReadonlyMemoryStream readonlyMemoryStream = new ReadonlyMemoryStream();
        TcpAdapter tcpAdapter = new TcpAdapter(readonlyMemoryStream, new ConnectServerResponseBytesFiller());

        byte[] rawResponse = BytesUtils.HexStringToByteArray(
          "0005" +
          "0000 0000 0000 0000 0000 0000 0000 0000 00DD 0000" +
          "0000 0000 0000 A290" +
          "00");
        readonlyMemoryStream.buffer = rawResponse;

        tcpAdapter.Start();

        // Wait data conversion. 
        Thread.Sleep(200);

        ConnectServerResponse connectServerResponse = (ConnectServerResponse)tcpAdapter.Receive();
        Assert.IsNotNull(connectServerResponse);
        Assert.AreEqual(connectServerResponse.EventType(), BytesUtils.HexStringToByteArray("0005"));
        Assert.AreEqual(connectServerResponse.SessionId, BytesUtils.HexStringToByteArray("0000 0000 0000 0000 0000 0000 0000 0000 00DD 0000"));
        Assert.AreEqual(connectServerResponse.UserId, 41616);
        Assert.AreEqual(connectServerResponse.ErrorCode, 0);

    }

    [Test]
    public void SendingNetworkMessage()
    {
        WriteOnlyMemoryStream writeOnlyMemoryStream = new WriteOnlyMemoryStream();
        TcpAdapter tcpAdapter = new TcpAdapter(writeOnlyMemoryStream, null);
        tcpAdapter.Start();

        // ConnectServerRequest from TCP Adapter.
        ConnectServerRequest connectServerRequest = new ConnectServerRequest(
            NetworkMessage.EventType_ConnectServer,
             BitConverter.ToUInt32(BytesUtils.HexStringToByteArray("1289 CB79"), 0),
             'A',
             new char[] { 'h', 'e', 'l', 'l', 'o' },
             new char[] { 'p', 'a', 's', 's', '!' });

        tcpAdapter.Send(connectServerRequest);

        // Wait data conversion. 
        Thread.Sleep(200);

        byte[] raw_request = writeOnlyMemoryStream.buffer;
        Assert.AreEqual(BytesUtils.Slice(raw_request, 0, 2), BytesUtils.HexStringToByteArray("0005"));
        Assert.AreEqual(BytesUtils.Slice(raw_request, 2, 4), BytesUtils.HexStringToByteArray("1289 CB79"));
        Assert.AreEqual(BitConverter.ToString(raw_request, 6, 256), "hello");
        Assert.AreEqual(BitConverter.ToString(raw_request, 262, 256), "pass!");
    }
}
