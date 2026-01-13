using System.Windows;
using Haier_E246_TestTool.ViewModels; // 引用 ViewModel 命名空间

namespace Haier_E246_TestTool // 建议放在 Views 文件夹下
{
    public partial class BurnWindow : Window
    {
        public BurnWindow()
        {
            InitializeComponent();

            // 直接绑定 ViewModel
            this.DataContext = new BurnViewModel();
        }
    }
}