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
    public class SomaViewModel : ViewModelBase
    {
        #region 构造函数
        private SomaParaWindow somaParaWindow;
        public SomaViewModel(string _path, int n)
        {
            path = _path;
            stationNum = n;
            DetecColor = "Red";
            DetecResult = "NG";

            HiKCamera = new HiKhelper(); 
            mySocket = new MySocket(n);
            stp = new SmartThreadPool { MaxThreads = 1 };//SmartThreadPool 第三方库多线程管理

            HiKCamera.MV_OnOriFrameInvoked += Hik_MV_OnOriFrameInvoked;
            ImSrc_test = new WriteableBitmap(new BitmapImage(new Uri(@"./图片/null.png", UriKind.Relative)));//加载一个自定义的黑图像框框进来

            init();
            ButtonStr = "连接相机";
            CommboxID = new int[2] { 0, 1 };
            isConnect = false;
            //CamCommand(true);
            //TriggerCommand(true);
        }
        #endregion

        #region 相机相关
        /// <summary>
        /// 相机信息
        /// </summary>
        public HiKhelper HiKCamera;

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

        //相机列表
        public string[] Cameralist
        {
            get => GetProperty(() => Cameralist);
            set => SetProperty(() => Cameralist, value);
        }
        #endregion

        #region 前台操作
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
                if (somaParaWindow == null || !somaParaWindow.IsVisible)
                {
                    somaParaWindow = new SomaParaWindow(this);
                    somaParaWindow.Closed += (s, args) => somaParaWindow = null;
                    somaParaWindow.WindowClosed += _jTParaWindowView_WindowClosed;
                    somaParaWindow.Show();
                }
                else
                {
                    Growl.Warning("请勿重复打开窗口！");
                }

            }
        }

        private void _jTParaWindowView_WindowClosed(object sender, EventArgs e)
        {
            isEnabled = false;
        }


        /// <summary>
        /// 开关相机
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void CamCommand(object obj)
        {
            Growl.Clear();
            if (!HiKCamera.isConnect)
            {
                HiKCamera.Search();
                HiKCamera.Connect(CamNum);
                
                HiKCamera.SetExposureTime(ValueExposureTime);//相机连接后把保存的曝光参数先执行一次
                HiKCamera.SetGainValue(ValueGain);//相机连接后把保存的曝光参数先执行一次
                SamplingMode = 0;//相机连接后先默认为触发采样

                HiKCamera.TriggerOnce();//相机连接后开启软件先触发一次

                //打开Socekt服务器
                mySocket.Listen();
                mySocket.MV_onMess += ReceiveDataInvoked;
            }
            else
            {
                HiKCamera.Disconnect();
            }

            if (HiKCamera.isConnect)
            {
                ButtonStr = "断开相机";
            }
            else
            {
                ButtonStr = "连接相机";
            }
            isConnect = HiKCamera.isConnect;
        }

        /// <summary>
        /// 触发一次拍照
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void TriggerCommand(object obj)
        {
            int i = HiKCamera.TriggerOnce();
        }

        /// <summary>
        /// 退出软件
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void ExitCommand(object obj)
        {
            if (HiKCamera.isConnect)
            {
                HiKCamera.Disconnect();               
                mySocket.StopListen();//关闭网络连接
            }
            Thread.Sleep(200);
            Application.Current.Shutdown(-1);
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
                classEle = new XElement("ValueGain", ValueGain);
                rootEle.Add(classEle);

                classEle = new XElement("ScanDirection", ScanDirection);
                rootEle.Add(classEle);

                classEle = new XElement("CamNum", CamNum);
                rootEle.Add(classEle);

                classEle = new XElement("DefultX", DefultX);
                rootEle.Add(classEle);

                classEle = new XElement("DefultY", DefultY);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadius", MinRadius);
                rootEle.Add(classEle);

                classEle = new XElement("MaxRadius", MaxRadius);
                rootEle.Add(classEle);

                classEle = new XElement("GrayValue", GrayValue);
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
                classEle = new XElement("SpecValueCD", SpecValueCD);
                rootEle.Add(classEle);
                classEle = new XElement("SpecValueZD", SpecValueZD);
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

                classEle = new XElement("TiduDirection", TiduDirection);
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
                    ValueGain = float.Parse(Config.Descendants("ValueGain").ElementAt(0).Value);
                    ScanDirection = int.Parse(Config.Descendants("ScanDirection").ElementAt(0).Value);
                    CamNum = int.Parse(Config.Descendants("CamNum").ElementAt(0).Value);

                    DefultX = float.Parse(Config.Descendants("DefultX").ElementAt(0).Value);
                    DefultY = float.Parse(Config.Descendants("DefultY").ElementAt(0).Value);
                    MinRadius = float.Parse(Config.Descendants("MinRadius").ElementAt(0).Value);
                    MaxRadius = float.Parse(Config.Descendants("MaxRadius").ElementAt(0).Value);
                    GrayValue = float.Parse(Config.Descendants("GrayValue").ElementAt(0).Value);
                    ReferenceX = float.Parse(Config.Descendants("ReferenceX").ElementAt(0).Value);
                    ReferenceY = float.Parse(Config.Descendants("ReferenceY").ElementAt(0).Value);
                    ReferenceSpec = float.Parse(Config.Descendants("ReferenceSpec").ElementAt(0).Value);

                    MinRadiusA1 = float.Parse(Config.Descendants("MinRadiusA1").ElementAt(0).Value);
                    MaxRadiusA1 = float.Parse(Config.Descendants("MaxRadiusA1").ElementAt(0).Value);
                    GrayValueA1 = float.Parse(Config.Descendants("GrayValueA1").ElementAt(0).Value);
                    SpecValueCD = float.Parse(Config.Descendants("SpecValueCD").ElementAt(0).Value);
                    SpecValueZD = float.Parse(Config.Descendants("SpecValueZD").ElementAt(0).Value);

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

                    TiduDirection = int.Parse(Config.Descendants("TiduDirection").ElementAt(0).Value);
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
                if (value == 0)
                {
                    //软触发
                    HiKCamera.TriggerMode(0);
                }

                if (value == 1)
                {
                    //连续触发
                    HiKCamera.TriggerMode(1);
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

        /// <summary>
        /// 基准X
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
        /// 基准Y
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
        /// 基准规格
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
        /// 重叠折叠设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 重叠折叠设置-MinRadiusA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 重叠折叠设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 重叠折叠设置-MaxRadiusA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 重叠折叠设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 重叠折叠设置-GrayValueA1由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 重叠折叠设置
        /// </summary>
        public float SpecValueCD
        {
            get => GetProperty(() => SpecValueCD);
            set => SetProperty(() => SpecValueCD, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueCD").ElementAt(0).Value);
                    config.Descendants("SpecValueCD").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 重叠折叠设置-SpecValueCD由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 重叠折叠设置
        /// </summary>
        public float SpecValueZD
        {
            get => GetProperty(() => SpecValueZD);
            set => SetProperty(() => SpecValueZD, value, () =>
            {
                if (Paraload == 1)
                {
                    XDocument config = XDocument.Load(path + "/Config.xml");
                    float oldValue = float.Parse(config.Descendants("SpecValueZD").ElementAt(0).Value);
                    config.Descendants("SpecValueZD").ElementAt(0).SetValue(value);
                    config.Save(path + "/Config.xml");
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 重叠折叠设置-SpecValueZD由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域2设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域2设置-MinRadiusA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域2设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域2设置-MaxRadiusA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域2设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域2设置-GrayValueA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域2设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域2设置-CompareA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域2设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域2设置-SpecValueA2由" + oldValue + "变为" + value + "\n");
                }

            });
        }




        /// <summary>
        /// 检测区域3设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域3设置-MinRadiusA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域3设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域3设置-MaxRadiusA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域3设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域3设置-GrayValueA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域3设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域3设置-CompareA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        /// <summary>
        /// 检测区域3设置
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
                    File.AppendAllText("ParaHis" + "/.txt", DateTime.Now + " station" + stationNum + " 检测区域3设置-SpecValueA3由" + oldValue + "变为" + value + "\n");
                }

            });
        }

        #endregion

        #region 前台参数显示

        /// <summary>
        /// X偏移
        /// </summary>
        public float Xdeviation
        {
            get => GetProperty(() => Xdeviation);
            set => SetProperty(() => Xdeviation, value);
        }

        /// <summary>
        /// Y偏移
        /// </summary>
        public float Ydeviation
        {
            get => GetProperty(() => Ydeviation);
            set => SetProperty(() => Ydeviation, value);
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
        /// OK/NG结果颜色
        /// </summary>
        public string DetecColor
        {
            get => GetProperty(() => DetecColor);
            set => SetProperty(() => DetecColor, value);
        }

        /// <summary>
        ///  OK/NG结果
        /// </summary>
        public string DetecResult
        {
            get => GetProperty(() => DetecResult);
            set => SetProperty(() => DetecResult, value);
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
        /// 捕捉圆心Y
        /// </summary>
        public float ActualY
        {
            get => GetProperty(() => ActualY);
            set => SetProperty(() => ActualY, value);
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


        #endregion

        #region 画面显示
        /// <summary>
        /// 线程锁对象
        /// </summary>
        private static readonly object objlock = new object();

        /// <summary>
        /// 画面显示缓冲
        /// </summary>
        CVAlgorithms.BmpBuf bmpBuf = new CVAlgorithms.BmpBuf();

        /// <summary>
        /// 线程执行函数
        /// </summary>
        /// <param name="i"></param>
        /// <param name="e"></param>
        public bool isBusy = false;
        public int Comflag = 0;
        public float SMResultflag = 0;
        private void MV_STPAction(int i, HiKhelper.MV_IM_INFO e)
        {
            isBusy = true;
            bmpBuf.Width = e.width;
            bmpBuf.Height = e.height;
            bmpBuf.pData_IntPtr = e.pData;
            float[] outParam = new float[15];//参数初始化
            string[] inParam = new string[30];

            //从前台获取输入的参数
            inParam[0] = ShowMode.ToString();
            inParam[1] = ScanDirection.ToString();

            inParam[2] = DefultX.ToString();
            inParam[3] = DefultY.ToString();
            inParam[4] = MinRadius.ToString();
            inParam[5] = MaxRadius.ToString();
            inParam[6] = GrayValue.ToString();
            inParam[7] = ReferenceX.ToString();
            inParam[8] = ReferenceY.ToString();
            inParam[9] = ReferenceSpec.ToString();

            inParam[10] = MinRadiusA1.ToString();
            inParam[11] = MaxRadiusA1.ToString();           
            inParam[12] = GrayValueA1.ToString();
            inParam[13] = SpecValueCD.ToString();
            inParam[14] = SpecValueZD.ToString();

            inParam[15] = MinRadiusA2.ToString();
            inParam[16] = MaxRadiusA2.ToString();
            inParam[17] = GrayValueA2.ToString();
            inParam[18] = CompareA2.ToString();
            inParam[19] = SpecValueA2.ToString();

            inParam[20] = MinRadiusA3.ToString();
            inParam[21] = MaxRadiusA3.ToString();
            inParam[22] = GrayValueA3.ToString();
            inParam[23] = CompareA3.ToString();
            inParam[24] = SpecValueA3.ToString();

            inParam[25] = TiduDirection.ToString();

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
                SMResultflag = outParam[0];
                ActualX = outParam[1];
                ActualY = outParam[2];
                ReferenceActual = outParam[3];
                ValueActualA1 = outParam[4];
                ValueActualA2 = outParam[5];
                ValueActualA3 = outParam[6];

                if (SMResultflag == 1)
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
        /// 回调函数，回调帧为YUV格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hik_MV_OnOriFrameInvoked(object sender, HiKhelper.MV_IM_INFO e)//SM相机回调函数
        {
            if (!isBusy)
            {
                stp.QueueWorkItem(new Action<int, HiKhelper.MV_IM_INFO>(MV_STPAction), 2, e, WorkItemPriority.Normal);//插入线程值,1:选择算法，e:图像信息,线程优先值:正常值
            }       
        }

        /// <summary>
        /// 智能线程池，用于处理回调图像算法
        /// </summary>
        private SmartThreadPool stp;
        #endregion

        #region 结果处理和发送
        //结果处理和发送
        public string ResultCalculation(float SMResultflag, float Xdeviation, float Ydeviation)
        {
            if ((BuchangMode == 0) || (Math.Abs(Xdeviation) > BuchangSpec) || (Math.Abs(Ydeviation) > BuchangSpec))
            {
                Xdeviation = 0;
                Ydeviation = 0;
            }

            string resultCalculation = "";

            if (SMResultflag == 1)//SOMA OK
            {
                resultCalculation = "10,0000.000,0000.000,0000.000";
            }

            else
            {
                resultCalculation = "20,0000.000,0000.000,0000.000";
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
                if (!isBusy)
                {
                    Comflag = 0;
                    HiKCamera.TriggerOnce();

                    while (Comflag == 0)
                    {
                        Thread.Sleep(20);
                    }

                    resultCalculation = ResultCalculation(SMResultflag, Xdeviation, Ydeviation);

                    //发送网口数据
                    byte[] data = Encoding.UTF8.GetBytes(resultCalculation);
                    mySocket.SocketSend(data);
                }
            }
        }
        #endregion
    }
}
