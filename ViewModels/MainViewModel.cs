using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.Services;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SerialPortService _serialService;
        private readonly object _logLock = new object();

        // 绑定属性
        [ObservableProperty] private ObservableCollection<string> _comPorts;
        [ObservableProperty] private string _selectedPort;
        [ObservableProperty] private int _baudRate = 9600;
        [ObservableProperty] private bool _isConnected;
        [ObservableProperty] private string _stationName; // 工位名称

        // 界面显示的日志列表
        public ObservableCollection<string> UiLogs { get; } = new ObservableCollection<string>();

        // 构造函数：这里接收外部传进来的服务
        public MainViewModel(SerialPortService serialService, string stationName)
        {
            _serialService = serialService;
            StationName = stationName;

            // 开启多线程集合同步（防止日志报错）
            BindingOperations.EnableCollectionSynchronization(UiLogs, _logLock);

            RefreshPorts();
        }

        // 供外部调用的加日志方法
        public void AddLog(string msg)
        {
            lock (_logLock)
            {
                // 限制显示最近1000条，防止卡顿
                if (UiLogs.Count > 1000) UiLogs.RemoveAt(0);
                UiLogs.Add(msg);
            }
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

        [RelayCommand]
        private void SendCommand(string commandTag)
        {
            byte[] dataToSend = new byte[0];

            switch (commandTag)
            {
                case "Cmd1": dataToSend = new byte[] { 0x01, 0x02 }; break;
                case "Cmd2": dataToSend = new byte[] { 0xA0, 0xFF }; break;
                case "Cmd3": dataToSend = new byte[] { 0xB1, 0x00 }; break;
                case "Cmd4": dataToSend = new byte[] { 0xC1 }; break;
                default: return;
            }

            _serialService.SendData(dataToSend);
        }

        [RelayCommand]
        private void ClearLog()
        {
            UiLogs.Clear();
        }

        public void Cleanup()
        {
            _serialService?.Close();
        }
    }
}