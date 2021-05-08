using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BananaClient;
using NUnit.Framework;
using UnityEngine.TestTools;

public class TcpAdapterTest : MonoBehaviour
{
    // A Test behaves as an ordinary method
    [Test]
    public void StartTcpAdapter()
    {
        MemoryStream test_stream = new MemoryStream();
        TcpAdapter tcpAdapter = new TcpAdapter(test_stream);
        tcpAdapter.Start();
        Thread.Sleep(100);
        Assert.AreEqual(tcpAdapter.Error, "");
    }

    [Test]
    public void SendingObject()
    {
        MemoryStream test_stream = new MemoryStream(66000);
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

        test_stream.Write(rawResponse, 0, rawResponse.Length);

        // Wait data transmition. 
        Thread.Sleep(200);

        ConnectServerResponse connectServerResponse = (ConnectServerResponse)tcpAdapter.Receive();
        Assert.IsNotNull(connectServerResponse);
        Assert.AreEqual(connectServerResponse.eventType, BytesUtils.HexStringToByteArray("0005"));
        Assert.AreEqual(connectServerResponse.sessionId, BytesUtils.HexStringToByteArray("0000 0000 0000 0000 0000 0000 0000 0000 00DD 0000"));
        Assert.AreEqual(connectServerResponse.userId, 41616);
        Assert.AreEqual(connectServerResponse.errorCode, 0);

        byte[] raw_request = new byte[2+4+1+256+256];
        test_stream.Read(raw_request, 0, raw_request.Length);
        Assert.AreEqual(BytesUtils.Slice(raw_request, 0, 2), BytesUtils.HexStringToByteArray("0005"));
        Assert.AreEqual(BytesUtils.Slice(raw_request, 2, 4), BytesUtils.HexStringToByteArray("1289 CB79"));
        Assert.AreEqual(BitConverter.ToString(raw_request, 6, 256), "hello");
        Assert.AreEqual(BitConverter.ToString(raw_request, 262, 256), "pass!");
    }
}
