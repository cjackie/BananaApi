using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BananaClient;
using NUnit.Framework;
using UnityEngine.TestTools;


public class MemoryStreamWrapper: Stream
{
    private long readPos, writePos;

    private bool _Disposed {get; set;} = false;

    private readonly MemoryStream ms;

    public override bool CanRead => ms.CanRead;

    public override bool CanSeek => ms.CanSeek;

    public override bool CanWrite => ms.CanWrite;

    public override long Length => ms.Length;

    public override long Position 
    { 
        get => ms.Position;
        set => ms.Position = value; 
    }

    public override void Flush() => ms.Flush();

    public override long Seek(long offset, SeekOrigin origin) => ms.Seek(offset, origin);

    public override void SetLength(long value) => ms.SetLength(value);


    public MemoryStreamWrapper()
    {
        readPos = writePos = 0;
        ms = new MemoryStream();
    }

    public MemoryStreamWrapper(int capacity)
    {
        readPos = writePos = 0;
        ms = new MemoryStream(capacity);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ms.Seek(readPos, SeekOrigin.Begin);
        var byteRead = ms.Read(buffer, offset, count);
        readPos = ms.Position;
        //Debug.Log("ReadPos: " + readPos);
        return byteRead;
    }

    public override int ReadByte()
    {
        ms.Seek(readPos, SeekOrigin.Begin);
        var byteRead = ms.ReadByte();
        readPos = ms.Position;
        //Debug.Log("ReadPos: " + readPos);
        return byteRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ms.Seek(writePos, SeekOrigin.Begin);
        ms.Write(buffer, offset, count);
        writePos = ms.Position;
        //Debug.Log("WritePos: " + writePos);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!_Disposed)
        {
            ms.Dispose();
            _Disposed = true;
        }
    }
    
    ~MemoryStreamWrapper()
    {
        Dispose(false);
    }
}

public class TcpAdapterTest
{
    // A Test behaves as an ordinary method
    [Test]
    public void StartTcpAdapter()
    {
        MemoryStreamWrapper test_stream = new MemoryStreamWrapper();
        TcpAdapter tcpAdapter = new TcpAdapter(test_stream);
        tcpAdapter.Start();
        Thread.Sleep(100);
        Assert.AreEqual(tcpAdapter.Error, "");
    }

    [Test]
    public void SendingObject()
    {
        MemoryStreamWrapper test_stream = new MemoryStreamWrapper(66000);
        TcpAdapter tcpAdapter = new TcpAdapter(test_stream);
        tcpAdapter.Start();

        // ConnectServerRequest from TCP Adapter.
        ConnectServerRequest connectServerRequest;
        connectServerRequest.eventType = NetworkMessage.EventType_ConnectServer;
        connectServerRequest.magicNumber = BitConverter.ToUInt32(BytesUtils.HexStringToByteArray("1289 CB79"), 0);
        connectServerRequest.authType = 'A';
        connectServerRequest.username = new char[] {'h', 'e', 'l', 'l', 'o'};
        connectServerRequest.password = new char[] { 'p', 'a', 's', 's', '!' };

        tcpAdapter.Send(connectServerRequest);

        // ConnectServerResponse from stream
        byte[] rawResponse = BytesUtils.HexStringToByteArray(
            "0005"+
            "0000 0000 0000 0000 0000 0000 0000 0000 00DD 0000"+
            "0000 0000 0000 A290"+
            "00");

        byte[] zeros = new byte[520 - rawResponse.Length];
        test_stream.Write(rawResponse, 0, rawResponse.Length);
        test_stream.Write(zeros, 0, zeros.Length);

        Thread.Sleep(400);

        var response = tcpAdapter.Receive();
        Assert.IsNotNull(response);
        var connectServerResponse = (ConnectServerResponse)response;
        // expect and equal are reversed
        Assert.AreEqual(connectServerResponse.eventType, 5);
        // Little Endianess?
        Assert.AreEqual(connectServerResponse.sessionId, BytesUtils.HexStringToByteArray("0000 DD00 0000 0000 0000 0000 0000 0000 0000 0000"));
        Assert.AreEqual(connectServerResponse.userId, 41616);
        Assert.AreEqual(connectServerResponse.errorCode, 0);

        response = tcpAdapter.Receive();
        Assert.IsNotNull(response);
        connectServerResponse = (ConnectServerResponse)response;
        // expect and equal are reversed
        Assert.AreEqual(connectServerResponse.eventType, 5);
        foreach(var b in connectServerResponse.sessionId)
        {
            Debug.Log((char)b);
        }
        // Little Endianess?
        Assert.AreEqual(connectServerResponse.sessionId, BytesUtils.HexStringToByteArray("1289 CB79A"));
        Assert.AreEqual(connectServerResponse.userId, 41616);
        Assert.AreEqual(connectServerResponse.errorCode, 0);

        // byte[] raw_request = new byte[520];
        // test_stream.Read(raw_request, 0, raw_request.Length);
        // foreach (var b in raw_request)
        // {
        //     Debug.Log((char)b);
        // }
        // Assert.AreEqual(BytesUtils.Slice(raw_request, 0, 2), BytesUtils.HexStringToByteArray("0005"));
        // Assert.AreEqual(BytesUtils.Slice(raw_request, 2, 4), BytesUtils.HexStringToByteArray("1289 CB79"));
        // Assert.AreEqual(BitConverter.ToString(raw_request, 6, 256), "hello");
        // Assert.AreEqual(BitConverter.ToString(raw_request, 262, 256), "pass!");

    }
}
