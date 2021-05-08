using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaClient
{
    public class Example : IBananaClientCallback 
    {
        private List<string> eventLog = new List<string>();
        private List<uint> roomCodes = new List<uint>();

        private IBananaClient client;
        public Example(IBananaClient bananaClient)
        {
            client = bananaClient;
            eventLog.Add("Attempt to connect to game server.");
            client.Connect(new ConnectOptions("username", "",
                "localhost", 2331));
        }

        public void ConnectCallback(ErrorCode errorCode)
        {
            if (errorCode.errorCode == 0)
            {
                eventLog.Add("Connection is established.");
                eventLog.Add("Sampling rooms.");
                client.SampleRooms(200);
            } else
            {
                eventLog.Add("Connection fails.");
            }
        }

        public void JoinRoomCallback(ErrorCode errorCode)
        {
            if (errorCode.errorCode == 0)
            {
                eventLog.Add("Joined room");
            } else
            {
                eventLog.Add("Joining room fails");
            }
        }

        public void LeaveRoomCallback(ErrorCode errorCode)
        {
            if (errorCode.errorCode == 0)
            {
                eventLog.Add("Left the room");
            } else
            {
                eventLog.Add("Left the room fails");
            }
        }

        public void OnClientBroadcast(uint actionId, byte[] data)
        {
            eventLog.Add("OnClientBroadcast: actionId=" + actionId +
                ", bytes" + BitConverter.ToString(data));            
        }

        public void OnServerBroadcast(uint actionId, byte[] data)
        {
            eventLog.Add("OnServerBroadcast: actionId=" + actionId +
                ", bytes" + BitConverter.ToString(data));
        }

        public void SampleRoomsCallback(SampleRoomResponse response)
        {
            if (response.firstTurn) {
                eventLog.Add("Receives first SampleRoomsCallback.");
                roomCodes = response.roomCodes;

                eventLog.Add("Joining room: " + roomCodes[0]);
                client.JoinRoom(roomCodes[0]);
            } else
            {
                eventLog.Add("Receives SampleRoomsCallback for rooms removed.");
                foreach (var roomCode in response.roomCodes)
                {
                    roomCodes.Remove(roomCode);
                }
            }
        }
    }
}
