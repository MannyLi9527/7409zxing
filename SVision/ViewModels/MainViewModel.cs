using Amib.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Vision.控件;
using Vision.通用;
using System.IO;
using Vision.控件.views;

namespace Vision
{
    public class MainViewModel : ViewModelBase
    {
        #region 构造函数

        private static MainParaWindow _mainParaWindow;

        public MainViewModel(string _path,int n)
        {
            path = _path;
            stationNum = n;
            DetecColor = "Red";
            DetecResult = "NG";
            DetecColor2 = "Red";
            DetecResult2 = "NG";

            HiKCamera = new HiKhelper();
            HiKCamera2 = new HiKhelper();

            mySocket = new MySocket(n);

            stp = new SmartThreadPool { MaxThreads = 1 };//SmartThreadPool 第三方库多线程管理
            stp2 = new SmartThreadPool { MaxThreads = 1 };//SmartThreadPool 第三方库多线程管理

            HiKCamera.MV_OnOriFrameInvoked += Hik_MV_OnOriFrameInvoked;
            HiKCamera2.MV_OnOriFrameInvoked += Hik_MV_OnOriFrameInvoked2;

            ImSrc_test = new WriteableBitmap(new BitmapImage(new Uri(@"./图片/null.png", UriKind.Relative)));//加载一个自定义的黑图像框框进来
            ImSrc_test2 = new WriteableBitmap(new BitmapImage(new Uri(@"./图片/null.png", UriKind.Relative)));//加载一个自定义的黑图像框框进来

            init();

            ButtonStr = "连接相机";
            CommboxID = new int[2] { 0, 1 };
            isConnect = false;
            isConnect2 = false;          
        }

        #endregion

        #region 相机相关

        /// <summary>
        /// 相机信息
        /// </summary>
        public HiKhelper HiKCamera;
        public HiKhelper HiKCamera2;

        #endregion

        #region 前台显示
        /// <summary>
        /// Image控件显示的资源
        /// </summary>
        public string ButtonStr
        {
            get => GetProperty(() => ButtonStr);
            set => SetProperty(() => ButtonStr, value);
        }

        /// <summary>
        /// Commbox选项
        /// </summary>
        public int[] CommboxID
        {
            get => GetProperty(() => CommboxID);
            set => SetProperty(() => CommboxID, value);
        }

        /// <summary>
        /// 相机是否已连接
        /// </summary>
        public bool isConnect
        {
            get => GetProperty(() => isConnect);
            set => SetProperty(() => isConnect, value);
        }

        public bool isConnect2
        {
            get => GetProperty(() => isConnect2);
            set => SetProperty(() => isConnect2, value);
        }

        //相机列表
        public string[] Cameralist
        {
            get => GetProperty(() => Cameralist);
            set => SetProperty(() => Cameralist, value);
        }
        

        #endregion

        #region 前台操作
        /// <summary>
        /// 开关相机
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void CamCommand(object obj)
        {
            Growl.Clear();
            if ((!HiKCamera.isConnect) && (!HiKCamera2.isConnect)) 
            {
                HiKCamera.Search();
                HiKCamera.Connect(CamNum);
                HiKCamera2.Connect(CamNum2);

                HiKCamera.SetExposureTime(ValueExposureTime);//相机连接后把保存的曝光参数先执行一次
                HiKCamera2.SetExposureTime(ValueExposureTime2);//相机连接后把保存的曝光参数先执行一次
                HiKCamera.SetGainValue(ValueGain);//相机连接后把保存的增益参数先执行一次
                HiKCamera2.SetGainValue(ValueGain2);//相机连接后把保存的增益参数先执行一次
                SamplingMode = 0;//相机连接后先默认为触发采样

                HiKCamera.TriggerOnce();//相机连接后开启软件先触发一次
                HiKCamera2.TriggerOnce();//相机连接后开启软件先触发一次

                //打开Socekt服务器
                mySocket.Listen();
                mySocket.MV_onMess += ReceiveDataInvoked;
            }
            else
            {
                HiKCamera.Disconnect();
                HiKCamera2.Disconnect();
            }

            if((HiKCamera.isConnect) && (HiKCamera2.isConnect)) 
            {
                ButtonStr = "断开相机";
            }
            else
            {
                ButtonStr = "连接相机";
            }
            isConnect = HiKCamera.isConnect;
            isConnect2 = HiKCamera2.isConnect;
        }

        /// <summary>
        /// 触发一次拍照
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void TriggerCommand(object obj)
        {
            int i = HiKCamera.TriggerOnce();
            int i2 = HiKCamera2.TriggerOnce();
        }

        /// <summary>
        /// 退出软件
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void ExitCommand(object obj)
        {

            HiKCamera.Disconnect();
            HiKCamera2.Disconnect();
               
            mySocket.StopListen();//关闭网络连接

            Thread.Sleep(200);                          
            Application.Current.Shutdown(-1);           
        }

        

        /// <summary>
        /// 参数配置窗口
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void ParaSetCommand(object obj)
        {

            LoginWindow loginWindow = new LoginWindow();
            LoginViewModel loginViewModel = new LoginViewModel();
            loginWindow.DataContext = loginViewModel;
            if (true == loginWindow.ShowDialog())
            {
                isEnabled = true;
                if (_mainParaWindow == null || !_mainParaWindow.IsVisible)
                {
                    _mainParaWindow = new MainParaWindow(this);
                    _mainParaWindow.Closed += (s, args) => _mainParaWindow = null;
                    _mainParaWindow.WindowClosed += _mainParaWindow_WindowClosed;
                    string var = obj as string;
                    if (var == "JT")
                    {
                        selectedIndex = 0;
                    }
                    else
                        selectedIndex = 3;
                    _mainParaWindow.Show();

                }
                else
                {
                    Growl.Warning("请勿重复打开窗口！");
                }

            }

            

        }

        private void _mainParaWindow_WindowClosed(object sender, EventArgs e)
        {
            isEnabled = false;
        }


        #endregion

