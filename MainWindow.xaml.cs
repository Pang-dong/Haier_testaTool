using Haier_E246_TestTool;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Haier_E246_TestTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 确保在窗口关闭时清理 ViewModel 资源
            Closing += (s, e) => (DataContext as MainViewModel)?.Cleanup();
        }
    }
}
