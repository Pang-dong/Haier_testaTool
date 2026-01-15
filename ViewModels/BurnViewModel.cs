using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.Models;
using Haier_E246_TestTool.Services;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Haier_E246_TestTool.Services.ReturnResult;

namespace Haier_E246_TestTool.ViewModels
{
    // 继承 ObservableObject 以使用 SetProperty
    public  partial class BurnViewModel : ObservableObject
    {
        private readonly AppConfig _config;
        private string _portNumber = "COM3";
        public string PortNumber
        {
            get => _portNumber;
            set => SetProperty(ref _portNumber, value);
        }

        private string _baudRate = "921600";
        public string BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        private string _bkLoaderPath = "";
        public string BkLoaderPath
        {
            get => _bkLoaderPath;
            set => SetProperty(ref _bkLoaderPath, value);
        }
        private string _sN = "";
        public string SN
        {
            get => _sN;
            set => SetProperty(ref _sN, value);
        }

        // 文件夹路径
        private string _sourceDir; // 待烧录
        public string SourceDir
        {
            get => _sourceDir;
            set => SetProperty(ref _sourceDir, value);
        }

        private string _targetDir;  // 已烧录
        public string TargetDir
        {
            get => _targetDir;
            set => SetProperty(ref _targetDir, value);
        }

        // 当前操作的文件名
        private string _currentFileName = "请点击开始烧录...";
        public string CurrentFileName
        {
            get => _currentFileName;
            set => SetProperty(ref _currentFileName, value);
        }

        // 状态颜色 (灰/黄/绿/红)
        private SolidColorBrush _statusColor = new SolidColorBrush(Colors.Gray);
        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        // 状态文字 (READY / PASS / FAIL)
        private string _statusText = "READY";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        // 是否正在烧录 (用于禁用按钮)
        private bool _isBurning;
        public bool IsBurning
        {
            get => _isBurning;
            set
            {
                if (SetProperty(ref _isBurning, value))
                {
                    // 当 IsBurning 改变时，StartBurnCommand 的可用状态也会改变
                    OnPropertyChanged(nameof(CanStart));
                }
            }
        }

        // 是否允许重试 (只有失败时为 true)
        private bool _canRetry;
        public bool CanRetry
        {
            get => _canRetry;
            set => SetProperty(ref _canRetry, value);
        }

        public bool CanStart => !IsBurning;

        // 日志列表
        public ObservableCollection<LogItem> Logs { get; } = new ObservableCollection<LogItem>();

        // 内部变量：当前文件的完整路径
        private string _currentFilePathInternal;
        private readonly ILogService _logService;
        public BurnViewModel()
        {
            _logService = new LogService();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            BkLoaderPath = Path.Combine(baseDir, "app", "bk_loader.exe");
            if (!File.Exists(BkLoaderPath))
            {
                AddLog($"【错误】找不到烧录工具: {BkLoaderPath}");
            }
            SourceDir = Path.Combine(baseDir, "Pending");
            TargetDir = Path.Combine(baseDir, "Burned");
            var configService = new ConfigService(_logService);
            _config = configService.Load();

            // 2. 初始化属性 (从 Config 读取)
            _portNumber = _config.BurnPort;
            _baudRate = _config.BurnBaud;
            _bkLoaderPath = _config.BkLoaderPath;
            // 初始化时确保文件夹存在
            try
            {
                if (!Directory.Exists(SourceDir)) Directory.CreateDirectory(SourceDir);
                if (!Directory.Exists(TargetDir)) Directory.CreateDirectory(TargetDir);
            }
            catch { /* 忽略权限错误 */ }
        }

