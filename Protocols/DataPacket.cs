using System;
using System.Collections.Generic;
using System.Linq;

namespace Haier_E246_TestTool.Protocols
{
    public class DataPacket
    {
        public const ushort MagicNumber = 0x5AA5; // 幻数
        public const int HeaderSize = 7; // Magic(2) + CRC(2) + CmdId(1) + Len(2)

        public byte CommandId { get; set; }
        public byte[] Payload { get; set; }

        public DataPacket(byte commandId, byte[] payload = null)
        {
            CommandId = commandId;
            Payload = payload ?? new byte[0];
        }

        // 将对象转换为用于发送的字节数组
        public byte[] ToBytes()
        {
            // 1. 准备要参与 CRC 计算的部分：[CmdID] + [Len] + [Data]
            // Len 是 Payload 的长度
            ushort payloadLength = (ushort)Payload.Length;

            // 计算部分的总长度 = 1(Cmd) + 2(Len) + N(Payload)
            int bodyLength = 1 + 2 + Payload.Length;
            byte[] bodyBytes = new byte[bodyLength];

            int offset = 0;
            // CmdID (1 byte)
            bodyBytes[offset++] = CommandId;

            // Length (2 bytes, Little Endian
            bodyBytes[offset++] = (byte)((payloadLength >> 8) & 0xFF);
            bodyBytes[offset++] = (byte)(payloadLength & 0xFF);
            
            // Payload (N bytes)
            if (Payload.Length > 0)
            {
                Array.Copy(Payload, 0, bodyBytes, offset, Payload.Length);
            }

            // 2. 计算 CRC
            ushort crc = Crc16.ComputeChecksum(bodyBytes);

            // 3. 组装最终包：[Magic] + [CRC] + [Body]
            // 最终长度 = 2(Magic) + 2(CRC) + BodyLength
            byte[] fullPacket = new byte[2 + 2 + bodyBytes.Length];
            offset = 0;

            // Magic (2 bytes, Little Endian: 0xA5, 0x5A)
            
            fullPacket[offset++] = (byte)((MagicNumber >> 8) & 0xFF);
            fullPacket[offset++] = (byte)(MagicNumber & 0xFF);

            // CRC (2 bytes, Little Endian)
            fullPacket[offset++] = (byte)((crc >> 8) & 0xFF); // High Byte
            fullPacket[offset++] = (byte)(crc & 0xFF);        // Low Byte

            // Body
            Array.Copy(bodyBytes, 0, fullPacket, offset, bodyBytes.Length);

            return fullPacket;
        }
    }
}
