using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace Haier_E246_TestTool.Services
{
    public class SerialPortService : IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly object _writeLock = new object(); // 线程安全锁

        // 定义数据接收事件
        public event Action<string> DataReceived;

        public SerialPortService()
        {
            _serialPort = new SerialPort();
            _serialPort.DataReceived += OnDataReceived;
        }

        public string[] GetAvailablePorts() => SerialPort.GetPortNames();

        public bool Connect(string portName, int baudRate)
        {
            try
            {
                if (_serialPort.IsOpen) _serialPort.Close();

                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;
                _serialPort.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Disconnect()
        {
            if (_serialPort.IsOpen) _serialPort.Close();
        }

        public bool IsOpen => _serialPort.IsOpen;

        // 线程安全的发送方法
        public void SendData(string data)
        {
            if (!_serialPort.IsOpen) return;

            lock (_writeLock) // 关键：防止多线程同时写入导致资源冲突
            {
                byte[] bytes = Encoding.ASCII.GetBytes(data); // 根据实际设备协议调整编码
                _serialPort.Write(bytes, 0, bytes.Length);
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // 读取现有所有数据
                string data = _serialPort.ReadExisting();
                DataReceived?.Invoke(data);
            }
            catch { /* 忽略读取错误或记录日志 */ }
        }

        public void Dispose()
        {
            _serialPort?.Dispose();
        }
    }
}