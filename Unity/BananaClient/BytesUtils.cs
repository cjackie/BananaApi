
using System;
using System.IO;

namespace BananaClient {
    public class BytesUtils {
        public  static string ByteArrayToHexString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba).Replace("-", "");
            string hexWithSpace = "";
            for (int i = 0; i < hex.Length; i++) {
                if (i != 0 && i % 4 == 0) {
                    hexWithSpace += " " + hex[i];
                } else {
                    hexWithSpace += hex[i];
                }
            }
            return hexWithSpace;
        }
        public static byte[] HexStringToByteArray(string hex)
        {
            // remove empty spaces
            string cleanHex = "";
            foreach (var c in hex) {
                if (c != ' ')
                    cleanHex += c;
            }

            int NumberChars = cleanHex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(cleanHex.Substring(i, 2), 16);
            return bytes;
        }

        // copy the bytes from "start" (inclusive) with len.
        public static byte[] Slice(byte[] src, int start, int len)
        {
            byte[] sliced = new byte[len];
            for (int i = 0; i < len; i++)
                sliced[i] = src[start + i];
            return sliced;
        }

        public static byte[] Zeros(int n)
        {
            if (n < 0)
            {
                throw new Exception("Negative byte array");
            }
            byte[] zeros = new byte[n];
            return zeros;
        }

        public static void ZeroOut(byte[] buffer, int len)
        {
            for (int i = 0; i < len; i++)
                buffer[i] = 0x00;
        }

        public static int WriteBytes(byte[] dest, int offset, byte[] data)
        {
            if (offset + data.Length >= dest.Length)
            {
                throw new Exception("data too big for dest.");
            }

            for (int i = 0; i < data.Length; i++)
            {
                dest[offset + i] = data[i];
            }
            return offset + data.Length;
        }

        public static int WriteArrayBytes(byte[] dest, int offset, char[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                offset = WritePrimitiveBytes(dest, offset, data[i]);
            }
            return offset;
        }

        public static int WritePrimitiveBytes(byte[] dest, int offset, char data)
        {
            if (offset >= dest.Length)
            {
                throw new Exception("data too big for dest.");
            }

            dest[offset++] = Convert.ToByte(data);
            return offset;
        }

        public static int WritePrimitiveBytes(byte[] dest, int offset, ushort data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return WriteBytes(dest, offset, bytes);
        }

        public static int WritePrimitiveBytes(byte[] dest, int offset, ulong data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return WriteBytes(dest, offset, bytes);
        }

        public static byte[] ReadBytes(Stream stream, int typeSize)
        {
            byte[] data = new byte[typeSize];
            for (int i = 0; i < typeSize; i++)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new Exception("byte is -1");
                data[i] = (byte)b;
            }
            // The network stream is agreed (assumed) on Big-Endian. So we
            // reverse data if the system is Little-Endian. 
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return data;
        }

        public static ushort ReadUInt16(Stream stream)
        {
            return BitConverter.ToUInt16(ReadBytes(stream, 2), 0);
        }

        public static byte ReadByte(Stream stream)
        {
            int b = stream.ReadByte();
            if (b == -1)
                throw new Exception("byte is -1");
            return (byte)b;
        }

        public static ulong ReadUInt64(Stream stream)
        {
            return BitConverter.ToUInt64(ReadBytes(stream, 8), 0);
        }

        public static short ReadInt16(Stream stream)
        {
            return BitConverter.ToInt16(ReadBytes(stream, 2), 0);
        }

    }
}