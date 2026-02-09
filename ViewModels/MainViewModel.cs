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
using Newtonsoft.Json.Linq;
using System.Threading;

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
        private readonly TestResultManager _testResultManager = new TestResultManager();
        public ObservableCollection<TestCommandItem> TestCommands { get; } = new ObservableCollection<TestCommandItem>();
        private AppConfig CurrentConfig { get; set; }
        private bool IsTotalPass { get; set; }  

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
        private Visibility _autoTestVisibility = Visibility.Visible;
        public Visibility AutoTestVisibility
        {
            get => _autoTestVisibility;
            set => SetProperty(ref _autoTestVisibility, value);
        }
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
                //TestCommands.Add(new TestCommandItem("打开AP模式", 0x08));
                //TestCommands.Add(new TestCommandItem("打开视频", 0x09));
                ////TestCommands.Add(new TestCommandItem("获取设备IP", 0x0B));
                //TestCommands.Add(new TestCommandItem("获取lisence", 0x05));
            }
            else
            {
                AutoTestVisibility = Visibility.Collapsed;
                TestCommands.Add(new TestCommandItem("信息核对", 0xFF));
            }
        }
        [RelayCommand]
        private async Task ExecuteTestCommand(TestCommandItem item)
        {
            if (item == null) return;
            if (!IsConnected) { AddLog("串口未打开"); return; }

            if (item.CommandId != 0x00 && !IsHandshaked)
            {
                MessageBox.Show("请先执行握手(进入产测模式)！");
                return;
            }
            if (item.CommandId == 0xFF)
            {
                var packetamc = new DataPacket(0x03);
                _serialService.SendData(packetamc.ToBytes());
                await Task.Delay(1000);
                await ExecuteInfoCheck(item);
                return;
            }
            _testResultManager.RecordTestResult(item.CommandId, false, null, "等待响应");
            // 发送数据
            var packet = new DataPacket(item.CommandId);
            _serialService.SendData(packet.ToBytes());
            _logService.WriteLog($"[发送] {item.Name} (ID:{item.CommandId:X2})");
        }

        /// <summary>
        /// 执行信息核对：MES获取MAC -> 弹窗扫贴纸MAC -> 与本地MAC进行三码比对
        /// </summary>
        private async Task ExecuteInfoCheck(TestCommandItem item)
        {

            var inputBoxsn = new InputBox();
            inputBoxsn.Title = "请扫描SN号";
            if (inputBoxsn.ShowDialog() == true)
            {
                SN = inputBoxsn.Value;
                AddLog($"[扫码] SN: {SN}");
            }
            else
            {
                AddLog("取消测试：未输入SN");
                return;
            }

            if (string.IsNullOrEmpty(DeviceMac) || DeviceMac.Contains("等待"))
            {
                AddLog("[核对] 错误：设备MAC未读取，无法进行比对。请先执行读取MAC步骤。");
                MesMessage = "核对失败：未获取设备MAC";
                ResultText = "FAIL";
                ResultColor = Brushes.Red;
                item.SetFail();
                return;
            }

            try
            {
                item.ResetColor();

                var inputBox1 = new InputBox();
                inputBox1.Title = "请扫描贴在外壳MAC条码";
                string mesMac = "";
                if (inputBox1.ShowDialog() == true)
                {
                    mesMac = inputBox1.Value;
                    AddLog($"[核对] 1/3 扫描贴纸MAC: {mesMac}");
                }
                else
                {
                    AddLog("[核对] 操作取消：未扫描贴在外壳贴纸MAC");
                    item.ResetColor();
                    return;
                }
                var inputBox = new InputBox();
                inputBox.Title = "请扫描贴纸MAC"; 

                string stickerMac = "";
                if (inputBox.ShowDialog() == true)
                {
                    stickerMac = inputBox.Value;
                    AddLog($"[核对] 2/3 扫描贴纸MAC: {stickerMac}");
                }
                else
                {
                    AddLog("[核对] 操作取消：未扫描贴纸MAC");
                    item.ResetColor();
                    return;
                }

                // 4. 三码比对 (归一化：去冒号、去横杠、转大写)
                string normDevMac = NormalizeMac(DeviceMac);
                string normMesMac = NormalizeMac(mesMac);
                string normScanMac = NormalizeMac(stickerMac);

                bool isMatch = (normDevMac == normMesMac) && (normDevMac == normScanMac);

                if (isMatch)
                {
                    // --- 校验通过 ---
                    string successMsg = $"三码一致 (PASS)\r\n设备: {DeviceMac}\r\nMES: {mesMac}\r\n贴纸: {stickerMac}";
                    MesMessage = successMsg;
                    ResultText = "PASS";
                    ResultColor = Brushes.Green;
                    item.SetSuccess();
                    _testResultManager.RecordTestResult(item.CommandId, true, successMsg);
                    AddLog($"[核对] 3/3 成功：{successMsg.Replace("\r\n", " | ")}");

                    var currentResult = new TestResultModel();
                    currentResult.YH_Result = 1;
                    string resut = JsonConvert.SerializeObject(currentResult);
                    var writeService = new WriteTestResultService();
                    string result = writeService.WriteJsonYHResult(resut, "信息核对", CurrentConfig, SN);
                    string resultInfo = await WebApiHelper.WriteTestResultAsync(result);

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
                            ResultColor = Brushes.Red;
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
                    // --- 校验失败 ---
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("MAC不一致 (FAIL)");
                    if (normDevMac != normMesMac) sb.AppendLine($"设备({DeviceMac}) != MES({mesMac})");
                    if (normDevMac != normScanMac) sb.AppendLine($"设备({DeviceMac}) != 贴纸({stickerMac})");
                    if (normMesMac != normScanMac) sb.AppendLine($"MES({mesMac}) != 贴纸({stickerMac})");

                    MesMessage = sb.ToString();
                    ResultText = "FAIL";
                    ResultColor = Brushes.Red;
                    item.SetFail();
                    _testResultManager.RecordTestResult(item.CommandId, false, sb.ToString());
                    AddLog($"[核对] 3/3 失败：{sb.ToString().Replace("\r\n", " ")}");
                }
            }
            catch (Exception ex)
            {
                HandleCheckFail(item, $"异常: {ex.Message}");
            }
        }
        /// <summary>
        /// 响应数据方法
        /// </summary>
        /// <param name="commandId"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private bool ValidateResponse(byte commandId, byte[] payload)
        {
            switch (commandId)
            {
                case 0x00: // 握手
                    return true; // 握手成功

                case 0x03: // MAC地址
                    string mac = BitConverter.ToString(payload).Replace("-", "");
                    return mac.Length == 12;
                case 0x02: // WiFi版本
                    string wifiVersion = Encoding.ASCII.GetString(payload);
                    return !string.IsNullOrWhiteSpace(wifiVersion);

                case 0x01: // Camera版本
                    string cameraVersion = Encoding.ASCII.GetString(payload);
                    return !string.IsNullOrWhiteSpace(cameraVersion);

                case 0x09: // RTSP视频
                    string rtsp = Encoding.ASCII.GetString(payload);
                    return !string.IsNullOrWhiteSpace(rtsp);

                case 0x08: // AP模式IP
                    string apIp = Encoding.ASCII.GetString(payload);
                    return !string.IsNullOrWhiteSpace(apIp);
                case 0x0B:
                    string apPort = Encoding.ASCII.GetString(payload);
                    return !string.IsNullOrWhiteSpace(apPort);
                default:
                    return true;
            }
        }

        /// <summary>
        /// 失败时的方法
        /// </summary>
        /// <param name="item"></param>
        /// <param name="errorMsg"></param>
        private void HandleCheckFail(TestCommandItem item, string errorMsg)
        {
            MesMessage = errorMsg;
            ResultText = "ERROR";
            ResultColor = Brushes.Red;
            item.SetFail();
            _testResultManager.RecordTestResult(item.CommandId, false, null, errorMsg);
            AddLog($"[核对] {errorMsg}");
        }

        /// <summary>
        /// 辅助方法：MAC地址归一化 (去冒号、横杠、空格，转大写)
        /// </summary>
        /// <param name="mac"></param>
        /// <returns></returns>
        private string NormalizeMac(string mac)
        {
            if (string.IsNullOrEmpty(mac)) return "";
            return mac.Replace(":", "").Replace("-", "").Replace(" ", "").Trim().ToUpper();
        }
        /// <summary>
        /// 开始自动测试
        /// </summary>
        /// <returns></returns>
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

            // 初始化结果记录
            var currentResult = new TestResultModel();
            _testResultManager.Reset(); // 重置测试结果管理器

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
                        // 记录成功到TestResultModel
                        if (cmd.CommandId == 0x01) currentResult.Test_Camera_Version = 1;
                        if (cmd.CommandId == 0x02) currentResult.Test_WIFI_VERSION = 1;
                        if (cmd.CommandId == 0x03) currentResult.Test_ReadMac = 1;
                    }
                    else
                    {
                        cmd.SetFail();
                    }
                    await Task.Delay(500);
                }

                // 根据TestResultManager判断总体结果
                if (_testResultManager.AllTestPassed())
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

                // 添加测试结果统计日志
                AddLog($"测试统计: {_testResultManager.GetPassCount()}/{_testResultManager.GetTotalCount()} 项通过");

                // 如果有失败的测试项，记录失败原因
                if (_testResultManager.AnyTestFailed())
                {
                    var allResults = _testResultManager.GetAllResults();
                    foreach (var result in allResults.Values.Where(r => !r.Success))
                    {
                        AddLog($"命令 0x{result.CommandId:X2} 失败: {result.ErrorMessage ?? "未知原因"}");
                    }
                }

                string rawJson = JsonConvert.SerializeObject(currentResult);

                // 2. 实例化服务并处理数据
                var writeService = new WriteTestResultService();

                string finalJson = writeService.EnrichJsonData(rawJson, CurrentConfig, "写号", DeviceMac, WiFiVersion, CameraVersion, SN);
                if (IsMesMode)
                {
                    if (_testResultManager.AllTestPassed())
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
                                ResultColor = Brushes.Red;
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
                ResultColor = Brushes.Red;
                MesMessage = $"异常: {ex.Message}";
            }
            finally
            {
                IsAutoTesting = false;
                _currentStepTcs = null;
            }
        }
        /// <summary>
        /// 执行发送命令并等待响应
        /// </summary>
        /// <param name="cmdId"></param>
        /// <returns></returns>
        private async Task<bool> RunTestStep(byte cmdId)
        {
            _waitingCmdId = cmdId;
            _currentStepTcs = new TaskCompletionSource<bool>();

            // 记录为等待响应状态
            _testResultManager.RecordTestResult(cmdId, false, null, "等待响应");

            // 发送
            var packet = new DataPacket(cmdId);
            _serialService.SendData(packet.ToBytes());

            var completedTask = await Task.WhenAny(_currentStepTcs.Task, Task.Delay(2000));

            if (completedTask == _currentStepTcs.Task)
            {
                return _currentStepTcs.Task.Result; // 返回验证结果
            }
            else
            {
                // 超时，记录失败
                _testResultManager.RecordTestResult(cmdId, false, null, "超时未响应");
                return false;
            }
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

                    // 验证响应
                    bool isValid = ValidateResponse(packet.CommandId, packet.Payload);
                    string dataStr = GetDataString(packet.CommandId, packet.Payload);
                    _testResultManager.RecordTestResult(packet.CommandId, isValid, dataStr,
                        isValid ? null : $"响应数据无效: {dataStr}");

                    // 如果正在自动测试，且收到了等待的指令，通知Task结果
                    if (IsAutoTesting && _currentStepTcs != null && packet.CommandId == _waitingCmdId)
                    {
                        _currentStepTcs.TrySetResult(isValid);
                    }

                    // 更新按钮状态（自动测试和手动测试都会更新）
                    var targetBtn = TestCommands.FirstOrDefault(x => x.CommandId == packet.CommandId);
                    if (targetBtn != null)
                    {
                        if (isValid)
                            targetBtn.SetSuccess();
                        else
                            targetBtn.SetFail();
                    }

                    // 处理数据并更新界面
                    ProcessResponseData(packet.CommandId, packet.Payload, isValid);
                });
            }
        }
        /// <summary>
        /// 字符串结果转换
        /// </summary>
        /// <param name="commandId"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private string GetDataString(byte commandId, byte[] payload)
        {
            switch (commandId)
            {
                case 0x03: // MAC地址
                    return BitConverter.ToString(payload).Replace("-", "");

                case 0x02: // WiFi版本
                case 0x01: // Camera版本
                case 0x09: // RTSP视频
                case 0x08: // AP模式IP
                    return Encoding.ASCII.GetString(payload);
                case 0x0B:
                    return Encoding.ASCII.GetString(payload);
                    case 0x05:
                    return Encoding.ASCII.GetString(payload);

                default:
                    return BitConverter.ToString(payload);
            }
        }
        /// <summary>
        /// 接收结果方法
        /// </summary>
        /// <param name="commandId"></param>
        /// <param name="payload"></param>
        /// <param name="isValid"></param>
        private void ProcessResponseData(byte commandId, byte[] payload, bool isValid)
        {
            switch (commandId)
            {
                case 0x00:
                    IsHandshaked = true;
                    var handshakePacket = new DataPacket(0x00);
                    _serialService.SendData(handshakePacket.ToBytes());
                    AddLog("已发送握手响应");
                    break;

                case 0x03: // MAC地址
                    string mac = BitConverter.ToString(payload).Replace("-", "");
                    DeviceMac = mac;
                    AddLog($"[响应] MAC地址: {mac} {(isValid ? "(有效)" : "(无效)")}");
                    break;

                case 0x02: // WiFi版本
                    string wifiVersion = Encoding.ASCII.GetString(payload);
                    WiFiVersion = wifiVersion;
                    AddLog($"[响应] WiFi版本: {wifiVersion} {(isValid ? "(有效)" : "(无效)")}");
                    break;

                case 0x01: // Camera版本
                    string cameraVersion = Encoding.ASCII.GetString(payload);
                    CameraVersion = cameraVersion;
                    AddLog($"[响应] Camera版本: {cameraVersion} {(isValid ? "(有效)" : "(无效)")}");
                    break;

                case 0x09: // RTSP视频
                    string rtsp = Encoding.ASCII.GetString(payload);
                    _videoHelper.PlayVideo(rtsp, VlcPath);
                    AddLog($"[响应] RTSP: {rtsp} {(isValid ? "(有效)" : "(无效)")}");
                    break;

                case 0x08: // AP模式IP
                    string apIp = Encoding.ASCII.GetString(payload);
                    AddLog($"[响应] AP模式IP: {apIp} {(isValid ? "(有效)" : "(无效)")}");
                    break;
                case 0x0A:
                    string apPort = Encoding.ASCII.GetString( payload);
                    AddLog($"连接wifi响应结果{apPort}");
                    break;
                case 0x0B:
                    string apHost = Encoding.ASCII.GetString(payload);
                    AddLog($"设备IP{apHost}");
                    break;
                    case 0x05:
                    string lisence = Encoding.ASCII.GetString(payload);
                    AddLog($"设备授权码{lisence}");
                    break; 
                default:
                    AddLog($"[响应] 未知命令 {commandId:X2}");
                    break;
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
                //_logService.WriteLog(msg);
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
                _testResultManager.Reset(); // 重置测试结果

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
            _testResultManager.RecordTestResult(cmdId, false, null, "等待响应");
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