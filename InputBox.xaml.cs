using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// InputBox.xaml 的交互逻辑
    /// </summary>
    public partial class InputBox : Window
    {
        public String Value { get; set; }
        private bool numberFlag = false;

        public InputBox()
        {
            InitializeComponent();
        }
        public InputBox(bool isnumber)
        {
            InitializeComponent();
            numberFlag = isnumber;
        }
        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_Confirm(object sender, RoutedEventArgs e)
        {
            Value = TBox_value.Text;
            this.DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TBox_value.Focus();
            TBox_value.SelectAll();
        }

        private void tbox_value_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Btn_Confirm.Focus();
            }
        }

        private void TBox_value_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (numberFlag)
            {
                Regex re = new Regex("[^0-9.-]+");
                e.Handled = re.IsMatch(e.Text);
            }
        }
    }
}
