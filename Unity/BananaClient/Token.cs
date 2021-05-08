using System;
using System.Collections.Generic;

namespace BananaClient
{  
    public class Token
    {
        public const ushort TOKEN_TYPE_BOOL   = 0x0002;
        public const ushort TOKEN_TYPE_BYTE   = 0x0003;
        public const ushort TOKEN_TYPE_CHAR   = 0x0004;
        public const ushort TOKEN_TYPE_DOUBLE = 0x0005;
        public const ushort TOKEN_TYPE_SINGLE = 0x0006;
        public const ushort TOKEN_TYPE_INT32  = 0x0007;
        public const ushort TOKEN_TYPE_UINT32 = 0x0008;
        public const ushort TOKEN_TYPE_INT64  = 0x0009;
        public const ushort TOKEN_TYPE_UINT64 = 0x000A;
        public const ushort TOKEN_TYPE_INT16  = 0x000B;
        public const ushort TOKEN_TYPE_UINT16 = 0x000C;

        private static Dictionary<ushort, int> PrimitiveSize = new Dictionary<ushort, int>()
        {
            {TOKEN_TYPE_BOOL, 1},
            {TOKEN_TYPE_BYTE, 1},
            {TOKEN_TYPE_CHAR, 1},
            {TOKEN_TYPE_DOUBLE, 8},
            {TOKEN_TYPE_SINGLE, 4},
            {TOKEN_TYPE_INT32, 4},
            {TOKEN_TYPE_UINT32, 4},
            {TOKEN_TYPE_INT64, 8},
            {TOKEN_TYPE_UINT64, 8},
            {TOKEN_TYPE_INT16, 2},
            {TOKEN_TYPE_UINT16, 2},
        };

        private ushort tokenType;
        private object tokenContent;

        private Token(ushort tokenType, object tokenContent)
        {
            this.tokenType = tokenType;
            this.tokenContent = tokenContent;
        }

        public static int Read(byte[] src, int offset, ref Token result)
        {
            ushort tokenType = BitConverter.ToUInt16(src, offset);
            offset += 2;
            if (tokenType >= 0x0002 && tokenType <= 0x000C)
            {
                result = new Token(tokenType, ConvertPrimitive(tokenType, src, offset));                
                return offset + PrimitiveSize[tokenType];
            }
            
            if (tokenType >= 0x0012 && tokenType <= 0x001C)
            {
                ushort primitiveType = (ushort)(tokenType - 0x0010);
                uint array_length = BitConverter.ToUInt32(src, offset);
                offset += 4;
                var elmSize = PrimitiveSize[tokenType];
                
                object array = AllocateArrayOfPrimitives(primitiveType, elmSize);
                for (int i = 0; i < array_length; i++)
                {
                    Append(primitiveType, array, i, ConvertPrimitive(primitiveType, src, offset));
                    offset += elmSize;
                }
                result = new Token(tokenType, array);
                return offset;
            }

            throw new InvalidCastException("Unsupported Token Type: " +
                       BitConverter.ToString(BitConverter.GetBytes(tokenType)));
        }

        private static void Append(ushort primitiveType, object array, int index, object elm)
        {
            switch (primitiveType)
            {
                case TOKEN_TYPE_BOOL:
                    ((bool[])array)[index] = (bool)elm;
                    return;
                case TOKEN_TYPE_BYTE:
                    ((byte[])array)[index] = (byte)elm;
                    return;
                case TOKEN_TYPE_CHAR:
                    ((char[])array)[index] = (char)elm;
                    return;
                case TOKEN_TYPE_DOUBLE:
                    ((double[])array)[index] = (double)elm;
                    return;
                case TOKEN_TYPE_SINGLE:
                    ((float[])array)[index] = (float)elm;
                    return;
                case TOKEN_TYPE_INT32:
                    ((int[])array)[index] = (int)elm;
                    return;
                case TOKEN_TYPE_UINT32:
                    ((uint[])array)[index] = (uint)elm;
                    return;
                case TOKEN_TYPE_INT64:
                    ((long[])array)[index] = (long)elm;
                    return;
                case TOKEN_TYPE_UINT64:
                    ((ulong[])array)[index] = (ulong)elm;
                    return;
                case TOKEN_TYPE_INT16:
                    ((short[])array)[index] = (short)elm;
                    return;
                case TOKEN_TYPE_UINT16:
                    ((ushort[])array)[index] = (ushort)elm;
                    return;
                default:
                    throw new InvalidCastException("Unsupported Token Type: " +
                        BitConverter.ToString(BitConverter.GetBytes(primitiveType)));
            }
        }

        private static object AllocateArrayOfPrimitives(ushort primitveType, int elmSize)
        {
            switch (primitveType)
            {
                case TOKEN_TYPE_BOOL:
                    return new bool[elmSize];
                case TOKEN_TYPE_BYTE:
                    return new byte[elmSize];
                case TOKEN_TYPE_CHAR:
                    return new char[elmSize];
                case TOKEN_TYPE_DOUBLE:
                    return new double[elmSize];
                case TOKEN_TYPE_SINGLE:
                    return new float[elmSize];
                case TOKEN_TYPE_INT32:
                    return new int[elmSize];
                case TOKEN_TYPE_UINT32:
                    return new uint[elmSize];
                case TOKEN_TYPE_INT64:
                    return new long[elmSize];
                case TOKEN_TYPE_UINT64:
                    return new ulong[elmSize];
                case TOKEN_TYPE_INT16:
                    return new short[elmSize];
                case TOKEN_TYPE_UINT16:
                    return new ushort[elmSize];
                default:
                    throw new InvalidCastException("Unsupported Token Type: " +
                        BitConverter.ToString(BitConverter.GetBytes(primitveType)));
            }
        }

        public static object ConvertPrimitive(ushort tokenType, byte[] src, int offset)
        {
            switch (tokenType)
            {
                case TOKEN_TYPE_BOOL:
                    return BitConverter.ToBoolean(src, offset);
                case TOKEN_TYPE_BYTE:
                    return src[0];
                case TOKEN_TYPE_CHAR:
                    return BitConverter.ToChar(src, offset);
                case TOKEN_TYPE_DOUBLE:
                    return BitConverter.ToDouble(src, offset);
                case TOKEN_TYPE_SINGLE:
                    return BitConverter.ToSingle(src, offset);
                case TOKEN_TYPE_INT32:
                    return BitConverter.ToInt32(src, offset);
                case TOKEN_TYPE_UINT32:
                    return BitConverter.ToUInt32(src, offset);
                case TOKEN_TYPE_INT64:
                    return BitConverter.ToInt64(src, offset);
                case TOKEN_TYPE_UINT64:
                    return BitConverter.ToUInt64(src, offset);
                case TOKEN_TYPE_INT16:
                    return BitConverter.ToInt16(src, offset);
                case TOKEN_TYPE_UINT16:
                    return BitConverter.ToUInt16(src, offset);
                default:
                    throw new InvalidCastException("Unsupported Token Type: " +
                        BitConverter.ToString(BitConverter.GetBytes(tokenType)));
            }
        }

        // copy the bytes from "start" (inclusive) to "end" (exclusive).
        private static byte[] Slice(byte[] src, int start, int end)
        {
            int len = end - start;
            byte[] sliced = new byte[len];
            for (int i = 0; i < len; i++)
                sliced[i] = src[start + i];
            return sliced;
        }
    }

}
