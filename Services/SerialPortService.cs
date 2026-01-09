using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace ProductTestTool.Services
{
    /// <summary>
    /// 串口服务
    /// </summary>
    public class SerialPortService : IDisposable
    {
        private SerialPort _serialPort;
        private readonly object _lock = new object();
        private readonly List<byte> _receiveBuffer = new List<byte>();

        public bool IsConnected => _serialPort?.IsOpen ?? false;

        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event Action<byte[]> DataReceived;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> ErrorOccurred;

        /// <summary>
        /// 获取可用端口
        /// </summary>
        public string[] GetAvailablePorts() => SerialPort.GetPortNames();

        /// <summary>
        /// 打开串口
        /// </summary>
        public bool Open(string portName, int baudRate, int dataBits = 8, 
            StopBits stopBits = StopBits.One, Parity parity = Parity.None)
        {
            try
            {
                Close();

                _serialPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    DataBits = dataBits,
                    StopBits = stopBits,
                    Parity = parity,
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"打开串口失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
            catch { }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        public bool Send(byte[] data)
        {
            if (!IsConnected || data == null) return false;

            try
            {
                lock (_lock)
                {
                    _serialPort.Write(data, 0, data.Length);
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"发送失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送并等待响应
        /// </summary>
        public async Task<byte[]> SendAndReceiveAsync(byte[] data, int timeout = 3000, 
            CancellationToken cancellationToken = default)
        {
            if (!IsConnected) return null;

            lock (_receiveBuffer)
            {
                _receiveBuffer.Clear();
            }

            if (!Send(data)) return null;

            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (_receiveBuffer)
                {
                    if (_receiveBuffer.Count > 0)
                    {
                        await Task.Delay(50, cancellationToken);
                        var result = _receiveBuffer.ToArray();
                        _receiveBuffer.Clear();
                        return result;
                    }
                }
                await Task.Delay(10, cancellationToken);
            }

            return null;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    var count = _serialPort.BytesToRead;
                    if (count > 0)
                    {
                        var buffer = new byte[count];
                        _serialPort.Read(buffer, 0, count);

                        lock (_receiveBuffer)
                        {
                            _receiveBuffer.AddRange(buffer);
                        }

                        DataReceived?.Invoke(buffer);
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
