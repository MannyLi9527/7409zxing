using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vision
{
    /// <summary>
    /// DJViewCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class DJViewCtrl : UserControl
    {
        public DJViewCtrl()
        {
            InitializeComponent();

            ButtonAutomationPeer peer = new ButtonAutomationPeer(AutoStart);
            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv.Invoke();
        }

        /// 图片原始属性 
        public TransformGroup Img_Recover;
        public TransformGroup Img_Recover_tgnew;

        /// <summary>
        /// 图片放大
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _ZoomIn(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                ScaleTransform st = tgnew.Children[1] as ScaleTransform;
                img.RenderTransformOrigin = new Point(0.5, 0.5);
                if (st.ScaleX > 0 && st.ScaleX <= 2.0)
                {
                    st.ScaleX += st.ScaleX * 0.05;
                    st.ScaleY += st.ScaleY * 0.05;
                }
                else if (st.ScaleX < 0 && st.ScaleX >= -2.0)
                {
                    st.ScaleX -= st.ScaleX * 0.05;
                    st.ScaleY += st.ScaleY * 0.05;
                }
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        /// <summary>
        /// 图片缩小
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _ZoomOut(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                ScaleTransform st = tgnew.Children[1] as ScaleTransform;
                img.RenderTransformOrigin = new Point(0.5, 0.5);
                if (st.ScaleX >= 0.2)
                {
                    st.ScaleX -= st.ScaleX * 0.05;
                    st.ScaleY -= st.ScaleY * 0.05;
                }
                else if (st.ScaleX <= -0.2)
                {
                    st.ScaleX += st.ScaleX * 0.05;
                    st.ScaleY -= st.ScaleY * 0.05;
                }
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        /// <summary>
        /// 滚轮缩放
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void ZoomWheel(Image img, double Delta)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                ScaleTransform st = tgnew.Children[1] as ScaleTransform;
                img.RenderTransformOrigin = new Point(0.5, 0.5);
                if (st.ScaleX < 0.3 && st.ScaleY < 0.3 && Delta < 0)
                {
                    return;
                }
                st.ScaleX += st.ScaleX * 0.05 * ((double)Delta / System.Math.Abs((double)Delta));
                st.ScaleY += st.ScaleY * 0.05 * ((double)Delta / System.Math.Abs((double)Delta));
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        private Image movingObject;  // 记录当前被拖拽移动的图片
        private Point StartPosition; // 本次移动开始时的坐标点位置
        private Point EndPosition;   // 本次移动结束时的坐标点位置

        private void Imc_Player_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image img = sender as Image;
            movingObject = img;
            StartPosition = e.GetPosition(img);
        }

        private void Imc_Player_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image img = sender as Image;
            movingObject = null;
        }

        private void Imc_Player_MouseMove(object sender, MouseEventArgs e)
        {
            Image img = sender as Image;
            if (e.LeftButton == MouseButtonState.Pressed && sender == movingObject)
            {
                EndPosition = e.GetPosition(img);

                TransformGroup tg = img.RenderTransform as TransformGroup;
                var tgnew = tg.CloneCurrentValue();
                if (tgnew != null)
                {
                    TranslateTransform tt = tgnew.Children[0] as TranslateTransform;

                    var X = EndPosition.X - StartPosition.X;
                    var Y = EndPosition.Y - StartPosition.Y;
                    tt.X += X;
                    tt.Y += Y;
                }
                img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
            }
        }

        private void Imc_Player_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ZoomWheel(Imc_PlayerForCamera, e.Delta);
        }

        /// <summary>
        /// 图片上移
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _MoveUp(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                TranslateTransform tt = tgnew.Children[0] as TranslateTransform;
                ScaleTransform st = tgnew.Children[1] as ScaleTransform;
                Console.WriteLine(st.ScaleY);
                tt.Y = tt.Y + 50;
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        /// <summary>
        /// 图片下移
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _MoveDown(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                TranslateTransform tt = tgnew.Children[0] as TranslateTransform;
                ScaleTransform st = tgnew.Children[1] as ScaleTransform;
                tt.Y = tt.Y - 50;
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        /// <summary>
        /// 图片左移
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _MoveLeft(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                TranslateTransform tt = tgnew.Children[0] as TranslateTransform;
                ScaleTransform st = tgnew.Children[1] as ScaleTransform;
                tt.X = tt.X - 50;
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        /// <summary>
        /// 图片右移
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _MoveRight(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                TranslateTransform tt = tgnew.Children[0] as TranslateTransform;
                ScaleTransform st = tgnew.Children[1] as ScaleTransform;
                Console.WriteLine(tt.X);
                tt.X = tt.X + 50;
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        /// <summary>
        /// 图片左转
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _RotateLeft(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                RotateTransform rt = tgnew.Children[2] as RotateTransform;
                img.RenderTransformOrigin = new Point(0.5, 0.5);
                rt.Angle += 90;
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        /// <summary>
        /// 图片右转
        /// </summary>
        /// <param name="img">被操作的前台Image控件</param>
        public void _RotateRight(Image img)
        {
            TransformGroup tg = img.RenderTransform as TransformGroup;
            var tgnew = tg.CloneCurrentValue();
            if (tgnew != null)
            {
                RotateTransform rt = tgnew.Children[2] as RotateTransform;
                img.RenderTransformOrigin = new Point(0.5, 0.5);
                rt.Angle -= 90;
            }
            img.RenderTransform = tgnew;// 重新给图像赋值Transform变换属性
        }

        private void RotateRight(object sender, RoutedEventArgs e)
        {
            _RotateRight(Imc_PlayerForCamera);
        }

        private void RotateLeft(object sender, RoutedEventArgs e)
        {
            _RotateLeft(Imc_PlayerForCamera);
        }

        private void Recover(object sender, RoutedEventArgs e)
        {
            Imc_PlayerForCamera.RenderTransform = Img_Recover_tgnew;
        }

        private void MoveUp(object sender, RoutedEventArgs e)
        {
            _MoveUp(Imc_PlayerForCamera);
        }

        private void MoveRight(object sender, RoutedEventArgs e)
        {
            _MoveRight(Imc_PlayerForCamera);
        }

        private void MoveDown(object sender, RoutedEventArgs e)
        {
            _MoveDown(Imc_PlayerForCamera);
        }

        private void MoveLeft(object sender, RoutedEventArgs e)
        {
            _MoveLeft(Imc_PlayerForCamera);
        }

        private void ZoomIn(object sender, RoutedEventArgs e)
        {
            _ZoomIn(Imc_PlayerForCamera);
        }

        private void ZoomOut(object sender, RoutedEventArgs e)
        {
            _ZoomOut(Imc_PlayerForCamera);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //ButtonAutomationPeer peer = new ButtonAutomationPeer(AutoStart);
            //IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            //invokeProv.Invoke();
        }
    }
}

