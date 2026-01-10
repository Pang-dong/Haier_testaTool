using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.LH;

using Haier_E246_TestTool.LH; // 删除这行，看起来是误写的
using Haier_E246_TestTool.Services; // 【关键修复】引用 SerialPortService
using log4net;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // 1. 获取 Log4Net 实例
        private static readonly ILog _fileLogger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SerialPortService _serialService;
        private readonly object _uiLogLock = new object();

        public MainViewModel()
        {
            _serialService = new SerialPortService();
            _serialService.DataReceived += OnSerialDataReceived;

            // 初始化时获取端口
            AvailablePorts = new ObservableCollection<string>(_serialService.GetAvailablePorts());
            // 如果有端口，默认选中第一个
            if (AvailablePorts.Count > 0) SelectedPort = AvailablePorts[0];

            Logs = new ObservableCollection<LogModel>();

            // 启用跨线程集合更新
            BindingOperations.EnableCollectionSynchronization(Logs, _uiLogLock);
        }

        [ObservableProperty]
        private ObservableCollection<string> _availablePorts;

        [ObservableProperty]
        private string _selectedPort;

        [ObservableProperty]
        private int _baudRate = 9600;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendCommand))] // 连接状态改变时，同时也刷新发送按钮的状态
        private bool _isConnected;

        [ObservableProperty]
        private ObservableCollection<LogModel> _logs;

        // 【关键修复】补全 XAML 中绑定的刷新端口命令
        [RelayCommand]
        private void RefreshPorts()
        {
            AvailablePorts = new ObservableCollection<string>(_serialService.GetAvailablePorts());
            if (AvailablePorts.Count > 0) SelectedPort = AvailablePorts[0];
            AddLog("刷新串口列表完成", "INFO");
        }

        [RelayCommand]
        private void Connect()
        {
            if (IsConnected)
            {
                _serialService.Disconnect();
                IsConnected = false;
                AddLog("用户断开串口连接", "INFO");
            }
            else
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    AddLog("请先选择端口", "ERROR");
                    return;
                }

                if (_serialService.Connect(SelectedPort, BaudRate))
                {
                    IsConnected = true;
                    AddLog($"串口 {SelectedPort} 打开成功", "INFO");
                }
                else
                {
                    AddLog($"串口 {SelectedPort} 打开失败", "ERROR");
                }
            }
        }

        [RelayCommand(CanExecute = nameof(IsConnected))]
        private async Task Send(string commandCode)
        {
            if (string.IsNullOrEmpty(commandCode)) return;

            await Task.Run(() =>
            {
                try
                {
                    _serialService.SendData(commandCode);
                    AddLog($"TX: {commandCode}", "TX");
                }
                catch (Exception ex)
                {
                    AddLog($"发送失败: {ex.Message}", "ERROR");
                }
            });
        }

        private void OnSerialDataReceived(string data)
        {
            AddLog($"RX: {data.Trim()}", "RX");
        }

        private void AddLog(string message, string type)
        {
            // 1. 写文件
            switch (type)
            {
                case "ERROR": _fileLogger.Error(message); break;
                case "TX":
                case "RX": _fileLogger.Debug(message); break;
                default: _fileLogger.Info(message); break;
            }

            // 2. 写界面
            lock (_uiLogLock)
            {
                if (Logs.Count > 1000) Logs.RemoveAt(0);
                Logs.Add(new LogModel
                {
                    Timestamp = DateTime.Now,
                    Message = message,
                    Type = type
                });
            }
        }
    }
}