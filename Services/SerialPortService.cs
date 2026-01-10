using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Services
{
    public class SerialPortService : IDisposable
    {
        private SerialPort _serialPort;
        private readonly ILogService _logService;

        // 定义数据接收事件
        public event Action<byte[]> DataReceived;

        public SerialPortService(ILogService logService)
        {
            _logService = logService;
            _serialPort = new SerialPort();
        }

        public string[] GetAvailablePorts() => SerialPort.GetPortNames();

        public bool Open(string portName, int baudRate)
        {
            try
            {
                if (_serialPort.IsOpen) Close();

                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Parity = Parity.None;

                _serialPort.DataReceived += OnSerialDataReceived;
                _serialPort.Open();

                _logService.WriteLog($"串口 {portName} 打开成功", LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                _logService.WriteLog($"打开串口失败: {ex.Message}", LogType.Error);
                return false;
            }
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.DataReceived -= OnSerialDataReceived;
                _serialPort.Close();
                _logService.WriteLog("串口已关闭", LogType.Info);
            }
        }

        public void SendData(byte[] data)
        {
            if (!_serialPort.IsOpen)
            {
                _logService.WriteLog("发送失败：串口未打开", LogType.Warning);
                return;
            }

            try
            {
                _serialPort.Write(data, 0, data.Length);
                _logService.WriteLog(BitConverter.ToString(data), LogType.Tx);
            }
            catch (Exception ex)
            {
                _logService.WriteLog($"发送异常: {ex.Message}", LogType.Error);
            }
        }

        // 后台线程接收数据
        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                byte[] buffer = new byte[bytesToRead];
                _serialPort.Read(buffer, 0, bytesToRead);

                // 记录原始日志
                _logService.WriteLog(BitConverter.ToString(buffer), LogType.Rx);

                // 触发业务层事件
                DataReceived?.Invoke(buffer);
            }
            catch (Exception ex)
            {
                _logService.WriteLog($"接收异常: {ex.Message}", LogType.Error);
            }
        }

        public void Dispose()
        {
            Close();
            _serialPort?.Dispose();
        }
    }
}