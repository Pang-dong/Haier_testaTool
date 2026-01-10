using System;
using System.Globalization;
using System.Windows.Data;

namespace Haier_E246_TestTool.LH
{
    // 将转换器独立为一个文件，确保命名空间和可见性正确
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
                return !booleanValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
