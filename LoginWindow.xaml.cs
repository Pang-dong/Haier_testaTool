using Haier_E246_TestTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Haier_E246_TestTool
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            var vm = new LoginViewModel();
            this.DataContext = vm;
            vm.CloseAction = () => this.Close();
            var btn = this.FindName("BtnLogin") as System.Windows.Controls.Button; // 这里的 Button 实际上在 XAML 绑定了 Command
        }
    }
}
