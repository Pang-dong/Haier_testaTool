using System.Collections.Specialized;
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
            var vm = new BurnViewModel();

            this.DataContext = vm;
            ((INotifyCollectionChanged)vm.Logs).CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    // 滚动到最后一条
                    if (ConsoleLogList.Items.Count > 0)
                    {
                        ConsoleLogList.ScrollIntoView(ConsoleLogList.Items[ConsoleLogList.Items.Count - 1]);
                    }
                }
            };
        }
    }
}