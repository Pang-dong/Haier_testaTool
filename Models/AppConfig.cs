using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Haier_E246_TestTool.Models
{
    public class AppConfig
    {
        public string UserName { get; set; } = "";

        public string Password { get; set; } = ""; 

        public bool IsRememberMe { get; set; } = false;

        public bool IsMesMode { get; set; } = true;

        public string FtpIp { get; set; } = "192.168.88.144";

        public string PortName { get; set; } = "";

        public int BaudRate { get; set; } = 9600;
        public string StationName { get; set; } = "";
        public string BurnPort { get; set; } = "COM3";
        public string BurnBaud { get; set; } = "921600";
        public string BkLoaderPath { get; set; } = @"app\bk_loader.exe";
        public string BurnSourceDir { get; set; }// 待烧录路径
        public string BurnTargetDir { get; set; } // 已烧录路径
        public string MainBin { get; set; } = "all-app.bin"; // 主程序文件名
        public string LittlefsBin { get; set; } = "littlefs.bin"; // littlefs文件名
        public string LastUser { get; set; } = ""
;
        public string LastStationType { get; set; } = "测试工站"; // 记录上次选的工站

    }
}
