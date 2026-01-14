using log4net; // 引用 log4net
using System;

namespace Haier_E246_TestTool.Services
{
    public class LogService : ILogService
    {
        // 1. 获取 log4net 的 logger 实例
        private static readonly ILog log = LogManager.GetLogger(typeof(LogService));

        public event Action<string, LogType> OnNewLog;

        public LogService()
        {
        }

        public void WriteLog(string message, LogType type, bool saveToFile)
        {
            string uiMsg = $"[{DateTime.Now:HH:mm:ss.fff}] [{type}] {message}";
            OnNewLog?.Invoke(uiMsg, type);

            // 2. 使用 log4net 写入文件
            if (saveToFile)
            {
                switch (type)
                {
                    case LogType.Error:
                        log.Error(message);
                        break;
                    case LogType.Warning:
                        log.Warn(message);
                        break;
                    default:
                        log.Info(message); // Info, Rx, Tx 都记为 Info
                        break;
                }
            }
        }
    }
}