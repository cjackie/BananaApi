using System;
using System.Collections.Generic;

namespace BananaClient
{
    public class SerializationUtils
    {

        public static byte[] SerializeRequest(NetworkMessage request)
        {
            if (request.GetType() == typeof(ConnectServerRequest))
            {
                var connectServerRequest = (ConnectServerRequest)request;
                int messageSize = 2 + 4 + 1 + 256 + 256;
                byte[] buffer = new byte[messageSize];
                int offset = 0;
                offset = BytesUtils.WritePrimitiveBytes(buffer, offset, connectServerRequest.EventType());
                offset = BytesUtils.WritePrimitiveBytes(buffer, offset, connectServerRequest.MagicNumber);
                offset = BytesUtils.WritePrimitiveBytes(buffer, offset, connectServerRequest.AuthType);

                offset = BytesUtils.WriteArrayBytes(buffer, offset, connectServerRequest.Username);
                offset = BytesUtils.WriteBytes(buffer, offset, BytesUtils.Zeros(256 - connectServerRequest.Username.Length));

                offset = BytesUtils.WriteArrayBytes(buffer, offset, connectServerRequest.Password);
                offset = BytesUtils.WriteBytes(buffer, offset, BytesUtils.Zeros(256 - connectServerRequest.Password.Length));
                if (offset != messageSize)
                {
                    throw new Exception("Message size is not matched");
                }
                return buffer;
            } else
            {
                throw new Exception("Unsupported type: " + request.GetType());
            }           
        }

        public static NetworkMessage DeserializeResponse(byte[] data)
        {
            ushort eventType = BitConverter.ToUInt16(data, 0);
            if (eventType == NetworkMessage.EventType_ConnectServer)
            {
                byte[] sessionId = BytesUtils.Slice(data, 2, 20);
                ulong userId = BitConverter.ToUInt64(data, 22);
                byte errorCode = data[30];
                return new ConnectServerResponse(eventType, sessionId, userId, errorCode);
            } else
            {
                throw new Exception("Unknown NetworkMessage");
            }
        }
    }

    public abstract class IMessageBytesFiller {
        private int messageSize = 0;
        private List<byte> buffer = new List<byte>();

        // Event Type 
        public abstract ushort EventType();

        // Return the message size of the fixed part.
        public abstract int GetFixedPartMessageSize();

        // Given the bytes of the fixed part of the message, return how
        // bytes for the variable part of the message. Return 0, if the message
        // does not have a variable size part.
        protected abstract int GetVariablePartMessageSize(byte[] fixedPart);
 
        // Return null when there is more to fill, otherwise return the
        // filled data.
        public byte[] Fill(byte b)
        {
            // initialize the size if we have not done it. 
            if (messageSize == 0)
            {
                messageSize = GetFixedPartMessageSize();
            }

            // Add the byte to our buffer.
            buffer.Add(b);

            // Add the size for the varaible part of fixed part has been filled.
            if (buffer.Count == messageSize)
            {
                messageSize += GetVariablePartMessageSize(buffer.ToArray());
            }

            // If message too big, throw an exception.
            if (messageSize > NetworkMessage.MaxMessageSize)
            {
                throw new Exception("Message to be filled is too big.");
            }

            // Return the data if it is ready, otherwize null.
            if (messageSize == buffer.Count)
            {
                return buffer.ToArray();
            }
            else {
                return null;
            }
        }
    }

    public class ConnectServerResponseBytesFiller : IMessageBytesFiller
    {
        public override ushort EventType()
        {
            return NetworkMessage.EventType_ConnectServer;
        }

        public override int GetFixedPartMessageSize()
        {
            return 2 + 20 + 8 + 1;
        }

        protected override int GetVariablePartMessageSize(byte[] fixedPart)
        {
            return 0;
        }
    }

    public class SampleRoomsResponseBytesFiller : IMessageBytesFiller
    {
        public override ushort EventType()
        {
            return NetworkMessage.EventType_SampleRooms;            
        }

        public override int GetFixedPartMessageSize()
        {
            return 2 + 2 + 1;
        }

        protected override int GetVariablePartMessageSize(byte[] fixedPart)
        {
            ushort numberOfRooms = BitConverter.ToUInt16(fixedPart, 2);
            return numberOfRooms * 4;
        }
    }
}
