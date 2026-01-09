using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductTestTool.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace ProductTestTool.ViewModels
{
    /// <summary>
    /// 主视图模型
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly SerialPortService _serialPort = new SerialPortService();

        #region 串口设置属性

        [ObservableProperty]
        private ObservableCollection<string> _portNames = new ObservableCollection<string>();

        [ObservableProperty]
        private string _selectedPort;

        [ObservableProperty]
        private ObservableCollection<int> _baudRates = new ObservableCollection<int> 
        { 9600, 19200, 38400, 57600, 115200, 230400 };

        [ObservableProperty]
        private int _selectedBaudRate = 115200;

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private string _connectionStatus = "未连接";

        #endregion

        #region 日志

        [ObservableProperty]
        private string _logText = "";

        #endregion

        #region 构造函数

        public MainViewModel()
        {
            RefreshPorts();

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.ErrorOccurred += msg => AppendLog($"[错误] {msg}");
        }

        #endregion

        #region 串口命令

        [RelayCommand]
        private void RefreshPorts()
        {
            PortNames.Clear();
            foreach (var port in _serialPort.GetAvailablePorts())
            {
                PortNames.Add(port);
            }
            if (PortNames.Count > 0 && string.IsNullOrEmpty(SelectedPort))
            {
                SelectedPort = PortNames[0];
            }
            AppendLog($"[信息] 发现 {PortNames.Count} 个串口");
        }

        [RelayCommand]
        private void ToggleConnection()
        {
            if (IsConnected)
            {
                _serialPort.Close();
                IsConnected = false;
                ConnectionStatus = "未连接";
                AppendLog("[信息] 已断开连接");
            }
            else
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    AppendLog("[警告] 请选择串口");
                    return;
                }

                if (_serialPort.Open(SelectedPort, SelectedBaudRate))
                {
                    IsConnected = true;
                    ConnectionStatus = $"已连接 ({SelectedPort})";
                    AppendLog($"[信息] 已连接到 {SelectedPort}, 波特率: {SelectedBaudRate}");
                }
            }
        }

        #endregion

        #region 测试命令按钮 (根据实际协议修改)

        [RelayCommand]
        private async Task ExecuteCommand1Async()
        {
            if (!CheckConnection()) return;

            AppendLog("[发送] 命令1: 读取设备信息");
            // TODO: 根据实际协议修改命令
            byte[] command = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 };
            await SendCommandAsync(command);
        }

        [RelayCommand]
        private async Task ExecuteCommand2Async()
        {
            if (!CheckConnection()) return;

            AppendLog("[发送] 命令2: 读取状态");
            // TODO: 根据实际协议修改命令
            byte[] command = { 0x01, 0x03, 0x00, 0x01, 0x00, 0x01 };
            await SendCommandAsync(command);
        }

        [RelayCommand]
        private async Task ExecuteCommand3Async()
        {
            if (!CheckConnection()) return;

            AppendLog("[发送] 命令3: 写入参数");
            // TODO: 根据实际协议修改命令
            byte[] command = { 0x01, 0x06, 0x00, 0x00, 0x00, 0x01 };
            await SendCommandAsync(command);
        }

        [RelayCommand]
        private async Task ExecuteCommand4Async()
        {
            if (!CheckConnection()) return;

            AppendLog("[发送] 命令4: 复位设备");
            // TODO: 根据实际协议修改命令
            byte[] command = { 0x01, 0x06, 0x00, 0x01, 0x00, 0x00 };
            await SendCommandAsync(command);
        }

        [RelayCommand]
        private async Task ExecuteCommand5Async()
        {
            if (!CheckConnection()) return;

            AppendLog("[发送] 命令5: 自定义测试");
            // TODO: 根据实际协议修改命令
            byte[] command = { 0x01, 0x04, 0x00, 0x00, 0x00, 0x01 };
            await SendCommandAsync(command);
        }

        #endregion

        #region 日志命令

        [RelayCommand]
        private void ClearLog()
        {
            LogText = "";
        }

        #endregion

        #region 辅助方法

        private bool CheckConnection()
        {
            if (!IsConnected)
            {
                AppendLog("[警告] 请先连接串口");
                return false;
            }
            return true;
        }

        private async Task SendCommandAsync(byte[] command)
        {
            var hexStr = BitConverter.ToString(command).Replace("-", " ");
            AppendLog($"[TX] {hexStr}");

            var response = await _serialPort.SendAndReceiveAsync(command, 3000);

            if (response != null && response.Length > 0)
            {
                var respHex = BitConverter.ToString(response).Replace("-", " ");
                AppendLog($"[RX] {respHex}");
                ParseResponse(response);
            }
            else
            {
                AppendLog("[警告] 无响应或超时");
            }
        }

        /// <summary>
        /// 解析响应数据 - 根据实际协议实现
        /// </summary>
        private void ParseResponse(byte[] data)
        {
            // TODO: 根据实际协议解析响应
            AppendLog($"[解析] 收到 {data.Length} 字节数据");
        }

        private void OnDataReceived(byte[] data)
        {
            var hexStr = BitConverter.ToString(data).Replace("-", " ");
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                AppendLog($"[RX] {hexStr}");
            });
        }

        private void AppendLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            LogText += $"[{timestamp}] {message}\r\n";
        }

        public void Cleanup()
        {
            _serialPort?.Dispose();
        }

        #endregion
    }
}
