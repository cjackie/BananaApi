using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace BananaClient
{
    public interface INetworkAdapter
    {
        // Format is "protocol://(hostname|IP)[:port]".
        void Send(object request);
        // null when no object.
        object Receive();
        void Start();
        void Stop();
    }

    public struct ConnectServerRequest {
        public ushort eventType;
        public uint magicNumber;
        public char authType;

        // Max size of 256.
        public char[] username;
        // Max size of 256.
        public char[] password;
    }

    public struct ConnectServerResponse {
        public ushort eventType;
        public byte[] sessionId;
        public ulong userId;
        public byte errorCode;
    }

    public struct NetworkMessage {
        public static int MaxMessageSize = 65535;
        public static ushort EventType_CreateRoom = 0x0001;
        public static ushort EventType_JoinRoom = 0x0002;
        public static ushort EventType_ServerBroadcast = 0x0003;
        public static ushort EventType_ClientBroadcast = 0x0004;
        public static ushort EventType_ConnectServer = 0x0005;
        public static ushort EventType_LeaveRoom = 0x0006;
        public static ushort EventType_SampleRooms = 0x0007;
        public static ushort EventType_Ping = 0x0008;
        public static ushort EventType_UpdateStorage = 0x0009;
        public static ushort EventType_GetStorage = 0x000A;

    }
  
    
}