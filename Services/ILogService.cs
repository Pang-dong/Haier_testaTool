using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Services
{
    public enum LogType { Info, Warning, Error, Rx, Tx }

    public interface ILogService
    {
        void WriteLog(string message, LogType type = LogType.Info);
        event Action<string, LogType> OnNewLog; // 用于通知UI更新
    }
}
