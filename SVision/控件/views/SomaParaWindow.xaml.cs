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

namespace Vision.控件.views
{
    /// <summary>
    /// SomaParaWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SomaParaWindow : Window
    {
        public event EventHandler WindowClosed;
        public SomaParaWindow(object dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }


        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            e.Handled = true;
        }


        private void Btn_Close_Click(object sender, RoutedEventArgs e)
        {
            WindowClosed?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
    }
}
