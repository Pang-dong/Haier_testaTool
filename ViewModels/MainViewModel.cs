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

        private ObservableCollection<string> _comPorts;
        public ObservableCollection<string> ComPorts
        {
            get => _comPorts;
            set => SetProperty(ref _comPorts, value);
        }

        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        private int _baudRate = 9600;
        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private string _stationName;
        public string StationName
        {
            get => _stationName;
            set => SetProperty(ref _stationName, value);
        }

        public ObservableCollection<string> UiLogs { get; } = new ObservableCollection<string>();

        public MainViewModel()
        {
        }

        // 实际运行时调用的构造函数
        public MainViewModel(SerialPortService serialService, string stationName)
        {
            _serialService = serialService;
            StationName = stationName; // 此时 StationName 属性已手动定义，不会报错了

            BindingOperations.EnableCollectionSynchronization(UiLogs, _logLock);
            RefreshPorts();
        }

        // 添加日志的方法
        public void AddLog(string msg)
        {
            lock (_logLock)
            {
                if (UiLogs.Count > 1000) UiLogs.RemoveAt(0);
                UiLogs.Add(msg);
            }
        }

        [RelayCommand]
        private void RefreshPorts()
        {
            if (_serialService == null) return;
            ComPorts = new ObservableCollection<string>(_serialService.GetAvailablePorts());
            if (ComPorts.Count > 0) SelectedPort = ComPorts[0];
        }

        [RelayCommand]
        private void ToggleConnection()
        {
            if (_serialService == null) return;

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
            if (_serialService == null) return;

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