        /// <summary>
        /// 开始烧录：从文件夹取一个新的文件
        /// </summary>
        [RelayCommand]
        private async Task StartBurn()
        {
            if (IsBurning) return;
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
            //var currentResult = new TestResultModel();
            //currentResult.Tset_Linsence = 1;
            //string resut = JsonConvert.SerializeObject(currentResult);
            //var writeService = new WriteTestResultService();
            //string result = writeService.WriteJsonResult(resut,"烧录工站", _config, "5C738E9CF3",SN);

            //string a = await WebApiHelper.WriteTestResultAsync(result);
            // 1. 检查并创建文件夹
            if (!Directory.Exists(SourceDir)) Directory.CreateDirectory(SourceDir);
            if (!Directory.Exists(TargetDir)) Directory.CreateDirectory(TargetDir);

            while (true)
            {
                // 重新获取文件列表（因为上一次循环可能移走了无效文件）
                var files = Directory.GetFiles(SourceDir, "*.bin");
                if (files.Length == 0)
                {
                    AddLog("错误：待烧录文件夹为空！请放入 .bin 授权文件。");
                    MessageBox.Show($"在 {SourceDir} 中未找到 .bin 文件！");
                    return;
                }

                // 锁定当前文件
                _currentFilePathInternal = files[0];
                CurrentFileName = Path.GetFileName(_currentFilePathInternal);

                // 获取不带后缀的文件名 (License)
                string licenseKey = Path.GetFileNameWithoutExtension(_currentFilePathInternal);
                AddLog($"正在校验 License: {licenseKey} ...");

                // 3. 调用 WebAPI 校验 License
                bool isLicenseValid = await CheckLicenseAsync(licenseKey);

                if (isLicenseValid)
                {
                    AddLog("License 校验通过，开始烧录...");
                    break;
                }
                else
                {
                    AddLog($"警告: License [{licenseKey}] 无效或已使用，自动移至已处理目录。");
                    MoveFileToTarget(_currentFilePathInternal);
                    await Task.Delay(500); // 稍微停顿一下，防止死循环刷屏太快
                    continue;
                }
            }

            await RunBurnProcess(_currentFilePathInternal);
        }
        /// <summary>
        /// 调用 WebAPI 检查 License 是否可用
        /// </summary>
        private async Task<bool> CheckLicenseAsync(string license)
        {
            try
            {
                string resultInfo = await WebApiHelper.GetLisenceInfonAsync(license);

                // 解析外层
                OuterResponse outer = JsonConvert.DeserializeObject<OuterResponse>(resultInfo);
                if (outer != null && !string.IsNullOrEmpty(outer.resultMsg))
                {
                    if (outer.status.Equals("0"))
                    {
                        AddLog($"失败:接口拒绝,该SN已经烧录过: {outer.resultMsg}");
                        return false;
                    }
                    else
                    {
                        AddLog("校验成功");
                        return true;
                    }
                }
                AddLog("接口返回格式异常");
                return false;
            }
            catch (Exception ex)
            {
                AddLog($"License 校验异常: {ex.Message}");
                return false; 
            }
        }
        /// <summary>
        /// 辅助方法：移动文件到目标目录
        /// </summary>
        private void MoveFileToTarget(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(TargetDir, fileName);

                // 如果目标有同名文件，删除旧的
                if (File.Exists(destPath)) File.Delete(destPath);

                File.Move(filePath, destPath);
                AddLog($"[文件移除] {fileName}");
            }
            catch (Exception ex)
            {
                AddLog($"移动文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 失败重试：使用当前文件再次烧录
        /// </summary>
        [RelayCommand]
        private async Task RetryBurn()
        {
            if (IsBurning || string.IsNullOrEmpty(_currentFilePathInternal) || !File.Exists(_currentFilePathInternal))
            {
                AddLog("错误：无法重试，文件不存在。");
                return;
            }
            await RunBurnProcess(_currentFilePathInternal);
        }

        /// <summary>
        /// 核心烧录流程
        /// </summary>
        private async Task RunBurnProcess(string authFile)
        {
            IsBurning = true;
            CanRetry = false;
            StatusColor = new SolidColorBrush(Colors.Orange); // 进行中：橙色
            StatusText = "BURNING...";
            Logs.Clear();
            AddLog($"准备烧录: {Path.GetFileName(authFile)}");
            bool success = false;
            string appDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app");

            // 在后台线程执行 Process 操作，防止卡死界面
            await Task.Run(() =>
            {
                // 这里填入你的固定文件路径
                string mainBin = Path.Combine(appDir, _config.MainBin);
                string littleFs = Path.Combine(appDir, _config.LittlefsBin);

                string args = $"download -p {PortNumber} -b {BaudRate} --mainBin-multi " +
                              $"{mainBin}@0x0-0x3ff600," +
                              $"{authFile}@0x72c000-0xa00," +
                              $"{littleFs}@0x74d000-0x70000 " +
                              $"--reboot 1 --fast-link 1";
                success = ExecuteProcess(BkLoaderPath, args);
            });

            IsBurning = false;

            if (success)
            {
                // --- 成功：变绿，移走文件 ---
                StatusColor = new SolidColorBrush(Colors.LightGreen);
                StatusText = "PASS";
                AddLog("烧录成功！");

                try
                {
                    string destPath = Path.Combine(TargetDir, Path.GetFileName(authFile));
                    // 如果目标有同名文件，覆盖
                    if (File.Exists(destPath)) File.Delete(destPath);

                    File.Move(authFile, destPath);
                    AddLog($"[文件移动] 已移至: {TargetDir}");

                    // 清理当前状态，准备下一个
                    _currentFilePathInternal = null;
                    CanRetry = false;
                }
                catch (Exception ex)
                {
                    AddLog($"警告：移动文件失败 {ex.Message}");
                }
            }
            else
            {
                // --- 失败：变红，允许重试 ---
                StatusColor = new SolidColorBrush(Colors.Red);
                StatusText = "FAIL";
                AddLog("烧录失败！");
                CanRetry = true; // 亮起重试按钮
            }
        }

        /// <summary>
        /// 封装 Process 调用逻辑 (参考你的原始代码)
        /// </summary>
        private bool ExecuteProcess(string exePath, string args)
        {
            if (!File.Exists(exePath))
            {
                DispatcherLog($"错误：找不到 {exePath}");
                return false;
            }

            Process p = new Process();
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = args;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;

            bool isSuccess = false;
            bool isPowerCyclePhase = false;
            DateTime lastOutput = DateTime.Now;
            bool hasOutput = false;

            p.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lastOutput = DateTime.Now;
                    hasOutput = true;
                    DispatcherLog(e.Data); // 实时打印

                    // 成功/失败判定逻辑
                    if (e.Data.Contains("All Finished Successfully") ||
                        e.Data.Contains("Writing Flash OK") ||
                        e.Data.Contains("Burn completed successfully"))
                    {
                        isSuccess = true;
                        isPowerCyclePhase = false; // 成功就不算掉电阶段了
                    }
                    else if (e.Data.Contains("Power cycle required"))
                    {
                        isPowerCyclePhase = true;
                    }
                    else if (e.Data.Contains("Writing Flash Failed"))
                    {
                        if (!isPowerCyclePhase) isSuccess = false;
                    }
                }
            };

            try
            {
                p.Start();
                p.BeginOutputReadLine();

                while (!p.HasExited)
                {
                    // 30秒无输出超时
                    if (hasOutput && (DateTime.Now - lastOutput).TotalSeconds > 30)
                    {
                        p.Kill();
                        DispatcherLog("错误：烧录超时 (30s无响应)");
                        return false;
                    }
                    System.Threading.Thread.Sleep(100);
                }
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                AddLog($"进程异常: {ex.Message}");
                return false;
            }

            if (isPowerCyclePhase && !isSuccess)
            {
                DispatcherLog("提示：检测到重启流程，视为烧录通过");
                isSuccess = true;
            }

            return isSuccess;
        }

