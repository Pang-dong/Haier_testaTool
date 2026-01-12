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
            // log4net 初始化已经在 AssemblyInfo.cs 里配置了 [assembly: XmlConfigurator(Watch = true)]
            // 所以这里不需要额外代码
        }

        public void WriteLog(string message, LogType type, bool saveToFile)
        {
            // 1. 触发 UI 更新事件 (这一步保持不变，用于界面显示)
            // 为了界面好看，这里可以简单拼接一下，或者直接传原始 msg
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