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
                // 1. 【修正】检查 Magic Number (Big Endian: 0x5AA5)
                // 大端序：先收到 0x5A (High)，后收到 0xA5 (Low)
                if (_buffer[0] != 0x5A || _buffer[1] != 0xA5)
                {
                    _buffer.RemoveAt(0); // 头不对，滑窗
                    continue;
                }

                // 2. 【修正】解析长度 (Length) 为大端序
                // Index 5 是高位(High), Index 6 是低位(Low)
                ushort dataLen = (ushort)((_buffer[5] << 8) | _buffer[6]);

                int totalPacketLen = MinPacketSize + dataLen;

                // 数据不够，等待下次
                if (_buffer.Count < totalPacketLen) break;

                byte[] packetBytes = _buffer.GetRange(0, totalPacketLen).ToArray();

                // 3. 【修正】解析接收到的 CRC 为大端序
                // Index 2 是高位(High), Index 3 是低位(Low)
                ushort receivedCrc = (ushort)((packetBytes[2] << 8) | packetBytes[3]);

                // 4. 计算 CRC (范围: Cmd + Len + Payload)
                // 从 Index 4 开始，长度 = 1(Cmd) + 2(Len) + dataLen
                ushort calcCrc = Crc16.ComputeChecksum(packetBytes, 4, 1 + 2 + dataLen);

                if (receivedCrc == calcCrc)
                {
                    byte cmdId = packetBytes[4];
                    byte[] payload = new byte[dataLen];
                    if (dataLen > 0)
                    {
                        // Payload 从 Index 7 开始
                        Array.Copy(packetBytes, 7, payload, 0, dataLen);
                    }

                    packets.Add(new DataPacket(cmdId, payload));
                    _buffer.RemoveRange(0, totalPacketLen);
                }
                else
                {
                    // CRC 失败，移除头，继续寻找
                    // Console.WriteLine($"CRC Error: Recv {receivedCrc:X4} != Calc {calcCrc:X4}");
                    _buffer.RemoveAt(0);
                }
            }

            return packets;
        }
    }
}