using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Services
{
    public class LogService : ILogService
    {
        public event Action<string, LogType> OnNewLog;
        private readonly object _lockObj = new object();
        private readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        public LogService()
        {
            if (!Directory.Exists(_logPath)) Directory.CreateDirectory(_logPath);
        }

        public void WriteLog(string message, LogType type)
        {
            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMsg = $"[{timeStr}] [{type}] {message}";

            // 1. 触发事件通知UI (UI层需要自己处理跨线程)
            OnNewLog?.Invoke(formattedMsg, type);

            // 2. 写入本地文件 (加锁保证多线程安全)
            lock (_lockObj)
            {
                try
                {
                    string fileName = Path.Combine(_logPath, $"{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(fileName, formattedMsg + Environment.NewLine);
                }
                catch { /* 忽略文件占用错误或处理 */ }
            }
        }
    }
}
