using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vision
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        MainViewModel MainVM01;
        SomaViewModel MainVM02;
        MainViewModel MainVM03;

        private Window mainWindow;
        public MainWindow()
        {
            InitializeComponent();
        
            Loaded += (s, e) =>
            {
                inputTextBox.Text = Properties.Settings.Default.SavedText;//机种名输入程序,Properties Setting相应设置

                //工位1
                MainVM01 = new MainViewModel("./Para/01",1);
                view01.DataContext = MainVM01;

                //工位2
                MainVM02 = new SomaViewModel("./Para/02", 2);
                view02.DataContext = MainVM02;

                //工位3
                MainVM03 = new MainViewModel("./Para/03", 3);
                view03.DataContext = MainVM03;



            };
        }

        //退出软件的时候避免占用<任务管理器>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.SavedText = inputTextBox.Text;//机种名输入程序,Properties Setting相应设置
            Properties.Settings.Default.Save();//机种名输入程序,Properties Setting相应设置

            System.Environment.Exit(0);
        }

        //鼠标点选当前位置灰度值显示
        private void OnTimedEvent(object sender, EventArgs e)
        {
            try
            {
                // 获取屏幕的大小
                System.Drawing.Rectangle screenBounds = Screen.PrimaryScreen.Bounds;

                // 创建一个和屏幕大小相同的位图
                Bitmap screenBitmap = new Bitmap(screenBounds.Width, screenBounds.Height);

                // 创建一个用于从屏幕获取像素的画布对象
                using (Graphics g = Graphics.FromImage(screenBitmap))
                {                   
                    g.CopyFromScreen(screenBounds.Location, System.Drawing.Point.Empty, screenBounds.Size);// 将屏幕像素复制到位图中
                }
                System.Drawing.Point mousePosition = new System.Drawing.Point(System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y);//取置顶点坐标                                                                                                                                                                          // 获取鼠标相对于窗口的位置

                // 确保鼠标在屏幕内
                if (screenBounds.Contains(mousePosition))
                {
                    // 获取鼠标位置的像素颜色
                    System.Drawing.Color pixelColor = screenBitmap.GetPixel(mousePosition.X, mousePosition.Y);
                    // 计算颜色的反色
                    System.Drawing.Color invertedColor = InvertColor(pixelColor);
                    // 将颜色转换为 SolidColorBrush，以便应用于 TextBlock 的文本颜色
                    SolidColorBrush invertedBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(invertedColor.A, invertedColor.R, invertedColor.G, invertedColor.B));
                    mouseFollower.Foreground = invertedBrush;

                    // 计算灰度值
                    int grayScale = (int)(0.3 * pixelColor.R + 0.59 * pixelColor.G + 0.11 * pixelColor.B);

                    //mouseFollower.Text = grayScale.ToString();//启用此句程序,灰度值将在鼠标位置显示
                    //Console.WriteLine("当前位置灰度值为:" + grayScale);

                    mouseValue.Text= "[Ver12.0-20250114] " + "当前位置灰度值为: " + grayScale.ToString();//主界面增加的灰度值显示控件                   
                }
                else
                {
                    Console.WriteLine("鼠标位置超出屏幕范围。");
                }
            }
            catch { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            btn_MaxSize.Click += Btn_MaxSize_Click;
            btn_MiniSize.Click += Btn_MiniSize_Click;
            btn_Close.Click += Btn_Close_Click;
            mainWindow = System.Windows.Application.Current.MainWindow;
            // 创建一个新的计时器
            System.Windows.Forms.Timer tim = new System.Windows.Forms.Timer();

            tim.Interval = 100;
            tim.Tick += OnTimedEvent;           
            tim.Start();// 启动计时器
        }

        /// <summary>
        /// 关闭主窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Btn_Close_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SavedText = inputTextBox.Text;//机种名输入程序,Properties Setting相应设置
            Properties.Settings.Default.Save();//机种名输入程序,Properties Setting相应设置
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// 设置最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Btn_MiniSize_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 最大化和正常窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Btn_MaxSize_Click(object sender, RoutedEventArgs e)
        {
            //判断是否以及最大化，最大化就还原窗口，否则最大化
            if (mainWindow.WindowState == WindowState.Maximized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            else
            {
                mainWindow.WindowState = WindowState.Maximized;
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // 获取鼠标当前位置
            System.Windows.Point mousePos = e.GetPosition(this);
            // 设置TextBlock的位置跟随鼠标
            mouseFollower.Margin = new Thickness(mousePos.X-20, mousePos.Y-130, 0, 0);
        }
        // 计算颜色的反色
        private System.Drawing.Color InvertColor(System.Drawing.Color color)
        {
            return System.Drawing.Color.FromArgb(255 - color.R, 255 - color.G, 255 - color.B);
        }
    }
}
