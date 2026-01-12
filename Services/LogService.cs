using System;
using System.IO;

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

        // 实现接口，加入 saveToFile 参数
        public void WriteLog(string message, LogType type, bool saveToFile)
        {
            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMsg = $"[{timeStr}] [{type}] {message}";

            // 1. 永远通知 UI 显示（界面日志要全）
            OnNewLog?.Invoke(formattedMsg, type);

            // 2. 只有当要求保存时，才写入文件（文件日志要精简）
            if (saveToFile)
            {
                lock (_lockObj)
                {
                    try
                    {
                        // 按天生成文件名
                        string fileName = Path.Combine(_logPath, $"{DateTime.Now:yyyyMMdd}.log");
                        File.AppendAllText(fileName, formattedMsg + Environment.NewLine);
                    }
                    catch { /* 忽略占用错误 */ }
                }
            }
        }
    }
}