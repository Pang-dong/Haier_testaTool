using Haier_E246_TestTool.Services;
using Haier_E246_TestTool.ViewModels;
using System.Windows;

namespace Haier_E246_TestTool
{
    public partial class App : Application
    {
        private SerialPortService _serialService;
        private LogService _logService;
        private MainViewModel _mainViewModel;
        private MainWindow _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. 先创建日志服务
            _logService = new LogService();

            // 2. 创建串口服务 (注入日志服务)
            _serialService = new SerialPortService(_logService);

            // 3. 创建 ViewModel (注入串口服务)
            string stationName = "Station-001";
            _mainViewModel = new MainViewModel(_serialService, stationName);

            // **** 关键：当日志服务有新消息时，推送到界面显示 ****
            _logService.OnNewLog += (msg, type) =>
            {
                _mainViewModel.AddLog(msg);
            };

            // 4. 创建窗口并绑定数据
            _mainWindow = new MainWindow();
            _mainWindow.DataContext = _mainViewModel;

            // 5. 窗口关闭时清理资源
            _mainWindow.Closing += (s, args) =>
            {
                _mainViewModel.Cleanup();
                _serialService.Dispose();
            };

            _mainWindow.Show();
        }
    }
}