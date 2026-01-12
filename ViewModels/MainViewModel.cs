using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.Services;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Haier_E246_TestTool.Protocols;
using System;
using Haier_E246_TestTool.Models;
using System.Windows;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly SerialPortService _serialService;
        private readonly object _logLock = new object();
        private readonly PacketParser _parser = new PacketParser();
        private readonly ILogService _logService;
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
        public MainViewModel(SerialPortService serialService, AppConfig config,ILogService logService)
        {
            _serialService = serialService;
            _logService = logService;
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
            var packets = _parser.ProcessChunk(rawData);

            foreach (var packet in packets)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AddLog($"收到CMD: {packet.CommandId:X2}, Len: {packet.Payload.Length}");
                    _logService.WriteLog($"接收：{BitConverter.ToString(rawData)}");

                    switch (packet.CommandId)
                    {
                        case 0x00: // 固件版本
                            AddLog("收到设备握手信号(Cmd 00)，正在回复...");
                            var handshakePacket = new DataPacket(0x00);

                            // 2. 转换为字节数组
                            byte[] sendBytes = handshakePacket.ToBytes();

                            // 3. 通过串口发送回去
                            _serialService.SendData(sendBytes);

                            AddLog("已发送握手响应，进入产测模式");
                            break;

                        case 0x03: // MAC 地址
                            string mac = BitConverter.ToString(packet.Payload).Replace("-", ":");
                            AddLog($"[响应] MAC地址: {mac}");
                            break;

                        //case 0x03: // USB 状态
                        //    if (packet.Payload.Length > 0)
                        //    {
                        //        byte status = packet.Payload[0];
                        //        string statusStr = status == 0 ? "OK" : (status == 1 ? "连接正常" : "未知状态");
                        //        AddLog($"[响应] USB状态: {statusStr} (Code: {status})");
                        //    }
                        //    break;

                        case 0x05: // Cmd5 响应
                            string payloadHex = BitConverter.ToString(packet.Payload);
                            AddLog($"[响应] Cmd5返回: {payloadHex}");
                            break;

                        default:
                            AddLog($"[响应] 未知命令 {packet.CommandId:X2}, 数据: {BitConverter.ToString(packet.Payload)}");
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
            if (string.IsNullOrEmpty(SelectedPort))
            {
                System.Windows.MessageBox.Show("请先选择一个串口端口！");
                AddLog("[警告] 试图打开串口，但未选择端口号");
                return;
            }
            bool success = _serialService.Open(SelectedPort, BaudRate);
            IsConnected = success;
        }

        [RelayCommand]
        private void SendCommand(string commandTag)
        {
            if (_serialService == null)
            {
                AddLog("发送失败，串口未打开");
                return;
            }

            byte cmdId = 0;
            byte[] paramsData = new byte[0];

            switch (commandTag)
            {
                case "Cmd1": // 握手
                    cmdId = 0x00;
                    break;
                case "Cmd2": // 读取ID
                    cmdId = 0x03;
                    break;
                case "Cmd3":
                    //cmdId = 0x03;
                    break;
                default: return;
            }

            // 1. 创建包
            var packet = new DataPacket(cmdId, paramsData);

            // 2. 序列化成字节数组
            byte[] finalBytes = packet.ToBytes();

            // 3. 发送
            _serialService.SendData(finalBytes);
            _logService.WriteLog($"[发送] Cmd_{cmdId:X2}{BitConverter.ToString(finalBytes)}");
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