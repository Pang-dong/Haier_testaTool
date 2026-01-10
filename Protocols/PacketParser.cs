using System;
using System.Collections.Generic;
using System.Linq;

namespace Haier_E246_TestTool.Protocols
{
    public class PacketParser
    {
        // 接收缓冲区
        private List<byte> _buffer = new List<byte>();

        // 最小包长：Magic(2) + CRC(2) + Cmd(1) + Len(2) = 7字节
        private const int MinPacketSize = 7;

        /// <summary>
        /// 将新接收到的数据推入解析器
        /// </summary>
        /// <param name="newData">串口收到的原始字节</param>
        /// <returns>解析出的完整数据包列表</returns>
        public List<DataPacket> ProcessChunk(byte[] newData)
        {
            var packets = new List<DataPacket>();
            if (newData == null || newData.Length == 0) return packets;

            // 1. 将新数据加入缓冲区
            _buffer.AddRange(newData);

            // 2. 循环尝试解析
            while (_buffer.Count >= MinPacketSize)
            {
                // 检查 Magic Number (Little Endian: 0xA5 0x5A)
                // 0x5AA5 -> Low byte 0xA5, High byte 0x5A
                if (_buffer[0] != 0xA5 || _buffer[1] != 0x5A)
                {
                    // 头部不对，移除第一个字节，继续寻找下一个头
                    _buffer.RemoveAt(0);
                    continue;
                }

                // 此时已经匹配到幻数，开始解析头部信息
                // 结构: [Magic:2][CRC:2][Cmd:1][Len:2][Payload:N]

                // 读取长度字段 (Index 5 和 6)
                ushort dataLen = (ushort)(_buffer[5] | (_buffer[6] << 8));

                // 计算当前包应有的总长度
                int totalPacketLen = MinPacketSize + dataLen;

                // 如果缓冲区数据不够一个完整的包，停止解析，等待下一次数据到来
                if (_buffer.Count < totalPacketLen)
                {
                    break;
                }

                // 数据足够，提取完整的一帧
                byte[] packetBytes = _buffer.GetRange(0, totalPacketLen).ToArray();

                // --- 校验 CRC ---
                // CRC 位于 index 2,3
                ushort receivedCrc = (ushort)(packetBytes[2] | (packetBytes[3] << 8));

                // 计算 CRC 的范围：从 CmdID(index 4) 开始，到包结束
                // 长度 = 1(Cmd) + 2(Len) + N(Payload)
                int bodyLen = 1 + 2 + dataLen;
                ushort calcCrc = Crc16.ComputeChecksum(packetBytes, 4, bodyLen);

                if (receivedCrc == calcCrc)
                {
                    // 校验通过，提取数据
                    byte cmdId = packetBytes[4];
                    byte[] payload = new byte[dataLen];
                    if (dataLen > 0)
                    {
                        Array.Copy(packetBytes, 7, payload, 0, dataLen);
                    }

                    packets.Add(new DataPacket(cmdId, payload));

                    // 从缓冲区移除已处理的包
                    _buffer.RemoveRange(0, totalPacketLen);
                }
                else
                {
                    _buffer.RemoveAt(0);
                }
            }

            return packets;
        }
    }
}