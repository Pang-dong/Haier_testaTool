using Haier_E246_TestTool.Services;
using Haier_E246_TestTool.ViewModels;
using System.Windows;

namespace Haier_E246_TestTool
{
    public partial class App : Application
    {
        private SerialPortService _serialService;
        private MainViewModel _mainViewModel;
        private MainWindow _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. 初始化日志
            log4net.Config.XmlConfigurator.Configure();

            // 2. 创建服务实例 (Service)
            _serialService = new SerialPortService();

            // 3. 创建 ViewModel，并注入服务和配置参数 (Dependency Injection)
            // 假设从配置文件读取了工位名称 "Station-001"
            string stationName = "Station-001";
            _mainViewModel = new MainViewModel(_serialService, stationName);

            // 4. 创建 Window，并赋值 DataContext
            _mainWindow = new MainWindow();
            _mainWindow.DataContext = _mainViewModel;

            // 5. 处理关闭事件，清理资源
            _mainWindow.Closing += (s, args) =>
            {
                _mainViewModel.Cleanup();
                _serialService.Dispose(); // 服务生命周期随 App 结束
            };

            // 6. 显示窗口
            _mainWindow.Show();
        }
    }
}