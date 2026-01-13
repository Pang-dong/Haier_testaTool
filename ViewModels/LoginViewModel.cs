using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haier_E246_TestTool.Models;
using Haier_E246_TestTool.Services;
using Haier_E246_TestTool; // 假设你的窗口都在 Views 命名空间
using System;
using System.Collections.ObjectModel;
using System.Web.UI.WebControls;
using System.Windows;

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

        // --- 构造函数 ---
        public LoginViewModel()
        {
            _config = App.AppConfig;

             // 获取服务实例
             var configService = App.ConfigService;
            // 读取上次记录
            UserName = _config.LastUser;
            SelectedStationType = string.IsNullOrEmpty(_config.LastStationType) ? "测试工站" : _config.LastStationType;
            IsMesMode = false; // 默认调试模式
        }

        // --- 登录命令 ---
        [RelayCommand]
        private void Login(object parameter)
        {
            if (parameter is System.Windows.Controls.PasswordBox pb)
            {
                Password = pb.Password;
            }

            ErrorMessage = "";

            // 1. MES 模式校验 (预留接口)
            if (IsMesMode)
            {
                if (string.IsNullOrWhiteSpace(UserName))
                {
                    ErrorMessage = "请输入账号！";
                    return;
                }

                // 【预留接口】在这里调用你的 MES 登录 API
                bool isMesLoginSuccess = MockMesLogin(UserName, Password);

                if (!isMesLoginSuccess)
                {
                    ErrorMessage = "MES 登录失败：账号或密码错误 (模拟)";
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(UserName)) UserName = "DebugUser";
            }

            // 2. 保存登录信息
            _config.LastUser = UserName;
            _config.LastStationType = SelectedStationType;
            App.ConfigService.Save(_config);

            // 3. 跳转逻辑
            if (SelectedStationType == "烧录工站")
            {
                // 打开烧录界面
                var burnWin = new BurnWindow();
                burnWin.Show();
            }
            else
            {
                // 打开测试主界面
                var mainWin = new MainWindow();
                mainWin.Show();
            }

            // 4. 关闭登录窗口
            CloseAction?.Invoke();
        }

        // 模拟 MES 登录的方法
        private bool MockMesLogin(string user, string pwd)
        {
            // 你以后在这里写真正的逻辑
            return true;
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }
    }
}