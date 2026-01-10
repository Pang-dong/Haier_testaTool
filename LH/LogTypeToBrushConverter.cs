using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Haier_E246_TestTool.LH
{
    public class LogTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value as string;
            switch (type)
            {
                case "ERROR": return Brushes.Red;        // 错误显示红色
                case "TX": return Brushes.Blue;       // 发送显示蓝色
                case "RX": return Brushes.Green;      // 接收显示绿色
                case "INFO": return Brushes.Black;      // 普通信息黑色
                default: return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
