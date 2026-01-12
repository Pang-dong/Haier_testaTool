using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Haier_E246_TestTool.Models
{
    public  class PlayVideoHelper
    {
        private Process playerProcess;
        /// <summary>
        /// 播放视频
        /// </summary>
        private void PlayVideo(string url,string FileName)
        {
            if (playerProcess == null)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    if (!string.IsNullOrEmpty( FileName))
                    {
                        playerProcess = new Process();
                        playerProcess.StartInfo.UseShellExecute = true;
                        playerProcess.StartInfo.FileName = FileName;
                        playerProcess.StartInfo.Arguments = url;
                        playerProcess.StartInfo.CreateNoWindow = true;
                        playerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        playerProcess.Start();
                    }
                    else
                    {
                        MessageBox.Show("没有找到VCL media player，请确认VCL是否配置正确！", languesInterface.messageShow.Caption, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("还没准备好呢，暂不能播放请稍后！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else if (playerProcess.HasExited)
            {

                if (File.Exists(FileName))
                {
                    playerProcess.Start();
                }
                else
                {
                    MessageBox.Show("没有找到VCL media player，请确认VCL是否配置正确！", languesInterface.messageShow.Caption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                IntPtr hWnd = FindWindow(null, TBox_Rtsp.Text + " - VLC media player"); //null为类名，可以用Spy++得到，也可以为空
                if (hWnd.ToInt32() != 0)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
            }
        }
    }
}
