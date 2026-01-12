using System;
using System.Collections.Generic;
using System.Linq;

namespace Haier_E246_TestTool.Protocols
{
    public class PacketParser
    {
        private List<byte> _buffer = new List<byte>();
        private const int MinPacketSize = 7; // Magic(2) + CRC(2) + Cmd(1) + Len(2)

        public List<DataPacket> ProcessChunk(byte[] newData)
        {
            var packets = new List<DataPacket>();
            if (newData == null || newData.Length == 0) return packets;

            _buffer.AddRange(newData);

            while (_buffer.Count >= MinPacketSize)
            {
                // 1. 检查 Magic Number (固定 0x5AA5)
                if (_buffer[0] != 0x5A || _buffer[1] != 0xA5)
                {
                    _buffer.RemoveAt(0);
                    continue;
                }
                ushort dataLen = (ushort)(_buffer[5] | (_buffer[6] << 8));

                int totalPacketLen = MinPacketSize + dataLen;

                // 数据不够，等待下次
                if (_buffer.Count < totalPacketLen) break;

                byte[] packetBytes = _buffer.GetRange(0, totalPacketLen).ToArray();
                ushort receivedCrc = (ushort)(packetBytes[2] | (packetBytes[3] << 8));
                ushort calcCrc = Crc16.ComputeChecksum(packetBytes, 4, 1 + 2 + dataLen);

                if (receivedCrc == calcCrc)
                {
                    byte cmdId = packetBytes[4];
                    byte[] payload = new byte[dataLen];
                    if (dataLen > 0)
                    {
                        Array.Copy(packetBytes, 7, payload, 0, dataLen);
                    }

                    packets.Add(new DataPacket(cmdId, payload));
                    _buffer.RemoveRange(0, totalPacketLen);
                }
                else
                {
                    // CRC 失败，移除头字节继续滑动窗口（避免丢弃整个包，因为可能只是头误判）
                    // 也可以选择 _buffer.RemoveRange(0, 2); 稍微激进一点
                    _buffer.RemoveAt(0);
                }
            }

            return packets;
        }
    }
}