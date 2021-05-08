
using System;

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
    }
}