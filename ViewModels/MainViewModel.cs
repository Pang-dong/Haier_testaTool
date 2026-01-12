using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.Services;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Haier_E246_TestTool.Protocols;
using System;
using Haier_E246_TestTool.Models;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Diagnostics;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly SerialPortService _serialService;
        private readonly object _logLock = new object();
        private readonly PacketParser _parser = new PacketParser();
        private readonly ILogService _logService;
        private Process playVideoProcess;
        public AppConfig CurrentConfig { get; private set; }

        private ObservableCollection<string> _comPorts;
        public ObservableCollection<string> ComPorts
        {
            get => _comPorts;
            set => SetProperty(ref _comPorts, value);
        }
        // --- 自动测试相关的 Task 控制 ---
        private TaskCompletionSource<bool> _currentStepTcs;
        private byte _waitingCmdId;

        private bool _isAutoTesting = false;

        public bool IsAutoTesting
        {
            get => _isAutoTesting;
            set
            {
                // SetProperty 是 ObservableObject 提供的基础方法
                // 它会自动处理 _isAutoTesting = value 和 OnPropertyChanged(nameof(IsAutoTesting))
                if (SetProperty(ref _isAutoTesting, value))
                {
                    // 【关键点】手动通知界面：IsNotAutoTesting 也变了！
                    // 这样你的按钮 IsEnabled 状态才会刷新
                    OnPropertyChanged(nameof(IsNotAutoTesting));
                }
            }
        }

        // 这个属性依赖于 IsAutoTesting，用于给按钮绑定 IsEnabled
        public bool IsNotAutoTesting => !IsAutoTesting;

        // 1. 设备信息属性 (手动实现)
        private string _deviceMac = "等待获取...";
        public string DeviceMac
        {
            get => _deviceMac;
            set => SetProperty(ref _deviceMac, value);
        }

        private string _deviceVersion = "等待获取...";
        public string DeviceVersion
        {
            get => _deviceVersion;
            set => SetProperty(ref _deviceVersion, value);
        }

        // 2. 按钮颜色属性 (手动实现)
        // 需要引用 using System.Windows.Media;
        private SolidColorBrush _cmd1Brush = new SolidColorBrush(Colors.White);
        public SolidColorBrush Cmd1Brush
        {
            get => _cmd1Brush;
            set => SetProperty(ref _cmd1Brush, value);
        }

        private SolidColorBrush _cmd2Brush = new SolidColorBrush(Colors.White);
        public SolidColorBrush Cmd2Brush
        {
            get => _cmd2Brush;
            set => SetProperty(ref _cmd2Brush, value);
        }

        private SolidColorBrush _cmd3Brush = new SolidColorBrush(Colors.White);
        public SolidColorBrush Cmd3Brush
        {
            get => _cmd3Brush;
            set => SetProperty(ref _cmd3Brush, value);
        }

        private SolidColorBrush _cmd4Brush = new SolidColorBrush(Colors.White);
        public SolidColorBrush Cmd4Brush
        {
            get => _cmd4Brush;
            set => SetProperty(ref _cmd4Brush, value);
        }
        private SolidColorBrush _cmd5Brush = new SolidColorBrush(Colors.White);
        public SolidColorBrush Cmd5Brush
        {
            get => _cmd5Brush;
            set => SetProperty(ref _cmd5Brush, value);
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

                    // 如果正在自动测试，且收到了等待的指令，通知 Task 继续
                    if (IsAutoTesting && _currentStepTcs != null && packet.CommandId == _waitingCmdId)
                    {
                        _currentStepTcs.TrySetResult(true);
                    }

                    switch (packet.CommandId)
                    {
                        case 0x00:
                            var handshakePacket = new DataPacket(0x00);
                            _serialService.SendData(handshakePacket.ToBytes());
                            AddLog("已发送握手响应");
                            break;

                        case 0x03: // MAC 地址
                            string mac = BitConverter.ToString(packet.Payload).Replace("-", ":");
                            DeviceMac = mac; // 更新界面显示
                            AddLog($"[响应] MAC地址: {mac}");
                            break;

                        case 0x02: // Cmd5 响应
                            string levver = Encoding.ASCII.GetString(packet.Payload);
                            DeviceVersion = levver;
                            AddLog($"[响应] Cmd5返回: {levver}");
                            break;
                        case 0x09:
                            string rtsp = Encoding.ASCII.GetString(packet.Payload);

                            AddLog(rtsp);
                            break;
                        case 0x08:
                            string apip = Encoding.ASCII.GetString(packet.Payload);
                            AddLog(apip);
                            break;
                        default:
                            AddLog($"[响应] 未知命令 {packet.CommandId:X2}");
                            break;
                    }
                });
            }
        }
        [RelayCommand]
        private async Task StartAutoTest()
        {
            if (!_serialService.IsOpen()) // 假设 SerialPortService 有 IsOpen 方法或属性
            {
                AddLog("请先打开串口！");
                return;
            }

            IsAutoTesting = true;
            AddLog("=== 开始自动测试 ===");
            var currentResult = new TestResult();

            // 重置颜色和显示
            Cmd1Brush = new SolidColorBrush(Colors.White);
            Cmd2Brush = new SolidColorBrush(Colors.White);
            Cmd3Brush = new SolidColorBrush(Colors.White);
            Cmd4Brush = new SolidColorBrush(Colors.White);
            DeviceMac = "获取中...";
            DeviceVersion = "获取中...";

            try
            {
                if (await RunTestStep(0x03, "Cmd2"))
                {
                    Cmd2Brush = new SolidColorBrush(Colors.LightGreen);
                    currentResult.Test_ReadMac = 1;
                }
                else
                {
                    Cmd2Brush = new SolidColorBrush(Colors.Red);
                    currentResult.Test_ReadMac = 0;
                }

                await Task.Delay(500);

                if (await RunTestStep(0x02, "Cmd3"))
                {
                    Cmd3Brush = new SolidColorBrush(Colors.LightGreen);
                    currentResult.Test_Handshake = 1;
                }
                else
                {
                    Cmd3Brush = new SolidColorBrush(Colors.Red);
                    currentResult.Test_Handshake = 0;
                }

                AddLog("=== 自动测试结束 ===");
            }
            finally
            {
                IsAutoTesting = false;
                _currentStepTcs = null;
            }
        }
        /// <summary>
        /// 执行单个测试步骤
        /// </summary>
        /// <param name="targetCmdId">发送和等待接收的命令ID</param>
        /// <param name="btnTag">用于复用 SendCommand 逻辑的 tag</param>
        /// <returns>是否成功</returns>
        private async Task<bool> RunTestStep(byte targetCmdId, string btnTag)
        {
            _waitingCmdId = targetCmdId;
            _currentStepTcs = new TaskCompletionSource<bool>();

            // 调用现有的发送逻辑
            SendCommand(btnTag);

            // 等待回复 或 2秒超时
            var completedTask = await Task.WhenAny(_currentStepTcs.Task, Task.Delay(2000));

            if (completedTask == _currentStepTcs.Task)
            {
                return true; // 收到回复
            }
            else
            {
                AddLog($"[超时] {btnTag} 未收到回复");
                return false; // 超时
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
                // 如果当前是连接状态，点击后执行关闭
                _serialService.Close();
                IsConnected = false;
                AddLog("[信息] 串口已关闭");
                return; 
            }
            // 如果当前是关闭状态，执行打开逻辑
            if (string.IsNullOrEmpty(SelectedPort))
            {
                System.Windows.MessageBox.Show("请先选择一个串口端口！");
                AddLog("[警告] 试图打开串口，但未选择端口号");
                return;
            }

            bool success = _serialService.Open(SelectedPort, BaudRate);
            IsConnected = success;

            if (success)
            {
                AddLog($"[信息] 串口 {SelectedPort} 打开成功");
            }
        }

        [RelayCommand]
        private void SendCommand(string commandTag)
        {
            if (!IsConnected) return;

            byte cmdId = 0;
            byte[] paramsData = new byte[0];

            switch (commandTag)
            {
                case "Cmd1": cmdId = 0x00; break;
                case "Cmd2": cmdId = 0x03; break;
                case "Cmd3": cmdId = 0x02; break;
                case "Cmd4": cmdId = 0x09;break;
                case "Cmd5": cmdId = 0x08; break;
                default: return;
            }

            var packet = new DataPacket(cmdId, paramsData);
            _serialService.SendData(packet.ToBytes());
            _logService.WriteLog($"[发送] Cmd_{cmdId:X2}");
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