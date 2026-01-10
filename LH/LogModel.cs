using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.LH
{
    public class LogModel
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // "INFO", "ERROR", "RX", "TX"

        public string FullMessage => $"[{Timestamp:HH:mm:ss.fff}] [{Type}] {Message}";
    }
}
