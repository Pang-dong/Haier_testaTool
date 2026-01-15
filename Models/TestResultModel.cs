using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Models
{
    public  class TestResultModel
    {
        /// <summary>
        /// 读取ID/MAC (Cmd2)
        /// </summary>
        public int Test_ReadMac { get; set; } = 0;

        /// <summary>
        /// 获取Camera版本
        /// </summary>
        public int Test_Camera_Version { get; set; } = 0;

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 获取wifi版本
        /// </summary>
        public int Test_WIFI_VERSION { get; set; } = 0;
        /// <summary>
        /// Linsence烧录
        /// </summary>
        public int Tset_Linsence {  get; set; } = 0;    

        /// <summary>
        /// 最终结论 (所有项都为1才算Pass)
        /// </summary>
        public bool IsTotalPass => Test_ReadMac == 1 && Test_Camera_Version == 1&& Test_WIFI_VERSION==1;

        public int Test_Handshake { get; set; }
        /// <summary>
        /// 写号结果
        /// </summary>
        public int YH_Result { get; set; } = 0;
    }
    public partial class OuterResponse
    {
        public int code { get; set; }
        public string status { get; set; }
        public string resultMsg { get; set; }  // 注意：这个字段包含的是JSON字符串
    }
}
