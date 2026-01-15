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
using Microsoft.Win32;
using System.Linq;
using Newtonsoft.Json;
using static Haier_E246_TestTool.Services.ReturnResult;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly SerialPortService _serialService;
        private readonly object _logLock = new object();
        private readonly PacketParser _parser = new PacketParser();
        private readonly ILogService _logService;
        private readonly PlayVideoHelper _videoHelper = new PlayVideoHelper();
        public ObservableCollection<TestCommandItem> TestCommands { get; } = new ObservableCollection<TestCommandItem>();
        private AppConfig CurrentConfig { get; set; }

        private ObservableCollection<string> _comPorts;
        public ObservableCollection<string> ComPorts
        {
            get => _comPorts;
            set => SetProperty(ref _comPorts, value);
        }
        // --- 自动测试相关的 Task 控制 ---
        private TaskCompletionSource<bool> _currentStepTcs;
        private byte _waitingCmdId;

        private string _vlcPath = @"C:\Program Files\VideoLAN\VLC\vlc.exe"; // 默认值
        public string VlcPath
        {
            get => _vlcPath;
            set
            {
                if (SetProperty(ref _vlcPath, value))
                {
                    // 如果你想把这个路径保存到 AppConfig，可以在这里写保存逻辑
                    if (CurrentConfig != null)
                    {
                        // CurrentConfig.VlcPath = value; // 需要在 AppConfig 加这个字段
                    }
                }
            }
        }
        private bool _isAutoTesting = false;

        public bool IsAutoTesting
        {
            get => _isAutoTesting;
            set
            {
                if (SetProperty(ref _isAutoTesting, value))
                {
                    OnPropertyChanged(nameof(IsNotAutoTesting));
                }
            }
        }

        // 这个属性依赖于 IsAutoTesting，用于给按钮绑定 IsEnabled
        public bool IsNotAutoTesting => !IsAutoTesting;
        private string _sN = "等待获取...";
        public string SN
        {
            get => _sN;
            set => SetProperty(ref _sN, value);
        }
        // 2. 【新增】提示信息 (绑定到画布 TextBlock)
        private string _mesMessage = "等待测试...";
        public string MesMessage
        {
            get => _mesMessage;
            set => SetProperty(ref _mesMessage, value);
        }

        // 3. 【新增】测试结果文本 (PASS / FAIL)
        private string _resultText = "WAIT";
        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        // 4. 【新增】结果颜色 (PASS用绿，FAIL用红)
        private Brush _resultColor = Brushes.Gray;
        public Brush ResultColor
        {
            get => _resultColor;
            set => SetProperty(ref _resultColor, value);
        }

        // 1. 设备信息属性 (手动实现)
        private string _deviceMac = "等待获取...";
        public string DeviceMac
        {
            get => _deviceMac;
            set => SetProperty(ref _deviceMac, value);
        }

        private string _wiFiVersion = "等待获取...";
        public string WiFiVersion
        {
            get => _wiFiVersion;
            set => SetProperty(ref _wiFiVersion, value);
        }
        private string _cameraVersion = "等待获取...";
        public string CameraVersion
        {
            get => _cameraVersion;
            set => SetProperty(ref _cameraVersion, value);
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
        private bool _isHandshaked = false;
        public bool IsHandshaked
        {
            get => _isHandshaked;
            set => SetProperty(ref _isHandshaked, value);
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
        private bool _isMesMode;
        public bool IsMesMode
        {
            get => _isMesMode;
            set => SetProperty(ref _isMesMode, value);
        }

        public ObservableCollection<string> UiLogs { get; } = new ObservableCollection<string>();

        public MainViewModel()
        {
        }

        // 实际运行时调用的构造函数
        public MainViewModel(SerialPortService serialService, AppConfig config, ILogService logService)
        {
            _serialService = serialService;
            _logService = logService;
            CurrentConfig = config;
            StationName = config.LastStationType;
            BaudRate = config.BaudRate;
            IsMesMode =config.IsMesMode;
            
            BindingOperations.EnableCollectionSynchronization(UiLogs, _logLock);
            RefreshPorts();
            _serialService.DataReceived += HandleDataReceived;
            if (StationName.Equals("测试工站"))
            {
                TestCommands.Add(new TestCommandItem("获取MAC", 0x03));
                TestCommands.Add(new TestCommandItem("获取WiFi版本", 0x02));
                TestCommands.Add(new TestCommandItem("获取Camera版本", 0x01));
            }
            else
            {
                TestCommands.Add(new TestCommandItem("信息核对", 0xFF));
            }
        }
        [RelayCommand]
        private void ExecuteTestCommand(TestCommandItem item)
        {
            if (item == null) return;
            if (!IsConnected) { AddLog("串口未打开"); return; }

            // 拦截逻辑：如果不是握手命令(0x00)且未握手，则拦截
            if (item.CommandId != 0x00 && !IsHandshaked)
            {
                MessageBox.Show("请先执行握手(进入产测模式)！");
                return;
            }

            // 发送数据
            var packet = new DataPacket(item.CommandId);
            _serialService.SendData(packet.ToBytes());
            _logService.WriteLog($"[发送] {item.Name} (ID:{item.CommandId:X2})");
        }
        [RelayCommand]
        private async Task StartAutoTest()
        {
            var inputBox = new InputBox();
            inputBox.Title = "请扫描SN号"; 
            if (inputBox.ShowDialog() == true)
            {
                SN = inputBox.Value;
                AddLog($"[扫码] SN: {SN}");
            }
            else
            {
                AddLog("取消测试：未输入SN");
                return;
            }
            if (!_serialService.IsOpen()) { AddLog("请先打开串口"); return; }

            IsAutoTesting = true;
            AddLog("=== 开始自动测试 ===");
            ResultText = "TESTING";
            ResultColor = Brushes.Orange;
            MesMessage = "正在执行自动测试...";
            bool isTotalPass = true;
            // 初始化结果记录
            var currentResult = new TestResultModel();

            // 重置所有颜色
            foreach (var cmd in TestCommands) cmd.ResetColor();

            try
            {
                // 【核心逻辑】遍历集合，自动执行
                foreach (var cmd in TestCommands)
                {
                    if (cmd.Name == "设备复位") continue;

                    AddLog($"正在执行: {cmd.Name}...");

                    // 执行一步
                    bool success = await RunTestStep(cmd.CommandId);

                    if (success)
                    {
                        cmd.SetSuccess(); 
                        if (cmd.CommandId == 0x01) currentResult.Test_Camera_Version = 1;
                        if (cmd.CommandId == 0x02) currentResult.Test_WIFI_VERSION = 1;
                        if (cmd.CommandId == 0x0) currentResult.Test_ReadMac = 1;
                    }
                    else
                    {
                        cmd.SetFail(); // 变红
                        isTotalPass = false;
                    }
                    await Task.Delay(500);
                }
                if (isTotalPass)
                {
                    ResultText = "PASS";
                    ResultColor = Brushes.Green;
                    MesMessage = "本地测试通过";
                }
                else
                {
                    ResultText = "FAIL";
                    ResultColor = Brushes.Red;
                    MesMessage = "本地测试失败，请检查红色的测试项。";
                }
                string rawJson = JsonConvert.SerializeObject(currentResult);

                // 2. 实例化服务并处理数据
                var writeService = new WriteTestResultService();

                string finalJson = writeService.EnrichJsonData(rawJson,CurrentConfig,"功能测试",DeviceMac,WiFiVersion, CameraVersion,SN);
                if (IsMesMode)
                {
                    if (isTotalPass)
                    {
                        AddLog("正在上传MES功能测试数据(请等待)...");
                        string resultInfo = await WebApiHelper.WriteTestResultAsync(finalJson);

                        // 解析外层响应
                        OuterResponse outer = JsonConvert.DeserializeObject<OuterResponse>(resultInfo);
                        if (outer != null && !string.IsNullOrEmpty(outer.resultMsg))
                        {
                            BaseResult baseResult = JsonConvert.DeserializeObject<BaseResult>(outer.resultMsg);

                            if (baseResult != null && baseResult.IsSuccess)
                            {
                                AddLog("MES上传成功");
                                MesMessage = $"MES上传成功: {baseResult.msg}"; // 更新画布信息
                            }
                            else
                            {
                                AddLog($"MES上传失败: {baseResult?.msg}");
                                MesMessage = $"MES上传失败: {baseResult?.msg}"; // 更新画布信息

                                ResultText = "FAIL (MES)";
                                ResultColor =Brushes.Red;
                            }
                        }
                        else
                        {
                            AddLog("MES返回格式异常");
                            MesMessage = "MES返回格式异常";
                        }
                    }
                    else
                    {
                        AddLog("本地测试失败，跳过MES上传");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"测试过程异常: {ex.Message}");
                ResultText = "ERROR";
                ResultColor =Brushes.Red;
                MesMessage = $"异常: {ex.Message}";
            }
            finally
            {
                IsAutoTesting = false;
                _currentStepTcs = null;
            }
        }
        private async Task<bool> RunTestStep(byte cmdId)
        {
            _waitingCmdId = cmdId;
            _currentStepTcs = new TaskCompletionSource<bool>();

            // 发送
            var packet = new DataPacket(cmdId);
            _serialService.SendData(packet.ToBytes());

            var completedTask = await Task.WhenAny(_currentStepTcs.Task, Task.Delay(2000));
            return completedTask == _currentStepTcs.Task && _currentStepTcs.Task.Result;
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
                    var targetBtn = TestCommands.FirstOrDefault(x => x.CommandId == packet.CommandId);
                    if (targetBtn != null)
                    {
                        targetBtn.SetSuccess();
                    }
                    switch (packet.CommandId)
                    {
                        case 0x00:
                            IsHandshaked = true;
                            var handshakePacket = new DataPacket(0x00);
                            _serialService.SendData(handshakePacket.ToBytes());
                            AddLog("已发送握手响应");
                            break;

                        case 0x03: // MAC 地址
                            string mac = BitConverter.ToString(packet.Payload).Replace("-", "");
                            DeviceMac = mac; // 更新界面显示
                            AddLog($"[响应] MAC地址: {mac}");
                            break;

                        case 0x02:
                            string levver = Encoding.ASCII.GetString(packet.Payload);
                            WiFiVersion = levver;
                            AddLog($"[响应] Wifi版本: {levver}");
                            break;
                        case 0x09:
                            string rtsp = Encoding.ASCII.GetString(packet.Payload);
                            _videoHelper.PlayVideo(rtsp, VlcPath);
                            AddLog(rtsp);
                            break;
                        case 0x01:
                            string devicelevver = Encoding.ASCII.GetString(packet.Payload);
                            CameraVersion = devicelevver;
                            AddLog($"[响应] Camera版本: {devicelevver}");
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
        // 4. 选择 VLC 路径的命令
        [RelayCommand]
        private void SelectVlcPath()
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "VLC player (*.exe)|*.exe",
                Title = "请选择 vlc.exe"
            };

            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                VlcPath = openFileDialog.FileName;
                AddLog($"[设置] VLC路径已更新: {VlcPath}");
            }
        }
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
                IsHandshaked = false;
                if (TestCommands != null)
                {
                    foreach (var item in TestCommands)
                    {
                        item.ResetColor(); // 调用 TestCommandItem 里的重置方法
                    }
                }
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
            IsHandshaked = false;

            if (success)
            {
                AddLog($"[信息] 串口 {SelectedPort} 打开成功");
            }
        }

        [RelayCommand]
        private void SendCommand(string commandTag)
        {
            if (!IsConnected) { AddLog("【错误】串口未打开"); return; }
            ;
            if (commandTag != "Cmd1" && !IsHandshaked)
            {
                MessageBox.Show("请先“进入产测模式”进行握手！", "操作受限");
                AddLog("[警告] 未握手，已拦截操作");
                return;
            }
            byte cmdId = 0;
            byte[] paramsData = new byte[0];

            switch (commandTag)
            {
                case "Cmd1": cmdId = 0x00; break;
                case "Cmd2": cmdId = 0x03; break;
                case "Cmd3": cmdId = 0x02; break;
                case "Cmd4": cmdId = 0x09; break;
                case "Cmd5": cmdId = 0x08; break;
                case "Cmd6": cmdId = 0x01; break;
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