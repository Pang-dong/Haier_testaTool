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

        public string Password { get; set; } = ""; // 提示：生产环境建议加密存储

        public bool IsRememberMe { get; set; } = false;

        public bool IsMesMode { get; set; } = true;

        public string FtpIp { get; set; } = "192.168.88.144";

        public string PortName { get; set; } = "";

        public int BaudRate { get; set; } = 9600;
        public string StationName { get; set; } = "Station-001";

    }
}
