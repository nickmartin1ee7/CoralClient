using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftRcon
{
    public class Encoder
    {
        public const int HEADER_LENGTH = 10; // Does not include 4-byte message length.

        public static byte[] EncodeMessage(Message msg)
        {
            var bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(msg.Length));
            bytes.AddRange(BitConverter.GetBytes(msg.Id));
            bytes.AddRange(BitConverter.GetBytes((int)msg.Type));
            bytes.AddRange(Encoding.ASCII.GetBytes(msg.Body));
            bytes.AddRange(new byte[] { 0, 0 });

            return bytes.ToArray();
        }

        public static Message DecodeMessage(byte[] bytes)
        {
            var len = BitConverter.ToInt32(bytes, 0);
            var id = BitConverter.ToInt32(bytes, 4);
            var type = BitConverter.ToInt32(bytes, 8);
            var bodyLen = bytes.Length - (HEADER_LENGTH + 4);
            
            if (bodyLen <= 0)
                return new Message(len, id, (MessageType) type, string.Empty);

            var bodyBytes = new byte[bodyLen];

            Array.Copy(bytes, 12, bodyBytes, 0, bodyLen);
            Array.Resize(ref bodyBytes, bodyLen);

            return new Message(len, id, (MessageType)type, Encoding.UTF8.GetString(bodyBytes));
        }
    }
}