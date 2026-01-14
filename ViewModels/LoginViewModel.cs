using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.Models;
using Haier_E246_TestTool.Services;
using Haier_E246_TestTool; // 假设你的窗口都在 Views 命名空间
using System;
using System.Collections.ObjectModel;
using System.Web.UI.WebControls;
using System.Windows;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Haier_E246_TestTool.Services.ReturnResult;

namespace Haier_E246_TestTool.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        // 窗口关闭动作 (由 View 赋值)
        public Action CloseAction { get; set; }

        // 配置文件
        private AppConfig _config;

        // --- 属性 ---

        // 工站列表
        public ObservableCollection<string> StationTypes { get; } = new ObservableCollection<string>
        {
            "测试工站", // 对应 MainWindow
            "烧录工站"  // 对应 BurnWindow
        };

        private string _selectedStationType;
        public string SelectedStationType
        {
            get => _selectedStationType;
            set => SetProperty(ref _selectedStationType, value);
        }
        private bool _isRememberMe;
        public bool IsRememberMe
        {
            get => _isRememberMe;
            set => SetProperty(ref _isRememberMe, value);
        }


        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        // 密码通常在 View 的 CodeBehind 处理，或者通过绑定 PasswordBox 助手
        // 这里简化处理，假设 View 传过来，或者只是个 placeholder
        public string Password { get; set; }

        private bool _isMesMode;
        public bool IsMesMode
        {
            get => _isMesMode;
            set
            {
                if (SetProperty(ref _isMesMode, value))
                {
                    // 切换模式时清空错误信息
                    ErrorMessage = "";
                }
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }
        private string _ftpIp;
        public string FtpIp
        {
            get => _ftpIp;
            set => SetProperty(ref _ftpIp, value);
        }
        private bool _isLoging;

        // 2. 手动实现 IsLoging 属性
        public bool IsLoging
        {
            get => _isLoging;
            set
            {
                // SetProperty 会自动对比新旧值，如果改变了不仅会更新 _isLoging，还会通知界面 "IsLoging" 变了
                if (SetProperty(ref _isLoging, value))
                {
                    // 【关键】手动通知界面："IsNotLoging" 也变了！
                    // 这样绑定了 IsNotLoging 的按钮才会知道要去刷新状态
                    OnPropertyChanged(nameof(IsNotLoging));
                }
            }
        }

        // 3. 定义只读属性给按钮绑定 (取反)
        public bool IsNotLoging => !IsLoging;

        // --- 构造函数 ---
        public LoginViewModel()
        {
            _config = App.AppConfig;
            // 读取上次记录
            UserName = _config.LastUser;
            IsRememberMe = _config.IsRememberMe;
            SelectedStationType = string.IsNullOrEmpty(_config.LastStationType) ? "测试工站" : _config.LastStationType;
            IsMesMode = _config.IsMesMode;
            FtpIp = _config.FtpIp;
        }

        // --- 登录命令 ---
        [RelayCommand]
        private async Task Login(object parameter)
        {
            if (IsLoging) return;
            try
            {
                IsLoging = true;
                if (parameter is System.Windows.Controls.PasswordBox pb)
                {
                    Password = pb.Password;
                }

                ErrorMessage = "";

                if (IsMesMode)
                {
                    if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password))
                    {
                        ErrorMessage = "账号密码不能为空";
                        return;
                    }
                    bool isMesLoginSuccess = await MesLoginAsync(UserName, Password);

                    if (!isMesLoginSuccess)
                    {
                        //ErrorMessage = "MES 登录失败：账号或密码错误";
                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(UserName)) UserName = "输入账号";
                }

                // 2. 保存登录信息
                _config.LastUser = UserName;
                _config.LastStationType = SelectedStationType;
                _config.IsRememberMe = IsRememberMe;
                _config.IsMesMode = IsMesMode;
                if (IsRememberMe)
                {
                    _config.Password = Password;
                }
                else
                {
                    _config.Password = "";
                }
                App.ConfigService.Save(_config);
                if (SelectedStationType == "烧录工站")
                {
                    var burnWin = new BurnWindow();
                    burnWin.Show();
                }
                else
                {
                    var mainViewModel = new MainViewModel(App.SerialService, App.AppConfig, App.LogService);

                    var mainWin = new MainWindow();
                    mainWin.DataContext = mainViewModel; // 绑定后，按钮才有反应

                    // 重新绑定日志事件，否则主界面日志不显示
                    App.LogService.OnNewLog += (msg, type) =>
                    {
                        mainViewModel.AddLog(msg);
                    };

                    mainWin.Show();
                }

                //关闭登录窗口
                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"系统异常: {ex.Message}";
            }
            finally
            {
                IsLoging = false;
            }
        }
        private async Task<bool> MesLoginAsync(string user, string pwd)
        {
            try
            {
                // 1. 准备参数
                var args = new Dictionary<string, object>
                {
                     { "_username", user },
                     { "_password", pwd }
                };

                string url = $"http://{FtpIp}:8017/Service.asmx";

                string jsonStr = await InvokeMESInterface.PostToMesAsync(url, "GetUserLoginInfo", args);

                // 4. 校验返回数据
                if (string.IsNullOrEmpty(jsonStr) || jsonStr.Contains("ERROR"))
                {
                    ErrorMessage = $"接口调用失败: {jsonStr}";
                    return false;
                }

                // 5. 解析 JSON
                var result = JsonConvert.DeserializeObject<BaseResult>(jsonStr);

                if (result != null && result.IsSuccess)
                {
                    return true;
                }
                else
                {
                    // 登录失败，显示 MES 返回的错误信息
                    ErrorMessage = result?.msg ?? "MES返回未知错误";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"系统异常: {ex.Message}";
                return false;
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }
    }
}