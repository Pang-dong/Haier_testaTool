using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Models
{
    public  class TestResult
    {
        /// <summary>
        /// 读取ID/MAC (Cmd2)
        /// </summary>
        public int Test_ReadMac { get; set; } = 0;

        /// <summary>
        /// 功能测试 (Cmd3)
        /// </summary>
        public int Test_Function { get; set; } = 0;

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最终结论 (所有项都为1才算Pass)
        /// </summary>
        public bool IsTotalPass => Test_ReadMac == 1 && Test_Function == 1;

        public int Test_Handshake { get; set; }
    }
}
