using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace Haier_E246_TestTool.Services // 注意命名空间调整
{
    public class PlayVideoHelper
    {
        private Process _playerProcess;

        // Windows API 用于窗口控制 (可选，如果只是播放可以不需要，但你的原代码里有)
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// 播放 RTSP 视频流
        /// </summary>
        /// <param name="url">RTSP 地址</param>
        /// <param name="vlcPath">VLC 播放器的完整路径 (vlc.exe)</param>
        public void PlayVideo(string url, string vlcPath)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("RTSP 地址为空，无法播放！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(vlcPath) || !File.Exists(vlcPath))
            {
                MessageBox.Show("未找到 VLC 播放器，请先设置正确的 VLC 路径！", "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (_playerProcess != null && !_playerProcess.HasExited)
                {
                    try
                    {
                        _playerProcess.Kill();
                        _playerProcess.WaitForExit(1000);
                    }
                    catch { /* 忽略无法杀死的异常 */ }
                }

                _playerProcess = new Process();
                _playerProcess.StartInfo.UseShellExecute = false; // 改为 false 更稳妥
                _playerProcess.StartInfo.FileName = vlcPath;
                _playerProcess.StartInfo.Arguments = $"{url}";
                _playerProcess.StartInfo.CreateNoWindow = true;
                _playerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                _playerProcess.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动播放器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 停止播放 (可选)
        /// </summary>
        public void Stop()
        {
            if (_playerProcess != null && !_playerProcess.HasExited)
            {
                try { _playerProcess.Kill(); } catch { }
            }
        }
    }
}