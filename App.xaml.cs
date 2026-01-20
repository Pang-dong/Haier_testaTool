using Haier_E246_TestTool.Models;
using Haier_E246_TestTool.Services;
using Haier_E246_TestTool.ViewModels;
using System.Windows;

namespace Haier_E246_TestTool
{
    public partial class App : Application
    {
        // 1. 【修改】把 private 改为 public static，并改名为大写开头（去掉下划线）
        public static SerialPortService SerialService;
        public static LogService LogService;
        public static ConfigService ConfigService;
        public static AppConfig AppConfig;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LogService = new LogService();

            // 初始化配置服务 (依赖日志)
            ConfigService = new ConfigService(LogService);

            // 加载配置 (赋值给全局静态变量 AppConfig)
            AppConfig = ConfigService.Load();

            // 初始化串口 (依赖日志)
            SerialService = new SerialPortService(LogService);
            var loginWin = new LoginWindow();
            loginWin.Show();
        }
    }
}