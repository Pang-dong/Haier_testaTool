using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.Services;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Haier_E246_TestTool.Protocols;
using System;
using Haier_E246_TestTool.Models;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SerialPortService _serialService;
        private readonly object _logLock = new object();
        private readonly PacketParser _parser = new PacketParser();
        public AppConfig CurrentConfig { get; private set; }

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
            set
            {
                if (SetProperty(ref _selectedPort, value))
                {
                    // 当用户在界面改变选择时，实时更新到 Config 对象中
                    if (CurrentConfig != null) CurrentConfig.PortName = value;
                }
            }
        }

        private int _baudRate = 9600;
        public int BaudRate
        {
            get => _baudRate;
            set
            {
                if (SetProperty(ref _baudRate, value))
                {
                    if (CurrentConfig != null) CurrentConfig.BaudRate = value;
                }
            }
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
            set
            {
                if (SetProperty(ref _stationName, value))
                {
                    // 同步更新到 Config 的 LastStation 字段
                    if (CurrentConfig != null) CurrentConfig.StationName = value;
                }
            }
        }

        public ObservableCollection<string> UiLogs { get; } = new ObservableCollection<string>();

        public MainViewModel()
        {
        }

        // 实际运行时调用的构造函数
        public MainViewModel(SerialPortService serialService, AppConfig config)
        {
            _serialService = serialService;
            CurrentConfig = config;
            StationName = config.StationName;
            BaudRate = config.BaudRate;
            BindingOperations.EnableCollectionSynchronization(UiLogs, _logLock);
            RefreshPorts();
            _serialService.DataReceived += HandleDataReceived;
        }
        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        /// <param name="rawData"></param>
        private void HandleDataReceived(byte[] rawData)
        {
            // 使用解析器处理碎片数据
            var packets = _parser.ProcessChunk(rawData);

            foreach (var packet in packets)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AddLog($"收到命令: {packet.CommandId:X2}, 数据长度: {packet.Payload.Length}");
                    switch (packet.CommandId)
                    {
                        case 0x01: // 握手返回
                            AddLog("握手成功！");
                            break;
                    }
                });
            }
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

            byte cmdId = 0;
            byte[] paramsData = new byte[0];

            switch (commandTag)
            {
                case "Cmd1": // 握手
                    cmdId = 0x01;
                    paramsData = new byte[] { 0x01 }; // 示例参数
                    break;
                case "Cmd2": // 读取ID
                    cmdId = 0x02;
                    break;
                case "Cmd3": // 功能测试
                    cmdId = 0x03;
                    // 假设需要传个 Int16 参数 1000
                    short val = 1000;
                    paramsData = BitConverter.GetBytes(val);
                    break;
                // ... 其他命令
                default: return;
            }

            // 1. 创建包
            var packet = new DataPacket(cmdId, paramsData);

            // 2. 序列化成字节数组
            byte[] finalBytes = packet.ToBytes();

            // 3. 发送
            _serialService.SendData(finalBytes);
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