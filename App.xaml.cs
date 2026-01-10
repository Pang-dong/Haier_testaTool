using Haier_E246_TestTool.Models;
using Haier_E246_TestTool.Services;
using Haier_E246_TestTool.ViewModels;
using System.Windows;

namespace Haier_E246_TestTool
{
    public partial class App : Application
    {
        private SerialPortService _serialService;
        private LogService _logService;
        private ConfigService _configService; // 新增配置服务
        private MainViewModel _mainViewModel;
        private MainWindow _mainWindow;
        private AppConfig _appConfig; // 全局配置对象

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. 初始化基础服务
            _logService = new LogService();
            _configService = new ConfigService(_logService);

            // 2. 加载配置 (如果文件不存在会自动返回默认值)
            _appConfig = _configService.Load();

            // 3. 初始化串口服务
            _serialService = new SerialPortService(_logService);

            // 4. 创建 ViewModel (注入配置对象)
            _mainViewModel = new MainViewModel(_serialService, _appConfig);

            // 绑定日志事件
            _logService.OnNewLog += (msg, type) =>
            {
                _mainViewModel.AddLog(msg);
            };

            // 5. 创建并显示窗口
            _mainWindow = new MainWindow();
            _mainWindow.DataContext = _mainViewModel;

            // 6. 处理关闭事件：保存配置、清理资源
            _mainWindow.Closing += (s, args) =>
            {
                // 保存当前的界面设置到文件
                _configService.Save(_appConfig);

                _mainViewModel.Cleanup();
                _serialService.Dispose();
            };

            _mainWindow.Show();
        }
    }
}