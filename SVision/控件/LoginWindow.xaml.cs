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

namespace Vision.控件
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        LoginViewModel loginViewModel = new LoginViewModel();

        public LoginWindow()
        {

            InitializeComponent();
            pw.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.Compare("ADMIN", un.Text.ToUpper()) == 0 && String.Compare(loginViewModel.pwsave, pw.Text.ToLower()) == 0)
            {
                this.DialogResult = true;
                Close();
                return;
            }

            MessageBox.Show("账户或密码输入错误！");
            this.DialogResult = false;
            Close();
        }
    }
}
