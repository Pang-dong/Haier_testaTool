using System;
using System.Collections.Generic;
using System.Linq;

namespace Haier_E246_TestTool.Protocols
{
    public class PacketParser
    {
        private List<byte> _buffer = new List<byte>();
        private const int MinPacketSize = 7; // Magic(2) + CRC(2) + Cmd(1) + Len(2)
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

                // 2. 解析长度 (小端序)
                ushort dataLen = (ushort)(_buffer[5] | (_buffer[6] << 8));
                int totalPacketLen = MinPacketSize + dataLen;
                logger.Debug(BitConverter.ToString(newData));
                // 数据不够，等待下次
                if (_buffer.Count < totalPacketLen) break;

                // 3. 获取完整包数据
                byte[] packetBytes = _buffer.GetRange(0, totalPacketLen).ToArray();

                // 4. 获取接收到的 CRC (小端序)
                ushort receivedCrc = (ushort)(packetBytes[2] | (packetBytes[3] << 8));

                // 5. 【关键修改】提前获取 Command ID
                byte cmdId = packetBytes[4];
                ushort calcCrc;

                if (cmdId == 0x00)
                {
                    // Cmd 0x00 使用初始值 0x0000 的算法
                    calcCrc = Crc16.ComputeChecksum(packetBytes, 4, 1 + 2 + dataLen);
                }
                else
                {
                    // 其他命令 (如 0x03) 使用初始值 0xFFFF 的算法
                    calcCrc = Crc16.ComputeChecksum(packetBytes, 4, 1 + 2 + dataLen);
                }

                // 7. 对比校验
                if ((byte)receivedCrc == (byte)calcCrc)
                {
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
                    // 校验失败
                    // System.Diagnostics.Debug.WriteLine($"CRC Fail: Cmd={cmdId:X2}, Recv={receivedCrc:X4}, Calc={calcCrc:X4}");
                    _buffer.RemoveAt(0);
                }
            }

            return packets;
        }
    }
}