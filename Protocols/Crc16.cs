using System;

namespace Haier_E246_TestTool.Protocols
{
    public static class Crc16
    {
        private const ushort Polynomial = 0xA001; // 0x8005 反转后的值
        private static readonly ushort[] Table = new ushort[256];

        static Crc16()
        {
            ushort value;
            ushort temp;
            for (ushort i = 0; i < Table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ Polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                Table[i] = value;
            }
        }

        public static ushort ComputeChecksum(byte[] bytes)
        {
            return ComputeChecksum(bytes, 0, bytes.Length);
        }

        public static ushort ComputeChecksum(byte[] bytes, int offset, int count)
        {
            ushort crc = 0x0000;
            for (int i = 0; i < count; ++i)
            {
                byte index = (byte)(crc ^ bytes[offset + i]);
                crc = (ushort)((crc >> 8) ^ Table[index]);
            }
            return crc;
        }
        // 替换 Crc16.cs 中的 ComputeCheckrecive 方法
        public static ushort ComputeCheckrecive(byte[] bytes, int offset, int count)
        {
            //ushort crc = 0xFFFF; // Modbus 初始值
            ushort crc = 0x0000;
            for (int i = 0; i < count; ++i)
            {
                byte index = (byte)(crc ^ bytes[offset + i]);
                crc = (ushort)((crc >> 8) ^ Table[index]);
            }
            return crc;
        }
    }
}