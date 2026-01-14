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

            // 绑定 ViewModel
            var vm = new LoginViewModel();
            this.DataContext = vm;

            // 绑定关闭动作
            vm.CloseAction = () => this.Close();
            if (App.AppConfig.IsRememberMe && !string.IsNullOrEmpty(App.AppConfig.Password))
            {
                this.TxtPassword.Password = App.AppConfig.Password;
            }
        }
    }
}
