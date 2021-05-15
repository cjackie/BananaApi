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
        void Send(NetworkMessage request);
        // null when no object.
        NetworkMessage Receive();
        void Start();
        void Stop();
    }

    public abstract class NetworkMessage {
        public const int MaxMessageSize = 65535; 

        public const ushort EventType_CreateRoom = 0x0001;
        public const ushort EventType_JoinRoom = 0x0002;
        public const ushort EventType_ServerBroadcast = 0x0003;
        public const ushort EventType_ClientBroadcast = 0x0004;
        public const ushort EventType_ConnectServer = 0x0005;
        public const ushort EventType_LeaveRoom = 0x0006;
        public const ushort EventType_SampleRooms = 0x0007;
        public const ushort EventType_Ping = 0x0008;
        public const ushort EventType_UpdateStorage = 0x0009;
        public const ushort EventType_GetStorage = 0x000A;

        public abstract ushort EventType();
    }

    public class ConnectServerRequest : NetworkMessage {
        private ushort eventType;
        private uint magicNumber;
        private char authType;

        // Max size of 256.
        private char[] username;
        // Max size of 256.
        private char[] password;

        public ConnectServerRequest(ushort eventType, uint magicNumber, char authType, char[] username, char[] password)
        {
            if (username.Length > 256 || password.Length > 256)
            {
                throw new Exception("Password or Username too long.");
            }

            this.eventType = eventType;
            this.magicNumber = magicNumber;
            this.authType = authType;
            this.username = username;
            this.password = password;
        }

        public uint MagicNumber { get => magicNumber; }
        public char AuthType { get => authType; }
        public char[] Username { get => username; }
        public char[] Password { get => password; }

        public override ushort EventType()
        {
            return eventType;
        }
    }

    public class ConnectServerResponse : NetworkMessage
    {
        private ushort eventType;
        private byte[] sessionId;
        private ulong userId;
        private byte errorCode;

        public ConnectServerResponse(ushort eventType, byte[] sessionId, ulong userId, byte errorCode)
        {
            this.eventType = eventType;
            this.sessionId = sessionId;
            this.userId = userId;
            this.errorCode = errorCode;
        }

        public override ushort EventType()
        {
            return eventType;
        }

        public ushort EventType1 { get => eventType; }
        public byte[] SessionId { get => sessionId; }
        public ulong UserId { get => userId; }
        public byte ErrorCode { get => errorCode; }
    }

}