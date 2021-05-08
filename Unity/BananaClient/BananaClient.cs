using System;
using System.Collections.Generic;

namespace BananaClient
{
    public interface IBananaClient
    {
        void Connect(ConnectOptions options);
        void SampleRooms(int numOfRooms);
        void CreateRoom();
        void JoinRoom(uint roomCode);
        void LeaveRoom();
        void Broadcast(uint actionId, byte[] data);
        void AddCallback(IBananaClientCallback callback);          
    }

    public interface IBananaClientCallback
    {
        void ConnectCallback(ErrorCode errorCode);
        void SampleRoomsCallback(SampleRoomResponse response);
        void JoinRoomCallback(ErrorCode errorCode);
        void LeaveRoomCallback(ErrorCode errorCode);
        void OnServerBroadcast(uint actionId, byte[] data);
        void OnClientBroadcast(uint actionId, byte[] data);
    }

    public struct ConnectOptions
    {
        public string username;
        public string password;
        public string serverIp;
        public ushort port;

        public ConnectOptions(string username, string password, string serverIp, ushort port)
        {
            this.username = username;
            this.password = password;
            this.serverIp = serverIp;
            this.port = port;
        }
    }

    public struct ErrorCode
    {
        public int errorCode;

        public ErrorCode(int errorCode)
        {
            this.errorCode = errorCode;
        }
    }

    public struct ActionId
    {
        uint id;
    }

    public struct SampleRoomResponse
    {
        public bool firstTurn;
        public List<uint> roomCodes;

        public SampleRoomResponse(bool firstTurn, List<uint> roomCodes)
        {
            this.firstTurn = firstTurn;
            this.roomCodes = roomCodes;
        }
    }
}
