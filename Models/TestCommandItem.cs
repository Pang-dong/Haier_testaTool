using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Haier_E246_TestTool.Models
{
    public class TestCommandItem : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        // 命令ID，不需要通知界面，所以用普通属性
        public byte CommandId { get; set; }

        private SolidColorBrush _background;
        public SolidColorBrush Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        // 构造函数
        public TestCommandItem(string name, byte cmdId)
        {
            Name = name;
            CommandId = cmdId;
            Background = new SolidColorBrush(Colors.White); // 默认白色
        }

        // 辅助方法：变绿
        public void SetSuccess() => Background = new SolidColorBrush(Colors.LightGreen);

        // 辅助方法：变红
        public void SetFail() => Background = new SolidColorBrush(Colors.Red);

        // 辅助方法：重置
        public void ResetColor() => Background = new SolidColorBrush(Colors.White);
    }
}