        // 辅助方法：在 UI 线程记录日志
        private void DispatcherLog(string msg)
        {
            Application.Current.Dispatcher.Invoke(() => AddLog(msg,false));
        }

        private void AddLog(string msg, bool isSaveToFile = true)
        {
            string logTime = DateTime.Now.ToString("HH:mm:ss");
            string fullMsg = $"{logTime} > {msg}";
            bool isErrorOrPrompt = msg.Contains("错误") || msg.Contains("Error") ||
                                   msg.Contains("失败") || msg.Contains("Fail") ||
                                   msg.Contains("异常") || msg.Contains("Exception") ||
                                   msg.Contains("警告") || msg.Contains("请扫描");

            SolidColorBrush logColor = isErrorOrPrompt ? Brushes.Red : Brushes.Lime;

            // 2. 更新界面
            if (Application.Current.Dispatcher.CheckAccess())
            {
                UpdateUiLog(fullMsg, logColor);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => UpdateUiLog(fullMsg, logColor));
            }
            if (isSaveToFile)
            {
                try { _logService?.WriteLog(msg, LogType.Info, true); } catch { }
            }
        }
        private void UpdateUiLog(string logContent, SolidColorBrush color)
        {
            // 创建对象并添加
            Logs.Add(new LogItem { Text = logContent, Color = color });

            // 限制行数
            if (Logs.Count > 200) Logs.RemoveAt(0);
        }
        /// <summary>
        /// 日志模型
        /// </summary>
        public class LogItem
        {
            public string Text { get; set; }           // 日志内容
            public SolidColorBrush Color { get; set; } // 显示颜色
        }
    }
}