        #region 初始化相关
        public string path;
        public void InitErr()
        {
            try
            {
                //获得文件路径
                string localFilePath = "";
                localFilePath = path + "/Config.xml";
                XDocument xdoc = new XDocument();
                XDeclaration xdec = new XDeclaration("1.0", "utf-8", "yes");
                xdoc.Declaration = xdec; // 百度搜索C#读写XML文件

                XElement rootEle;
                XElement classEle;

                //添加根节点
                rootEle = new XElement("CamConfig");
                xdoc.Add(rootEle);

                classEle = new XElement("BuchangMode", BuchangMode);
                rootEle.Add(classEle);
                classEle = new XElement("MeasuringScaleX", MeasuringScaleX);
                rootEle.Add(classEle);
                classEle = new XElement("MeasuringScaleY", MeasuringScaleY);
                rootEle.Add(classEle);
                classEle = new XElement("BuchangSpec", BuchangSpec);
                rootEle.Add(classEle);
                classEle = new XElement("ReadSample", ReadSample);
                rootEle.Add(classEle);

                classEle = new XElement("ValueExposureTime", ValueExposureTime);
                rootEle.Add(classEle);
                classEle = new XElement("ValueExposureTime2", ValueExposureTime2);
                rootEle.Add(classEle);
                classEle = new XElement("ValueGain", ValueGain);
                rootEle.Add(classEle);
                classEle = new XElement("ValueGain2", ValueGain2);
                rootEle.Add(classEle);

                classEle = new XElement("ScanDirection", ScanDirection);
                rootEle.Add(classEle);
                classEle = new XElement("ScanDirection2", ScanDirection2);
                rootEle.Add(classEle);

                classEle = new XElement("CamNum", CamNum);
                rootEle.Add(classEle);
                classEle = new XElement("CamNum2", CamNum2);
                rootEle.Add(classEle);

                classEle = new XElement("DefultX", DefultX);
                rootEle.Add(classEle);
                classEle = new XElement("DefultX2", DefultX2);
                rootEle.Add(classEle);

                classEle = new XElement("DefultY", DefultY);
                rootEle.Add(classEle);
                classEle = new XElement("DefultY2", DefultY2);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadius", MinRadius);
                rootEle.Add(classEle);
                classEle = new XElement("MinRadius2", MinRadius2);
                rootEle.Add(classEle);

                classEle = new XElement("MaxRadius", MaxRadius);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadius2", MaxRadius2);
                rootEle.Add(classEle);

                classEle = new XElement("GrayValue", GrayValue);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValue2", GrayValue2);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusAng", MinRadiusAng);
                rootEle.Add(classEle);
                classEle = new XElement("MinRadiusAng2", MinRadiusAng2);
                rootEle.Add(classEle);

                classEle = new XElement("MaxRadiusAng", MaxRadiusAng);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusAng2", MaxRadiusAng2);
                rootEle.Add(classEle);

                classEle = new XElement("GrayValueAng", GrayValueAng);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueAng2", GrayValueAng2);
                rootEle.Add(classEle);

                classEle = new XElement("MarkMode", MarkMode);
                rootEle.Add(classEle);
                classEle = new XElement("MarkMode2", MarkMode2);
                rootEle.Add(classEle);

                classEle = new XElement("SpecAreaAng", SpecAreaAng);
                rootEle.Add(classEle);
                classEle = new XElement("SpecAreaAng2", SpecAreaAng2);
                rootEle.Add(classEle);

                classEle = new XElement("ReferenceX", ReferenceX);
                rootEle.Add(classEle);
                classEle = new XElement("ReferenceY", ReferenceY);
                rootEle.Add(classEle);
                classEle = new XElement("ReferenceSpec", ReferenceSpec);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusA1", MinRadiusA1);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusA1", MaxRadiusA1);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueA1", GrayValueA1);
                rootEle.Add(classEle);
                classEle = new XElement("CompareA1", CompareA1);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueA1", SpecValueA1);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusA2", MinRadiusA2);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusA2", MaxRadiusA2);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueA2", GrayValueA2);
                rootEle.Add(classEle);
                classEle = new XElement("CompareA2", CompareA2);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueA2", SpecValueA2);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusA3", MinRadiusA3);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusA3", MaxRadiusA3);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueA3", GrayValueA3);
                rootEle.Add(classEle);
                classEle = new XElement("CompareA3", CompareA3);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueA3", SpecValueA3);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusA4", MinRadiusA4);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusA4", MaxRadiusA4);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueA4", GrayValueA4);
                rootEle.Add(classEle);
                classEle = new XElement("CompareA4", CompareA4);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueA4", SpecValueA4);
                rootEle.Add(classEle);

                classEle = new XElement("ReferenceX2", ReferenceX2);
                rootEle.Add(classEle);
                classEle = new XElement("ReferenceY2", ReferenceY2);
                rootEle.Add(classEle);
                classEle = new XElement("ReferenceSpec2", ReferenceSpec2);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusB1", MinRadiusB1);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusB1", MaxRadiusB1);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueB1", GrayValueB1);
                rootEle.Add(classEle);
                classEle = new XElement("CompareB1", CompareB1);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueB1", SpecValueB1);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusB2", MinRadiusB2);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusB2", MaxRadiusB2);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueB2", GrayValueB2);
                rootEle.Add(classEle);
                classEle = new XElement("CompareB2", CompareB2);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueB2", SpecValueB2);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadiusB3", MinRadiusB3);
                rootEle.Add(classEle);
                classEle = new XElement("MaxRadiusB3", MaxRadiusB3);
                rootEle.Add(classEle);
                classEle = new XElement("GrayValueB3", GrayValueB3);
                rootEle.Add(classEle);
                classEle = new XElement("CompareB3", CompareB3);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueB3", SpecValueB3);
                rootEle.Add(classEle);

                classEle = new XElement("AngZuzhuang", AngZuzhuang);
                rootEle.Add(classEle);


                classEle = new XElement("SpecAreaAngSX", SpecAreaAngSX);
                rootEle.Add(classEle);
                classEle = new XElement("MarkErroMode", MarkErroMode);
                rootEle.Add(classEle);

                classEle = new XElement("SpecAreaAngSX2", SpecAreaAngSX2);
                rootEle.Add(classEle);
                classEle = new XElement("MarkErroMode2", MarkErroMode2);
                rootEle.Add(classEle);

                classEle = new XElement("TiduDirection", TiduDirection);
                rootEle.Add(classEle);
                classEle = new XElement("TiduDirection2", TiduDirection2);
                rootEle.Add(classEle);


                xdoc.Save(localFilePath);

            }
            catch
            {
                Growl.Error("默认配置生成失败！");
            }
        }

        //刷新相机列表
        private void RefreshCameraList()
        {
            HiKCamera.Search();
            Cameralist = HiKCamera.MV_CameraList;
            Thread.Sleep(50);
        }

        /// <summary>
        /// 读取参数文件
        /// </summary>
        int Paraload = 0;
        public void init()
        {
            int ret = 0;
            string filePath = path + "/Config.xml";
            if (XmlHelper.Exists(path, "Config.xml"))
            {
                try
                {
                    XDocument Config = XDocument.Load(path + "/Config.xml");

                    BuchangMode = int.Parse(Config.Descendants("BuchangMode").ElementAt(0).Value);
                    MeasuringScaleX = float.Parse(Config.Descendants("MeasuringScaleX").ElementAt(0).Value);
                    MeasuringScaleY = float.Parse(Config.Descendants("MeasuringScaleY").ElementAt(0).Value);
                    BuchangSpec = float.Parse(Config.Descendants("BuchangSpec").ElementAt(0).Value);
                    ReadSample = Config.Descendants("ReadSample").ElementAt(0).Value;

                    ValueExposureTime = float.Parse(Config.Descendants("ValueExposureTime").ElementAt(0).Value);
                    ValueExposureTime2 = float.Parse(Config.Descendants("ValueExposureTime2").ElementAt(0).Value);
                    ValueGain = float.Parse(Config.Descendants("ValueGain").ElementAt(0).Value);
                    ValueGain2 = float.Parse(Config.Descendants("ValueGain2").ElementAt(0).Value);

                    ScanDirection = int.Parse(Config.Descendants("ScanDirection").ElementAt(0).Value);
                    ScanDirection2 = int.Parse(Config.Descendants("ScanDirection2").ElementAt(0).Value);

                    CamNum = int.Parse(Config.Descendants("CamNum").ElementAt(0).Value);
                    CamNum2 = int.Parse(Config.Descendants("CamNum2").ElementAt(0).Value);

                    DefultX = float.Parse(Config.Descendants("DefultX").ElementAt(0).Value);
                    DefultX2 = float.Parse(Config.Descendants("DefultX2").ElementAt(0).Value);
                    DefultY = float.Parse(Config.Descendants("DefultY").ElementAt(0).Value);
                    DefultY2 = float.Parse(Config.Descendants("DefultY2").ElementAt(0).Value);

                    MinRadius = float.Parse(Config.Descendants("MinRadius").ElementAt(0).Value);
                    MinRadius2 = float.Parse(Config.Descendants("MinRadius2").ElementAt(0).Value);
                    MaxRadius = float.Parse(Config.Descendants("MaxRadius").ElementAt(0).Value);
                    MaxRadius2 = float.Parse(Config.Descendants("MaxRadius2").ElementAt(0).Value);

                    GrayValue = float.Parse(Config.Descendants("GrayValue").ElementAt(0).Value);
                    GrayValue2 = float.Parse(Config.Descendants("GrayValue2").ElementAt(0).Value);

                    MinRadiusAng = float.Parse(Config.Descendants("MinRadiusAng").ElementAt(0).Value);
                    MinRadiusAng2 = float.Parse(Config.Descendants("MinRadiusAng2").ElementAt(0).Value);
                    MaxRadiusAng = float.Parse(Config.Descendants("MaxRadiusAng").ElementAt(0).Value);
                    MaxRadiusAng2 = float.Parse(Config.Descendants("MaxRadiusAng2").ElementAt(0).Value);

                    GrayValueAng = float.Parse(Config.Descendants("GrayValueAng").ElementAt(0).Value);
                    GrayValueAng2 = float.Parse(Config.Descendants("GrayValueAng2").ElementAt(0).Value);
                    MarkMode = float.Parse(Config.Descendants("MarkMode").ElementAt(0).Value);
                    MarkMode2 = float.Parse(Config.Descendants("MarkMode2").ElementAt(0).Value);

                    SpecAreaAng = float.Parse(Config.Descendants("SpecAreaAng").ElementAt(0).Value);
                    SpecAreaAng2 = float.Parse(Config.Descendants("SpecAreaAng2").ElementAt(0).Value);


                    ReferenceX = float.Parse(Config.Descendants("ReferenceX").ElementAt(0).Value);
                    ReferenceY = float.Parse(Config.Descendants("ReferenceY").ElementAt(0).Value);
                    ReferenceSpec = float.Parse(Config.Descendants("ReferenceSpec").ElementAt(0).Value);

                    MinRadiusA1 = float.Parse(Config.Descendants("MinRadiusA1").ElementAt(0).Value);
                    MaxRadiusA1 = float.Parse(Config.Descendants("MaxRadiusA1").ElementAt(0).Value);
                    GrayValueA1 = float.Parse(Config.Descendants("GrayValueA1").ElementAt(0).Value);
                    CompareA1 = float.Parse(Config.Descendants("CompareA1").ElementAt(0).Value);
                    SpecValueA1 = float.Parse(Config.Descendants("SpecValueA1").ElementAt(0).Value);

                    MinRadiusA2 = float.Parse(Config.Descendants("MinRadiusA2").ElementAt(0).Value);
                    MaxRadiusA2 = float.Parse(Config.Descendants("MaxRadiusA2").ElementAt(0).Value);
                    GrayValueA2 = float.Parse(Config.Descendants("GrayValueA2").ElementAt(0).Value);
                    CompareA2 = float.Parse(Config.Descendants("CompareA2").ElementAt(0).Value);
                    SpecValueA2 = float.Parse(Config.Descendants("SpecValueA2").ElementAt(0).Value);

                    MinRadiusA3 = float.Parse(Config.Descendants("MinRadiusA3").ElementAt(0).Value);
                    MaxRadiusA3 = float.Parse(Config.Descendants("MaxRadiusA3").ElementAt(0).Value);
                    GrayValueA3 = float.Parse(Config.Descendants("GrayValueA3").ElementAt(0).Value);
                    CompareA3 = float.Parse(Config.Descendants("CompareA3").ElementAt(0).Value);
                    SpecValueA3 = float.Parse(Config.Descendants("SpecValueA3").ElementAt(0).Value);

                    MinRadiusA4 = float.Parse(Config.Descendants("MinRadiusA4").ElementAt(0).Value);
                    MaxRadiusA4 = float.Parse(Config.Descendants("MaxRadiusA4").ElementAt(0).Value);
                    GrayValueA4 = float.Parse(Config.Descendants("GrayValueA4").ElementAt(0).Value);
                    CompareA4 = float.Parse(Config.Descendants("CompareA4").ElementAt(0).Value);
                    SpecValueA4 = float.Parse(Config.Descendants("SpecValueA4").ElementAt(0).Value);

                    ReferenceX2 = float.Parse(Config.Descendants("ReferenceX2").ElementAt(0).Value);
                    ReferenceY2 = float.Parse(Config.Descendants("ReferenceY2").ElementAt(0).Value);
                    ReferenceSpec2 = float.Parse(Config.Descendants("ReferenceSpec2").ElementAt(0).Value);

                    MinRadiusB1 = float.Parse(Config.Descendants("MinRadiusB1").ElementAt(0).Value);                   
                    MaxRadiusB1 = float.Parse(Config.Descendants("MaxRadiusB1").ElementAt(0).Value);                   
                    GrayValueB1 = float.Parse(Config.Descendants("GrayValueB1").ElementAt(0).Value);
                    CompareB1 = float.Parse(Config.Descendants("CompareB1").ElementAt(0).Value);
                    SpecValueB1 = float.Parse(Config.Descendants("SpecValueB1").ElementAt(0).Value);

                    MinRadiusB2 = float.Parse(Config.Descendants("MinRadiusB2").ElementAt(0).Value);
                    MaxRadiusB2 = float.Parse(Config.Descendants("MaxRadiusB2").ElementAt(0).Value);
                    GrayValueB2 = float.Parse(Config.Descendants("GrayValueB2").ElementAt(0).Value);
                    CompareB2 = float.Parse(Config.Descendants("CompareB2").ElementAt(0).Value);
                    SpecValueB2 = float.Parse(Config.Descendants("SpecValueB2").ElementAt(0).Value);

                    MinRadiusB3 = float.Parse(Config.Descendants("MinRadiusB3").ElementAt(0).Value);
                    MaxRadiusB3 = float.Parse(Config.Descendants("MaxRadiusB3").ElementAt(0).Value);
                    GrayValueB3 = float.Parse(Config.Descendants("GrayValueB3").ElementAt(0).Value);
                    CompareB3 = float.Parse(Config.Descendants("CompareB3").ElementAt(0).Value);
                    SpecValueB3 = float.Parse(Config.Descendants("SpecValueB3").ElementAt(0).Value);

                    AngZuzhuang = float.Parse(Config.Descendants("AngZuzhuang").ElementAt(0).Value);


                    SpecAreaAngSX = float.Parse(Config.Descendants("SpecAreaAngSX").ElementAt(0).Value);
                    MarkErroMode = float.Parse(Config.Descendants("MarkErroMode").ElementAt(0).Value);

                    SpecAreaAngSX2 = float.Parse(Config.Descendants("SpecAreaAngSX2").ElementAt(0).Value);
                    MarkErroMode2 = float.Parse(Config.Descendants("MarkErroMode2").ElementAt(0).Value);

                    TiduDirection = int.Parse(Config.Descendants("TiduDirection").ElementAt(0).Value);
                    TiduDirection2 = int.Parse(Config.Descendants("TiduDirection2").ElementAt(0).Value);

                }
                catch (Exception err)
                {
                    Growl.Error("配置信息丢失！已重新生成");
                    InitErr();
                }
            }
            else
            {
                Growl.Error("配置路径不存在，初始化失败！");
                InitErr();//c#自己的库，库的作用就是重新保存一份默认参数
                ret++;
            }
            RefreshCameraList();
            Paraload = 1;
        }
        #endregion

        #region 前台参数绑定

        public int stationNum;

        /// <summary>
        /// Image控件显示的资源
        /// </summary>
        public WriteableBitmap ImSrc_test
        {
            get => GetProperty(() => ImSrc_test);
            set => SetProperty(() => ImSrc_test, value);
        }

        public WriteableBitmap ImSrc_test2
        {
            get => GetProperty(() => ImSrc_test2);
            set => SetProperty(() => ImSrc_test2, value);
        }


        /// <summary>
        /// 参数设置登录
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void SetCommand(object obj)
        {
            Growl.Clear();
            LoginWindow loginWindow = new LoginWindow();
            LoginViewModel loginViewModel = new LoginViewModel();
            loginWindow.DataContext = loginViewModel;
            if (true == loginWindow.ShowDialog())
            {
                isEnabled = true;
            }
        }

        /// <summary>
        /// 参数设置使能
        /// </summary>
        public bool isEnabled
        {
            get => GetProperty(() => isEnabled);
            set => SetProperty(() => isEnabled, value, () =>
            {

            });
        }

        /// <summary>
        /// 参数设置加密
        /// </summary>
        public int selectedIndex
        {
            get => GetProperty(() => selectedIndex);
            set => SetProperty(() => selectedIndex, value, () =>
            {
               
            });
        }

        /// <summary>
        /// 显示模式
        /// </summary>
        public int ShowMode
        {
            get => GetProperty(() => ShowMode);
            set => SetProperty(() => ShowMode, value);
        }

        /// <summary>
        /// 补偿模式
        /// </summary>
        public int BuchangMode
        {
            get => GetProperty(() => BuchangMode);
            set => SetProperty(() => BuchangMode, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    int oldValue = int.Parse(config.Descendants("BuchangMode").ElementAt(0).Value);
                    config.Descendants("BuchangMode").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 补偿模式-BuchangMode由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 比例尺X
        /// </summary>
        public float MeasuringScaleX
        {
            get => GetProperty(() => MeasuringScaleX);
            set => SetProperty(() => MeasuringScaleX, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MeasuringScaleX").ElementAt(0).Value);
                    config.Descendants("MeasuringScaleX").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 比例尺X-MeasuringScaleX由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 比例尺Y
        /// </summary>
        public float MeasuringScaleY
        {
            get => GetProperty(() => MeasuringScaleY);
            set => SetProperty(() => MeasuringScaleY, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MeasuringScaleY").ElementAt(0).Value);
                    config.Descendants("MeasuringScaleY").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 比例尺Y-MeasuringScaleY由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 补偿上限
        /// </summary>
        public float BuchangSpec
        {
            get => GetProperty(() => BuchangSpec);
            set => SetProperty(() => BuchangSpec, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("BuchangSpec").ElementAt(0).Value);
                    config.Descendants("BuchangSpec").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 补偿上限-BuchangSpec由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 通信读取样本
        /// </summary>
        public string ReadSample
        {
            get => GetProperty(() => ReadSample);
            set => SetProperty(() => ReadSample, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    string oldValue = config.Descendants("ReadSample").ElementAt(0).Value;
                    config.Descendants("ReadSample").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 读取样本-ReadSample由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 采样模式
        /// </summary>
        public int SamplingMode
        {
            get => GetProperty(() => SamplingMode);
            set => SetProperty(() => SamplingMode, value, () =>
            {
                if(value==0)
                {
                    //软触发
                    HiKCamera.TriggerMode(0);
                    HiKCamera2.TriggerMode(0);
                }

                if (value == 1)
                {
                    //连续触发
                    HiKCamera.TriggerMode(1);
                    HiKCamera2.TriggerMode(1);
                }
            });
        }

        /// <summary>
        /// 曝光设置
        /// </summary>
        public float ValueExposureTime
        {
            get => GetProperty(() => ValueExposureTime);
            set => SetProperty(() => ValueExposureTime, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ValueExposureTime").ElementAt(0).Value);
                    config.Descendants("ValueExposureTime").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 曝光设置-ValueExposureTime由" + oldValue + "变为" + value + "\n");
                }

                HiKCamera.SetExposureTime(value);
            });
        }

        public float ValueExposureTime2
        {
            get => GetProperty(() => ValueExposureTime2);
            set => SetProperty(() => ValueExposureTime2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ValueExposureTime2").ElementAt(0).Value);
                    config.Descendants("ValueExposureTime2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 曝光设置-ValueExposureTime2由" + oldValue + "变为" + value + "\n");
                }

                HiKCamera2.SetExposureTime(value);
            });
        }
      

        /// <summary>
        /// 增益设置
        /// </summary>
        public float ValueGain
        {
            get => GetProperty(() => ValueGain);
            set => SetProperty(() => ValueGain, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ValueGain").ElementAt(0).Value);
                    config.Descendants("ValueGain").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 增益设置-ValueGain由" + oldValue + "变为" + value + "\n");
                }

                HiKCamera.SetGainValue(value);
            });
        }

        public float ValueGain2
        {
            get => GetProperty(() => ValueGain2);
            set => SetProperty(() => ValueGain2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ValueGain2").ElementAt(0).Value);
                    config.Descendants("ValueGain2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 增益设置-ValueGain2由" + oldValue + "变为" + value + "\n");
                }

                HiKCamera2.SetGainValue(value);
            });
        }



        /// <summary>
        /// 扫描方向
        /// </summary>
        public int ScanDirection
        {
            get => GetProperty(() => ScanDirection);
            set => SetProperty(() => ScanDirection, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    int oldValue = int.Parse(config.Descendants("ScanDirection").ElementAt(0).Value);
                    config.Descendants("ScanDirection").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 扫描方向-ScanDirection由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public int ScanDirection2
        {
            get => GetProperty(() => ScanDirection2);
            set => SetProperty(() => ScanDirection2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    int oldValue = int.Parse(config.Descendants("ScanDirection2").ElementAt(0).Value);
                    config.Descendants("ScanDirection2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 扫描方向-ScanDirection2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 梯度方向
        /// </summary>
        public int TiduDirection
        {
            get => GetProperty(() => TiduDirection);
            set => SetProperty(() => TiduDirection, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    int oldValue = int.Parse(config.Descendants("TiduDirection").ElementAt(0).Value);
                    config.Descendants("TiduDirection").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 梯度方向-TiduDirection由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public int TiduDirection2
        {
            get => GetProperty(() => TiduDirection2);
            set => SetProperty(() => TiduDirection2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    int oldValue = int.Parse(config.Descendants("TiduDirection2").ElementAt(0).Value);
                    config.Descendants("TiduDirection2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 梯度方向-TiduDirection2由" + oldValue + "变为" + value + "\n");
                }

            });
        }





        /// <summary>
        /// 相机
        /// </summary>  
        public int CamNum
        {
            get => GetProperty(() => CamNum);
            set => SetProperty(() => CamNum, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    int oldValue = int.Parse(config.Descendants("CamNum").ElementAt(0).Value);
                    config.Descendants("CamNum").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 相机-CamNum由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public int CamNum2
        {
            get => GetProperty(() => CamNum2);
            set => SetProperty(() => CamNum2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    int oldValue = int.Parse(config.Descendants("CamNum2").ElementAt(0).Value);
                    config.Descendants("CamNum2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 相机-CamNum2由" + oldValue + "变为" + value + "\n");
                }

            });
        }


        /// <summary>
        /// 定位X
        /// </summary>
        public float DefultX
        {
            get => GetProperty(() => DefultX);
            set => SetProperty(() => DefultX, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("DefultX").ElementAt(0).Value);
                    config.Descendants("DefultX").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位X-DefultX由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float DefultX2
        {
            get => GetProperty(() => DefultX2);
            set => SetProperty(() => DefultX2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("DefultX2").ElementAt(0).Value);
                    config.Descendants("DefultX2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位X-DefultX2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 定位Y
        /// </summary>
        public float DefultY
        {
            get => GetProperty(() => DefultY);
            set => SetProperty(() => DefultY, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("DefultY").ElementAt(0).Value);
                    config.Descendants("DefultY").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位Y-DefultY由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float DefultY2
        {
            get => GetProperty(() => DefultY2);
            set => SetProperty(() => DefultY2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("DefultY2").ElementAt(0).Value);
                    config.Descendants("DefultY2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位Y-DefultY2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 定位内界限
        /// </summary>
        public float MinRadius
        {
            get => GetProperty(() => MinRadius);
            set => SetProperty(() => MinRadius, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadius").ElementAt(0).Value);
                    config.Descendants("MinRadius").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位内界限-MinRadius由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float MinRadius2
        {
            get => GetProperty(() => MinRadius2);
            set => SetProperty(() => MinRadius2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadius2").ElementAt(0).Value);
                    config.Descendants("MinRadius2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位内界限-MinRadius2由" + oldValue + "变为" + value + "\n");
                }

            });
        }



        /// <summary>
        /// 定位外界限
        /// </summary>
        public float MaxRadius
        {
            get => GetProperty(() => MaxRadius);
            set => SetProperty(() => MaxRadius, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadius").ElementAt(0).Value);
                    config.Descendants("MaxRadius").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位外界限-MaxRadius由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float MaxRadius2
        {
            get => GetProperty(() => MaxRadius2);
            set => SetProperty(() => MaxRadius2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadius2").ElementAt(0).Value);
                    config.Descendants("MaxRadius2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位外界限-MaxRadius2由" + oldValue + "变为" + value + "\n");
                }

            });
        }



        /// <summary>
        /// 定位灰度值
        /// </summary>
        public float GrayValue
        {
            get => GetProperty(() => GrayValue);
            set => SetProperty(() => GrayValue, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValue").ElementAt(0).Value);
                    config.Descendants("GrayValue").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位灰度值-GrayValue由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float GrayValue2
        {
            get => GetProperty(() => GrayValue2);
            set => SetProperty(() => GrayValue2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValue2").ElementAt(0).Value);
                    config.Descendants("GrayValue2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 定位灰度值-GrayValue2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 角度内界限
        /// </summary>
        public float MinRadiusAng
        {
            get => GetProperty(() => MinRadiusAng);
            set => SetProperty(() => MinRadiusAng, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusAng").ElementAt(0).Value);
                    config.Descendants("MinRadiusAng").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度内界限-MinRadiusAng由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float MinRadiusAng2
        {
            get => GetProperty(() => MinRadiusAng2);
            set => SetProperty(() => MinRadiusAng2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusAng2").ElementAt(0).Value);
                    config.Descendants("MinRadiusAng2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度内界限-MinRadiusAng2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 角度外界限
        /// </summary>
        public float MaxRadiusAng
        {
            get => GetProperty(() => MaxRadiusAng);
            set => SetProperty(() => MaxRadiusAng, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusAng").ElementAt(0).Value);
                    config.Descendants("MaxRadiusAng").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度外界限-MaxRadiusAng由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float MaxRadiusAng2
        {
            get => GetProperty(() => MaxRadiusAng2);
            set => SetProperty(() => MaxRadiusAng2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusAng2").ElementAt(0).Value);
                    config.Descendants("MaxRadiusAng2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度外界限-MaxRadiusAng2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 角度灰度值
        /// </summary>
        public float GrayValueAng
        {
            get => GetProperty(() => GrayValueAng);
            set => SetProperty(() => GrayValueAng, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueAng").ElementAt(0).Value);
                    config.Descendants("GrayValueAng").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度灰度值-GrayValueAng由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float GrayValueAng2
        {
            get => GetProperty(() => GrayValueAng2);
            set => SetProperty(() => GrayValueAng2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueAng2").ElementAt(0).Value);
                    config.Descendants("GrayValueAng2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度灰度值-GrayValueAng2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 角度标记点类型
        /// </summary>
        public float MarkMode
        {
            get => GetProperty(() => MarkMode);
            set => SetProperty(() => MarkMode, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MarkMode").ElementAt(0).Value);
                    config.Descendants("MarkMode").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度标记点类型-MarkMode由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float MarkMode2
        {
            get => GetProperty(() => MarkMode2);
            set => SetProperty(() => MarkMode2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MarkMode2").ElementAt(0).Value);
                    config.Descendants("MarkMode2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度标记点类型-MarkMode2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 角度面积规格下限
        /// </summary>
        public float SpecAreaAng
        {
            get => GetProperty(() => SpecAreaAng);
            set => SetProperty(() => SpecAreaAng, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecAreaAng").ElementAt(0).Value);
                    config.Descendants("SpecAreaAng").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度面积规格下限-SpecAreaAng由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float SpecAreaAng2
        {
            get => GetProperty(() => SpecAreaAng2);
            set => SetProperty(() => SpecAreaAng2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecAreaAng2").ElementAt(0).Value);
                    config.Descendants("SpecAreaAng2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度面积规格下限-SpecAreaAng2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 角度面积规格上限
        /// </summary>
        public float SpecAreaAngSX
        {
            get => GetProperty(() => SpecAreaAngSX);
            set => SetProperty(() => SpecAreaAngSX, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecAreaAngSX").ElementAt(0).Value);
                    config.Descendants("SpecAreaAngSX").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度面积规格上限-SpecAreaAngSX由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float SpecAreaAngSX2
        {
            get => GetProperty(() => SpecAreaAngSX2);
            set => SetProperty(() => SpecAreaAngSX2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecAreaAngSX2").ElementAt(0).Value);
                    config.Descendants("SpecAreaAngSX2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 角度面积规格上限-SpecAreaAngSX2由" + oldValue + "变为" + value + "\n");
                }

            });
        }


        /// <summary>
        /// 未找到标记点报警类型
        /// </summary>
        public float MarkErroMode
        {
            get => GetProperty(() => MarkErroMode);
            set => SetProperty(() => MarkErroMode, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MarkErroMode").ElementAt(0).Value);
                    config.Descendants("MarkErroMode").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 未找到标记点报警类型-MarkErroMode由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        public float MarkErroMode2
        {
            get => GetProperty(() => MarkErroMode2);
            set => SetProperty(() => MarkErroMode2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MarkErroMode2").ElementAt(0).Value);
                    config.Descendants("MarkErroMode2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 未找到标记点报警类型-MarkErroMode2由" + oldValue + "变为" + value + "\n");
                }

            });
        }



        //镜筒测试区域参数


        /// <summary>
        /// JT定位检测设置-基准X
        /// </summary>
        public float ReferenceX
        {
            get => GetProperty(() => ReferenceX);
            set => SetProperty(() => ReferenceX, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ReferenceX").ElementAt(0).Value);
                    config.Descendants("ReferenceX").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 基准X-ReferenceX由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT定位检测设置-基准Y
        /// </summary>
        public float ReferenceY
        {
            get => GetProperty(() => ReferenceY);
            set => SetProperty(() => ReferenceY, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ReferenceY").ElementAt(0).Value);
                    config.Descendants("ReferenceY").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 基准Y-ReferenceY由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT定位检测设置-基准规格
        /// </summary>
        public float ReferenceSpec
        {
            get => GetProperty(() => ReferenceSpec);
            set => SetProperty(() => ReferenceSpec, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ReferenceSpec").ElementAt(0).Value);
                    config.Descendants("ReferenceSpec").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 基准规格-ReferenceSpec由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT漏组检测设置
        /// </summary>
        public float MinRadiusA1
        {
            get => GetProperty(() => MinRadiusA1);
            set => SetProperty(() => MinRadiusA1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusA1").ElementAt(0).Value);
                    config.Descendants("MinRadiusA1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 漏组检测设置-MinRadiusA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT漏组检测设置
        /// </summary>
        public float MaxRadiusA1
        {
            get => GetProperty(() => MaxRadiusA1);
            set => SetProperty(() => MaxRadiusA1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusA1").ElementAt(0).Value);
                    config.Descendants("MaxRadiusA1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 漏组检测设置-MaxRadiusA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT漏组检测设置
        /// </summary>
        public float GrayValueA1
        {
            get => GetProperty(() => GrayValueA1);
            set => SetProperty(() => GrayValueA1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueA1").ElementAt(0).Value);
                    config.Descendants("GrayValueA1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 漏组检测设置-GrayValueA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT漏组检测设置
        /// </summary>
        public float CompareA1
        {
            get => GetProperty(() => CompareA1);
            set => SetProperty(() => CompareA1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("CompareA1").ElementAt(0).Value);
                    config.Descendants("CompareA1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 漏组检测设置-CompareA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT漏组检测设置
        /// </summary>
        public float SpecValueA1
        {
            get => GetProperty(() => SpecValueA1);
            set => SetProperty(() => SpecValueA1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueA1").ElementAt(0).Value);
                    config.Descendants("SpecValueA1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 漏组检测设置-SpecValueA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT挡光片检测设置
        /// </summary>
        public float MinRadiusA2
        {
            get => GetProperty(() => MinRadiusA2);
            set => SetProperty(() => MinRadiusA2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusA2").ElementAt(0).Value);
                    config.Descendants("MinRadiusA2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 挡光片检测设置-MinRadiusA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT挡光片检测设置
        /// </summary>
        public float MaxRadiusA2
        {
            get => GetProperty(() => MaxRadiusA2);
            set => SetProperty(() => MaxRadiusA2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusA2").ElementAt(0).Value);
                    config.Descendants("MaxRadiusA2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 挡光片检测设置-MaxRadiusA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT挡光片检测设置
        /// </summary>
        public float GrayValueA2
        {
            get => GetProperty(() => GrayValueA2);
            set => SetProperty(() => GrayValueA2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueA2").ElementAt(0).Value);
                    config.Descendants("GrayValueA2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 挡光片检测设置-GrayValueA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT挡光片检测设置
        /// </summary>
        public float CompareA2
        {
            get => GetProperty(() => CompareA2);
            set => SetProperty(() => CompareA2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("CompareA2").ElementAt(0).Value);
                    config.Descendants("CompareA2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 挡光片检测设置-CompareA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT挡光片检测设置
        /// </summary>
        public float SpecValueA2
        {
            get => GetProperty(() => SpecValueA2);
            set => SetProperty(() => SpecValueA2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueA2").ElementAt(0).Value);
                    config.Descendants("SpecValueA2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 挡光片检测设置-SpecValueA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测设置
        /// </summary>
        public float MinRadiusA3
        {
            get => GetProperty(() => MinRadiusA3);
            set => SetProperty(() => MinRadiusA3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusA3").ElementAt(0).Value);
                    config.Descendants("MinRadiusA3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测设置-MinRadiusA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测设置
        /// </summary>
        public float MaxRadiusA3
        {
            get => GetProperty(() => MaxRadiusA3);
            set => SetProperty(() => MaxRadiusA3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusA3").ElementAt(0).Value);
                    config.Descendants("MaxRadiusA3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测设置-MaxRadiusA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测设置
        /// </summary>
        public float GrayValueA3
        {
            get => GetProperty(() => GrayValueA3);
            set => SetProperty(() => GrayValueA3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueA3").ElementAt(0).Value);
                    config.Descendants("GrayValueA3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测设置-GrayValueA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测设置
        /// </summary>
        public float CompareA3
        {
            get => GetProperty(() => CompareA3);
            set => SetProperty(() => CompareA3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("CompareA3").ElementAt(0).Value);
                    config.Descendants("CompareA3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测设置-CompareA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测设置
        /// </summary>
        public float SpecValueA3
        {
            get => GetProperty(() => SpecValueA3);
            set => SetProperty(() => SpecValueA3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueA3").ElementAt(0).Value);
                    config.Descendants("SpecValueA3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测设置-SpecValueA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测2设置
        /// </summary>
        public float MinRadiusA4
        {
            get => GetProperty(() => MinRadiusA4);
            set => SetProperty(() => MinRadiusA4, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusA4").ElementAt(0).Value);
                    config.Descendants("MinRadiusA4").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测2设置-MinRadiusA4由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测2设置
        /// </summary>
        public float MaxRadiusA4
        {
            get => GetProperty(() => MaxRadiusA4);
            set => SetProperty(() => MaxRadiusA4, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusA4").ElementAt(0).Value);
                    config.Descendants("MaxRadiusA4").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测2设置-MaxRadiusA4由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测2设置
        /// </summary>
        public float GrayValueA4
        {
            get => GetProperty(() => GrayValueA4);
            set => SetProperty(() => GrayValueA4, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueA4").ElementAt(0).Value);
                    config.Descendants("GrayValueA4").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测2设置-GrayValueA4由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测2设置
        /// </summary>
        public float CompareA4
        {
            get => GetProperty(() => CompareA4);
            set => SetProperty(() => CompareA4, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("CompareA4").ElementAt(0).Value);
                    config.Descendants("CompareA4").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测2设置-CompareA4由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JT组装检测2设置
        /// </summary>
        public float SpecValueA4
        {
            get => GetProperty(() => SpecValueA4);
            set => SetProperty(() => SpecValueA4, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueA4").ElementAt(0).Value);
                    config.Descendants("SpecValueA4").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装检测2设置-SpecValueA4由" + oldValue + "变为" + value + "\n");
                }

            });
        }



        //镜片测试区域参数

        /// <summary>
        /// JP定位检测设置-基准X
        /// </summary>
        public float ReferenceX2
        {
            get => GetProperty(() => ReferenceX2);
            set => SetProperty(() => ReferenceX2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ReferenceX2").ElementAt(0).Value);
                    config.Descendants("ReferenceX2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP定位检测设置-基准X-ReferenceX2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP定位检测设置-基准Y
        /// </summary>
        public float ReferenceY2
        {
            get => GetProperty(() => ReferenceY2);
            set => SetProperty(() => ReferenceY2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ReferenceY2").ElementAt(0).Value);
                    config.Descendants("ReferenceY2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP定位检测设置-基准Y-ReferenceY2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP定位检测设置-基准规格
        /// </summary>
        public float ReferenceSpec2
        {
            get => GetProperty(() => ReferenceSpec2);
            set => SetProperty(() => ReferenceSpec2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("ReferenceSpec2").ElementAt(0).Value);
                    config.Descendants("ReferenceSpec2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP定位检测设置-基准规格-ReferenceSpec2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP正反检测设置
        /// </summary>
        public float MinRadiusB1
        {
            get => GetProperty(() => MinRadiusB1);
            set => SetProperty(() => MinRadiusB1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusB1").ElementAt(0).Value);
                    config.Descendants("MinRadiusB1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP正反检测设置-MinRadiusB1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP正反检测设置
        /// </summary>
        public float MaxRadiusB1
        {
            get => GetProperty(() => MaxRadiusB1);
            set => SetProperty(() => MaxRadiusB1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusB1").ElementAt(0).Value);
                    config.Descendants("MaxRadiusB1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP正反检测设置-MaxRadiusB1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP正反检测设置
        /// </summary>
        public float GrayValueB1
        {
            get => GetProperty(() => GrayValueB1);
            set => SetProperty(() => GrayValueB1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueB1").ElementAt(0).Value);
                    config.Descendants("GrayValueB1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP正反检测设置-GrayValueB1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP正反检测设置
        /// </summary>
        public float CompareB1
        {
            get => GetProperty(() => CompareB1);
            set => SetProperty(() => CompareB1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("CompareB1").ElementAt(0).Value);
                    config.Descendants("CompareB1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP正反检测设置-CompareB1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP正反检测设置
        /// </summary>
        public float SpecValueB1
        {
            get => GetProperty(() => SpecValueB1);
            set => SetProperty(() => SpecValueB1, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueB1").ElementAt(0).Value);
                    config.Descendants("SpecValueB1").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP正反检测设置-SpecValueB1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP涂黑检测设置
        /// </summary>
        public float MinRadiusB2
        {
            get => GetProperty(() => MinRadiusB2);
            set => SetProperty(() => MinRadiusB2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusB2").ElementAt(0).Value);
                    config.Descendants("MinRadiusB2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP涂黑检测设置-MinRadiusB2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP涂黑检测设置
        /// </summary>
        public float MaxRadiusB2
        {
            get => GetProperty(() => MaxRadiusB2);
            set => SetProperty(() => MaxRadiusB2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusB2").ElementAt(0).Value);
                    config.Descendants("MaxRadiusB2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP涂黑检测设置-MaxRadiusB2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP涂黑检测设置
        /// </summary>
        public float GrayValueB2
        {
            get => GetProperty(() => GrayValueB2);
            set => SetProperty(() => GrayValueB2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueB2").ElementAt(0).Value);
                    config.Descendants("GrayValueB2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP涂黑检测设置-GrayValueB2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP涂黑检测设置
        /// </summary>
        public float CompareB2
        {
            get => GetProperty(() => CompareB2);
            set => SetProperty(() => CompareB2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("CompareB2").ElementAt(0).Value);
                    config.Descendants("CompareB2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP涂黑检测设置-CompareB2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP涂黑检测设置
        /// </summary>
        public float SpecValueB2
        {
            get => GetProperty(() => SpecValueB2);
            set => SetProperty(() => SpecValueB2, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueB2").ElementAt(0).Value);
                    config.Descendants("SpecValueB2").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP涂黑检测设置-SpecValueB2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP部品检测设置
        /// </summary>
        public float MinRadiusB3
        {
            get => GetProperty(() => MinRadiusB3);
            set => SetProperty(() => MinRadiusB3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MinRadiusB3").ElementAt(0).Value);
                    config.Descendants("MinRadiusB3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP部品检测设置-MinRadiusB3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP部品检测设置
        /// </summary>
        public float MaxRadiusB3
        {
            get => GetProperty(() => MaxRadiusB3);
            set => SetProperty(() => MaxRadiusB3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("MaxRadiusB3").ElementAt(0).Value);
                    config.Descendants("MaxRadiusB3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP部品检测设置-MaxRadiusB3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP部品检测设置
        /// </summary>
        public float GrayValueB3
        {
            get => GetProperty(() => GrayValueB3);
            set => SetProperty(() => GrayValueB3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("GrayValueB3").ElementAt(0).Value);
                    config.Descendants("GrayValueB3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP部品检测设置-GrayValueB3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP部品检测设置
        /// </summary>
        public float CompareB3
        {
            get => GetProperty(() => CompareB3);
            set => SetProperty(() => CompareB3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("CompareB3").ElementAt(0).Value);
                    config.Descendants("CompareB3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP部品检测设置-CompareB3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// JP部品检测设置
        /// </summary>
        public float SpecValueB3
        {
            get => GetProperty(() => SpecValueB3);
            set => SetProperty(() => SpecValueB3, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueB3").ElementAt(0).Value);
                    config.Descendants("SpecValueB3").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " JP部品检测设置-SpecValueB3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 组装角度设置
        /// </summary>
        public float AngZuzhuang
        {
            get => GetProperty(() => AngZuzhuang);
            set => SetProperty(() => AngZuzhuang, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("AngZuzhuang").ElementAt(0).Value);
                    config.Descendants("AngZuzhuang").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 组装角度设置-AngZuzhuang由" + oldValue + "变为" + value + "\n");
                }
                    
            });
        }

        #endregion

        #region 前台参数显示

        /// <summary>
        /// JT角度显示
        /// </summary>
        public float AngJT
        {
            get => GetProperty(() => AngJT);
            set => SetProperty(() => AngJT, value);
        }

        /// <summary>
        /// JP角度显示
        /// </summary>
        public float AngJP
        {
            get => GetProperty(() => AngJP);
            set => SetProperty(() => AngJP, value);
        }

        /// <summary>
        /// 偏移角度显示
        /// </summary>
        public float AngSend
        {
            get => GetProperty(() => AngSend);
            set => SetProperty(() => AngSend, value);
        }

        /// <summary>
        /// X偏移
        /// </summary>
        public float Xdeviation
        {
            get => GetProperty(() => Xdeviation);
            set => SetProperty(() => Xdeviation, value);
        }

        public float Xdeviation2
        {
            get => GetProperty(() => Xdeviation2);
            set => SetProperty(() => Xdeviation2, value);
        }

        /// <summary>
        /// Y偏移
        /// </summary>
        public float Ydeviation
        {
            get => GetProperty(() => Ydeviation);
            set => SetProperty(() => Ydeviation, value);
        }

        public float Ydeviation2
        {
            get => GetProperty(() => Ydeviation2);
            set => SetProperty(() => Ydeviation2, value);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        public string resultCalculation
        {
            get => GetProperty(() => resultCalculation);
            set => SetProperty(() => resultCalculation, value);
        }

        /// <summary>
        /// 结果颜色
        /// </summary>
        public string DetecColor
        {
            get => GetProperty(() => DetecColor);
            set => SetProperty(() => DetecColor, value);
        }

        /// <summary>
        /// 结果颜色2
        /// </summary>
        public string DetecColor2
        {
            get => GetProperty(() => DetecColor2);
            set => SetProperty(() => DetecColor2, value);
        }

        /// <summary>
        /// 结果
        /// </summary>
        public string DetecResult
        {
            get => GetProperty(() => DetecResult);
            set => SetProperty(() => DetecResult, value);
        }

        /// <summary>
        /// 结果2
        /// </summary>
        public string DetecResult2
        {
            get => GetProperty(() => DetecResult2);
            set => SetProperty(() => DetecResult2, value);
        }

        /// <summary>
        /// 捕捉圆心X
        /// </summary>
        public float ActualX
        {
            get => GetProperty(() => ActualX);
            set => SetProperty(() => ActualX, value);
        }

        /// <summary>
        /// 捕捉圆心X2
        /// </summary>
        public float ActualX2
        {
            get => GetProperty(() => ActualX2);
            set => SetProperty(() => ActualX2, value);
        }

        /// <summary>
        /// 捕捉圆心Y
        /// </summary>
        public float ActualY
        {
            get => GetProperty(() => ActualY);
            set => SetProperty(() => ActualY, value);
        }

        /// <summary>
        /// 捕捉圆心Y2
        /// </summary>
        public float ActualY2
        {
            get => GetProperty(() => ActualY2);
            set => SetProperty(() => ActualY2, value);
        }

        /// <summary>
        /// 基准实测
        /// </summary>
        public float ReferenceActual
        {
            get => GetProperty(() => ReferenceActual);
            set => SetProperty(() => ReferenceActual, value);
        }

        /// <summary>
        /// 基准实测2
        /// </summary>
        public float ReferenceActual2
        {
            get => GetProperty(() => ReferenceActual2);
            set => SetProperty(() => ReferenceActual2, value);
        }

        /// <summary>
        /// 实测轮廓数
        /// </summary>
        public float Index
        {
            get => GetProperty(() => Index);
            set => SetProperty(() => Index, value);
        }

        /// <summary>
        /// 实测轮廓数2
        /// </summary>
        public float Index2
        {
            get => GetProperty(() => Index2);
            set => SetProperty(() => Index2, value);
        }

        /// <summary>
        /// 实测最大面积
        /// </summary>
        public float MaxAreaAng
        {
            get => GetProperty(() => MaxAreaAng);
            set => SetProperty(() => MaxAreaAng, value);
        }

        /// <summary>
        /// 实测最大面积2
        /// </summary>
        public float MaxAreaAng2
        {
            get => GetProperty(() => MaxAreaAng2);
            set => SetProperty(() => MaxAreaAng2, value);
        }

        /// <summary>
        /// 实测值A1
        /// </summary>
        public float ValueActualA1
        {
            get => GetProperty(() => ValueActualA1);
            set => SetProperty(() => ValueActualA1, value);
        }

        /// <summary>
        /// 实测值A2
        /// </summary>
        public float ValueActualA2
        {
            get => GetProperty(() => ValueActualA2);
            set => SetProperty(() => ValueActualA2, value);
        }

        /// <summary>
        /// 实测值A3
        /// </summary>
        public float ValueActualA3
        {
            get => GetProperty(() => ValueActualA3);
            set => SetProperty(() => ValueActualA3, value);
        }

        /// <summary>
        /// 实测值A4
        /// </summary>
        public float ValueActualA4
        {
            get => GetProperty(() => ValueActualA4);
            set => SetProperty(() => ValueActualA4, value);
        }

        /// <summary>
        /// 实测值B1
        /// </summary>
        public float ValueActualB1
        {
            get => GetProperty(() => ValueActualB1);
            set => SetProperty(() => ValueActualB1, value);
        }

        /// <summary>
        /// 实测值B2
        /// </summary>
        public float ValueActualB2
        {
            get => GetProperty(() => ValueActualB2);
            set => SetProperty(() => ValueActualB2, value);
        }

        /// <summary>
        /// 实测值B3
        /// </summary>
        public float ValueActualB3
        {
            get => GetProperty(() => ValueActualB3);
            set => SetProperty(() => ValueActualB3, value);
        }

        #endregion

        #region 画面显示
        /// <summary>
        /// 线程锁对象
        /// </summary>
        private static readonly object objlock = new object();
        private static readonly object objlock2 = new object();

        /// <summary>
        /// 画面显示缓冲
        /// </summary>
        CVAlgorithms.BmpBuf bmpBuf = new CVAlgorithms.BmpBuf();
        CVAlgorithms.BmpBuf bmpBuf2 = new CVAlgorithms.BmpBuf();

        /// <summary>
        /// JT线程执行函数
        /// </summary>
        /// <param name="i"></param>
        /// <param name="e"></param>
        public bool isBusy = false;
        public int Comflag = 0;
        public float JTResultflag =0;
        private void MV_STPAction(int i, HiKhelper.MV_IM_INFO e)
        {
            isBusy = true;
            bmpBuf.Width = e.width;
            bmpBuf.Height = e.height;
            bmpBuf.pData_IntPtr = e.pData;
            float[] outParam = new float[20];//参数初始化
            string[] inParam = new string[50];

            //从前台获取输入的参数
            inParam[0] = ShowMode.ToString();
            inParam[1] = ScanDirection.ToString();

            inParam[2] = DefultX.ToString();
            inParam[3] = DefultY.ToString();
            inParam[4] = MinRadius.ToString();
            inParam[5] = MaxRadius.ToString();
            inParam[6] = GrayValue.ToString();

            inParam[8] = MinRadiusAng.ToString();
            inParam[9] = MaxRadiusAng.ToString();
            inParam[10] = GrayValueAng.ToString();
            inParam[11] = MarkMode.ToString();
            inParam[12] = SpecAreaAng.ToString();

            inParam[13] = ReferenceX.ToString();
            inParam[14] = ReferenceY.ToString();
            inParam[15] = ReferenceSpec.ToString();

            inParam[16] = MinRadiusA1.ToString();
            inParam[17] = MaxRadiusA1.ToString();
            inParam[18] = GrayValueA1.ToString();
            inParam[19] = CompareA1.ToString();
            inParam[20] = SpecValueA1.ToString();

            inParam[21] = MinRadiusA2.ToString();
            inParam[22] = MaxRadiusA2.ToString();
            inParam[23] = GrayValueA2.ToString();
            inParam[24] = CompareA2.ToString();
            inParam[25] = SpecValueA2.ToString();

            inParam[26] = MinRadiusA3.ToString();
            inParam[27] = MaxRadiusA3.ToString();
            inParam[28] = GrayValueA3.ToString();
            inParam[29] = CompareA3.ToString();
            inParam[30] = SpecValueA3.ToString();

            inParam[31] = MinRadiusA4.ToString();
            inParam[32] = MaxRadiusA4.ToString();
            inParam[33] = GrayValueA4.ToString();
            inParam[34] = CompareA4.ToString();
            inParam[35] = SpecValueA4.ToString();

            inParam[36] = SpecAreaAngSX.ToString();
            inParam[37] = MarkErroMode.ToString();

            inParam[38] = TiduDirection.ToString();

            CVAlgorithms.MV_EntryPoint(i, ref bmpBuf, inParam, ref outParam[0]);//把图像和参数传进C++算法里面去，ref outParam[0]返回的数据，如果要用，就在下面再写程序
            
            //显示//从C++里面处理完的图像和数据在前台显示出来
            Application.Current.Dispatcher.Invoke(() =>//申请同步线程
            {
                int size = (int)bmpBuf.size;//把图像大小给size
                try
                {
                    if (ImSrc_test == null || ImSrc_test.Width != bmpBuf.Width || ImSrc_test.Height != bmpBuf.Height)//如果图像是空的或者大小与现有图像大小：长宽不匹配
                    {
                        if (size > 3 * bmpBuf.Width * bmpBuf.Height / 2)
                            ImSrc_test = new WriteableBitmap(bmpBuf.Width, bmpBuf.Height, 96.0, 96.0, PixelFormats.Bgr24, null);//重新申请彩色图像
                        else
                            ImSrc_test = new WriteableBitmap(bmpBuf.Width, bmpBuf.Height, 24.0, 24.0, PixelFormats.Gray8, null);////重新申请黑白图像
                    }

                    lock (objlock)//上锁处理完当前图像
                    {
                        if ((e.pData != (IntPtr)0x00000000))//图像数据不等于空
                        {
                            ImSrc_test.Lock();
                            ImSrc_test.WritePixels(new Int32Rect(0, 0, bmpBuf.Width, bmpBuf.Height), bmpBuf.pData, size, ImSrc_test.BackBufferStride);
                            ImSrc_test.AddDirtyRect(new Int32Rect(0, 0, bmpBuf.Width, bmpBuf.Height));
                            ImSrc_test.Unlock();
                        }
                    }
                }
                catch 
                {
                    //前台刷新失败
                }
                CVAlgorithms.MV_Release(ref bmpBuf);

                //C++处理结果返回
                JTResultflag = outParam[0];
                ActualX = outParam[1];
                ActualY = outParam[2];
                MaxAreaAng = outParam[3];
                Index = outParam[4];
                AngJT = outParam[5];
                ReferenceActual = outParam[6];
                ValueActualA1 = outParam[7];
                ValueActualA2 = outParam[8];
                ValueActualA3 = outParam[9];
                ValueActualA4 = outParam[10];

                //实际移动X轴丝杆,图像实际y坐标在移动,坐标xy刚好反的         
                //MeasuringScaleX 比例尺X：就是镜筒移动跟丝杆的关系，丝杆X或Y移动1mm，视觉X或Y移动的像素差
                //MeasuringScaleY 比例尺Y：就是镜片移动跟丝杆的关系，丝杆X或Y移动1mm，视觉X或Y移动的像素差
                Xdeviation = (ReferenceY - ActualY)/ MeasuringScaleX;
                Ydeviation = (ActualX - ReferenceX)/ MeasuringScaleX;


                if (JTResultflag==1)
                {
                    DetecColor = "Green";
                    DetecResult = "OK";
                }
                else
                {
                    DetecColor = "Red";
                    DetecResult = "NG";
                }

                isBusy = false;
                Comflag = 1;
            });
        }

        /// <summary>
        /// JP线程执行函数2
        /// </summary>
        /// <param name="i"></param>
        /// <param name="e"></param>
        public bool isBusy2=false;
        public int Comflag2 = 0;
        public float JPResultflag = 0;
        private void MV_STPAction2(int i, HiKhelper.MV_IM_INFO e)
        {
            isBusy2 = true;
            bmpBuf2.Width = e.width;
            bmpBuf2.Height = e.height;
            bmpBuf2.pData_IntPtr = e.pData;
            float[] outParam = new float[20];//参数初始化
            string[] inParam = new string[50];

            //从前台获取输入的参数
            inParam[0] = ShowMode.ToString();
            inParam[1] = ScanDirection2.ToString();

            inParam[2] = DefultX2.ToString();
            inParam[3] = DefultY2.ToString();
            inParam[4] = MinRadius2.ToString();
            inParam[5] = MaxRadius2.ToString();           
            inParam[6] = GrayValue2.ToString();

            inParam[8] = MinRadiusAng2.ToString();
            inParam[9] = MaxRadiusAng2.ToString();         
            inParam[10] = GrayValueAng2.ToString();
            inParam[11] = MarkMode2.ToString();
            inParam[12] = SpecAreaAng2.ToString();


            inParam[13] = ReferenceX2.ToString();
            inParam[14] = ReferenceY2.ToString();
            inParam[15] = ReferenceSpec2.ToString();

            inParam[16] = MinRadiusB1.ToString();
            inParam[17] = MaxRadiusB1.ToString();
            inParam[18] = GrayValueB1.ToString();
            inParam[19] = CompareB1.ToString();
            inParam[20] = SpecValueB1.ToString();

            inParam[21] = MinRadiusB2.ToString();
            inParam[22] = MaxRadiusB2.ToString();
            inParam[23] = GrayValueB2.ToString();
            inParam[24] = CompareB2.ToString();
            inParam[25] = SpecValueB2.ToString();

            inParam[26] = MinRadiusB3.ToString();
            inParam[27] = MaxRadiusB3.ToString();
            inParam[28] = GrayValueB3.ToString();
            inParam[29] = CompareB3.ToString();
            inParam[30] = SpecValueB3.ToString();

            inParam[31] = SpecAreaAngSX2.ToString();
            inParam[32] = MarkErroMode2.ToString();

            inParam[33] = TiduDirection2.ToString();

            CVAlgorithms.MV_EntryPoint(i, ref bmpBuf2, inParam, ref outParam[0]);//把图像和参数传进C++算法里面去，ref outParam[0]返回的数据，如果要用，就在下面再写程序
           

            //显示//从C++里面处理完的图像和数据在前台显示出来
            Application.Current.Dispatcher.Invoke(() =>//申请同步线程
            {
                int size = (int)bmpBuf2.size;//把图像大小给size
                try
                {
                    if (ImSrc_test2 == null || ImSrc_test2.Width != bmpBuf2.Width || ImSrc_test2.Height != bmpBuf2.Height)//如果图像是空的或者大小与现有图像大小：长宽不匹配
                    {
                        if (size > 3 * bmpBuf2.Width * bmpBuf2.Height / 2)
                            ImSrc_test2 = new WriteableBitmap(bmpBuf2.Width, bmpBuf2.Height, 96.0, 96.0, PixelFormats.Bgr24, null);//重新申请彩色图像
                        else
                            ImSrc_test2 = new WriteableBitmap(bmpBuf2.Width, bmpBuf2.Height, 24.0, 24.0, PixelFormats.Gray8, null);////重新申请黑白图像
                    }

                    lock (objlock2)//上锁处理完当前图像
                    {
                        if ((e.pData != (IntPtr)0x00000000))//图像数据不等于空
                        {
                            ImSrc_test2.Lock();
                            ImSrc_test2.WritePixels(new Int32Rect(0, 0, bmpBuf2.Width, bmpBuf2.Height), bmpBuf2.pData, size, ImSrc_test2.BackBufferStride);
                            ImSrc_test2.AddDirtyRect(new Int32Rect(0, 0, bmpBuf2.Width, bmpBuf2.Height));
                            ImSrc_test2.Unlock();

                        }
                    }
                }
                catch
                {
                    //前台刷新失败
                }
                CVAlgorithms.MV_Release(ref bmpBuf2);

                //C++处理结果返回
                JPResultflag = outParam[0];
                ActualX2 = outParam[1];
                ActualY2= outParam[2];
                MaxAreaAng2 = outParam[3];
                Index2 = outParam[4];
                AngJP = outParam[5];
                ReferenceActual2 = outParam[6];
                ValueActualB1 = outParam[7];
                ValueActualB2 = outParam[8];
                ValueActualB3 = outParam[9];

                //MeasuringScaleX 比例尺X：就是镜筒移动跟丝杆的关系，丝杆X或Y移动1mm，视觉X或Y移动的像素差
                //MeasuringScaleY 比例尺Y：就是镜片移动跟丝杆的关系，丝杆X或Y移动1mm，视觉X或Y移动的像素差
                Xdeviation2 = (ActualY2 - ReferenceY2)/ MeasuringScaleY;
                Ydeviation2 = (ReferenceX2 - ActualX2)/ MeasuringScaleY;

                if (JPResultflag == 1)
                {
                    DetecColor2 = "Green";
                    DetecResult2 = "OK";
                }
                else
                {
                    DetecColor2 = "Red";
                    DetecResult2 = "NG";
                }


                isBusy2 = false;
                Comflag2 = 1;
            });
        }

        /// <summary>
        /// 回调函数，回调帧为YUV格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hik_MV_OnOriFrameInvoked(object sender, HiKhelper.MV_IM_INFO e)//JT相机回调函数
        {
            if(!isBusy)
            {
                stp.QueueWorkItem(new Action<int, HiKhelper.MV_IM_INFO>(MV_STPAction), 0, e, WorkItemPriority.Normal);//插入线程值,0:选择算法，e:图像信息,线程优先值:正常值
            }                    
        }

        private void Hik_MV_OnOriFrameInvoked2(object sender, HiKhelper.MV_IM_INFO e)//JP相机回调函数
        {
           if(!isBusy2)
            {
                stp2.QueueWorkItem(new Action<int, HiKhelper.MV_IM_INFO>(MV_STPAction2), 1, e, WorkItemPriority.Normal);////插入线程值,0:选择算法，e:图像信息,线程优先值:正常值
            }
        }

        /// <summary>
        /// 智能线程池，用于处理回调图像算法
        /// </summary>
        private SmartThreadPool stp;
        private SmartThreadPool stp2;
        #endregion

        #region 角度计算算法
        //角度计算算法
        public float AngCalculation(float AngJT, float AngJP, float AngZuzhuang)
        {
            float AngJTJP = AngJT - AngJP;
            float Angout1 = 0;
            float angSend = 0;

            if (AngJTJP >= 0)
            {
                if (AngJTJP > 180) { Angout1 = AngJTJP - 360; }
                if (AngJTJP <= 180) { Angout1 = AngJTJP; }
            }
            else
            {
                if (Math.Abs(AngJTJP) > 180) { Angout1 = AngJTJP + 360; }
                if (Math.Abs(AngJTJP) <= 180) { Angout1 = AngJTJP; }
            }

            float Angout2 = Angout1 - AngZuzhuang;

            if (Angout2 >= 0)
            {
                if (Angout2 > 180) { angSend = Angout2 - 360; }
                if (Angout2 <= 180) { angSend = Angout2; }
            }
            else
            {
                if (Math.Abs(Angout2) > 180) { angSend = Angout2 + 360; }
                if (Math.Abs(Angout2) <= 180) { angSend = Angout2; }
            }

            AngSend = angSend;
            return AngSend;
        }

        #endregion

        #region 结果处理和发送
        //结果处理和发送
        public string ResultCalculation(float JTResultflag, float JPResultflag,  float Xdeviation, float Ydeviation, float Xdeviation2, float Ydeviation2, float AngSend)
        {
            float XdeviationSend=0;
            float YdeviationSend = 0;

            string XdeviationSendSymbol = "";
            string YdeviationSendSymbol = "";
            string AngSendSymbol = "";

            string resultCalculation = "";

            XdeviationSend = Xdeviation + Xdeviation2; 
            YdeviationSend = Ydeviation + Ydeviation2;

            if ( (BuchangMode == 0) || (Math.Abs(XdeviationSend) > BuchangSpec) || (Math.Abs(YdeviationSend) > BuchangSpec) )
            {
                XdeviationSend = 0;
                YdeviationSend = 0;
            }

            if (XdeviationSend >= 0)
            {
                XdeviationSendSymbol = "1";
            }
            else
            {
                XdeviationSendSymbol = "2";
                XdeviationSend = Math.Abs(XdeviationSend);
            }

            if (YdeviationSend >= 0)
            {
                YdeviationSendSymbol = "1";
            }
            else
            {
                YdeviationSendSymbol = "2";
                YdeviationSend = Math.Abs(YdeviationSend);
            }

            if (AngSend >= 0)
            {
                AngSendSymbol = "1";
            }
            else
            {
                AngSendSymbol = "2";
                AngSend = Math.Abs(AngSend);
            }

            if ((JTResultflag==1) && (JPResultflag == 1))//镜筒镜片OK
            {
                string _XdeviationSend = "";
                _XdeviationSend = string.Format("{0:000.000}", XdeviationSend);

                string _YdeviationSend = "";
                _YdeviationSend = string.Format("{0:000.000}", YdeviationSend);

                string _AngSend = "";
                _AngSend = string.Format("{0:000.000}", AngSend);

                resultCalculation = "10," + XdeviationSendSymbol + _XdeviationSend + "," + YdeviationSendSymbol + _YdeviationSend + "," + AngSendSymbol + _AngSend;
            }

            if ((JTResultflag == 1) && (JPResultflag == 0))//镜片NG
            {
                resultCalculation = "20,0000.000,0000.000,0000.000";
            }

            if ((JTResultflag == 0) && (JPResultflag == 1) || (JTResultflag == 0) && (JPResultflag == 0))//镜筒NG
            {
                resultCalculation = "30,0000.000,0000.000,0000.000";
            }

            return resultCalculation;
        }

        #endregion

        #region Socket
        public MySocket mySocket
        {
            get => GetProperty(() => mySocket);
            set => SetProperty(() => mySocket, value);
        }

        //Socket接收信息
        private void ReceiveDataInvoked(object sender, string e)
        {
            if (e == ReadSample)
            {
                if((!isBusy)&& (!isBusy2))
                {
                    Comflag = 0;
                    Comflag2 = 0;
                    HiKCamera.TriggerOnce();
                    HiKCamera2.TriggerOnce();

                    while (Comflag == 0 || Comflag2 == 0)
                    {
                        Thread.Sleep(20);
                    }

                    AngSend= AngCalculation(AngJT, AngJP, AngZuzhuang);
                    resultCalculation = ResultCalculation(JTResultflag, JPResultflag, Xdeviation, Ydeviation, Xdeviation2, Ydeviation2, AngSend);

                    //发送网口数据
                    byte[] data = Encoding.UTF8.GetBytes(resultCalculation);
                    mySocket.SocketSend(data);
                }
            }
        }
        #endregion
    }
}
