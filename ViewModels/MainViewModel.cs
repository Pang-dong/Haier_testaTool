using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.LH;
using Haier_E246_TestTool.Services;
using log4net;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public partial class MainViewModel : ObservableObject
        {
            private readonly SerialPortService _serialService;
            private readonly ILogService _logService;
            private readonly object _logLock = new object();

            // 绑定属性
            [ObservableProperty] private ObservableCollection<string> _comPorts;
            [ObservableProperty] private string _selectedPort;
            [ObservableProperty] private int _baudRate = 9600;
            [ObservableProperty] private bool _isConnected;

            // 界面显示的日志列表
            public ObservableCollection<string> UiLogs { get; } = new ObservableCollection<string>();

            public MainViewModel()
            {
                // 初始化服务
                _logService = new LogService();
                _serialService = new SerialPortService(_logService);

                // 绑定日志事件
                _logService.OnNewLog += HandleNewLog;

                // 绑定串口数据事件 (后续具体协议在这里处理)
                _serialService.DataReceived += HandleDataReceived;

                // 允许跨线程更新集合 (WPF特定的线程安全集合方案)
                BindingOperations.EnableCollectionSynchronization(UiLogs, _logLock);

                RefreshPorts();
            }

            [RelayCommand]
            private void RefreshPorts()
            {
                ComPorts = new ObservableCollection<string>(_serialService.GetAvailablePorts());
                if (ComPorts.Count > 0) SelectedPort = ComPorts[0];
            }

            [RelayCommand]
            private void ToggleConnection()
            {
                if (IsConnected)
                {
                    _serialService.Close();
                    IsConnected = false;
                }
                else
                {
                    if (string.IsNullOrEmpty(SelectedPort)) return;
                    bool success = _serialService.Open(SelectedPort, BaudRate);
                    IsConnected = success;
                }
            }

            // 命令发送按钮逻辑
            [RelayCommand]
            private void SendCommand(string commandTag)
            {
                // 这里仅仅是框架演示，后续你可以在这里实现具体的协议封装
                byte[] dataToSend = new byte[0];

                switch (commandTag)
                {
                    case "Cmd1": dataToSend = new byte[] { 0x01, 0x02 }; break; // 示例：握手
                    case "Cmd2": dataToSend = new byte[] { 0xA0, 0xFF }; break; // 示例：读ID
                    case "Cmd3": dataToSend = new byte[] { 0xB1, 0x00 }; break; // 示例：开始测试
                    case "Cmd4": dataToSend = new byte[] { 0xC1 }; break;       // 示例：复位
                    default:
                        _logService.WriteLog("未定义的命令", LogType.Warning);
                        return;
                }

                _serialService.SendData(dataToSend);
            }

            [RelayCommand]
            private void ClearLog()
            {
                UiLogs.Clear();
            }

            // 处理新日志（线程安全已经在 EnableCollectionSynchronization 中处理，但为了保险依然建议注意）
            private void HandleNewLog(string msg, LogType type)
            {
                // 如果日志量非常大，建议限制UI显示的行数，防止界面卡顿
                lock (_logLock)
                {
                    if (UiLogs.Count > 1000) UiLogs.RemoveAt(0);
                    UiLogs.Add(msg);
                }
            }

            private void HandleDataReceived(byte[] data)
            {
                // TODO: 在这里解析具体的通信协议
                // 解析完后可以通过 Application.Current.Dispatcher.Invoke 更新界面上的具体状态指示灯等
            }
        }
    }