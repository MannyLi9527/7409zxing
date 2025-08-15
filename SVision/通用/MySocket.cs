using DevExpress.Mvvm;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Vision.通用
{
    public class MySocket : ViewModelBase
    {
        #region 构造函数/析构函数
        public MySocket()
        {
            isShow = true;

            //配置文件读取
            Init();
        }

        public MySocket(int n)
        {
            isShow = true;
            CamID = n;

            //配置文件读取
            Init();
        }

        ~MySocket()
        {
            if (ListenThread != null) ListenThread.Abort();
        }
        #endregion

        #region 本地参数
        public string path = "./Para";
        /// <summary>
        /// 本机服务器
        /// </summary>
        private Socket Server = null;

        /// <summary>
        /// 缓冲区
        /// </summary>
        private byte[] buffer = new byte[128];

        /// <summary>
        /// 唯一客户端
        /// </summary>
        private Socket clientSocket = null;

        /// <summary>
        /// 监听线程
        /// </summary>
        Thread ListenThread;

        int CamID = -1;
        #endregion

        #region 绑定参数

        /// <summary>
        /// IP地址
        /// </summary>
        public string IP_Adress
        {
            get => GetProperty(() => IP_Adress);
            set => SetProperty(() => IP_Adress, value, () =>
            {
                string filePath = "./Para/NetConfig.xml";
                if (CamID >= 0)
                {
                    filePath = "./Para/Cam" + CamID + "/NetConfig.xml";
                }

                XDocument config = XDocument.Load(filePath);
                config.Descendants("IP_Adress").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int nPort
        {
            get => GetProperty(() => nPort);
            set => SetProperty(() => nPort, value, () =>
            {
                string filePath = "./Para/NetConfig.xml";
                if (CamID >= 0)
                {
                    filePath = "./Para/Cam" + CamID + "/NetConfig.xml";
                }

                XDocument config = XDocument.Load(filePath);
                config.Descendants("nPort").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 端口号 true表示使用网口
        /// </summary>
        public bool isShow
        {
            get => GetProperty(() => isShow);
            set => SetProperty(() => isShow, value);
        }

        #endregion

        #region 外调接口

        /// <summary>
        /// 停止监听
        /// </summary>
        public void StopListen()
        {
            try
            {
                if (clientSocket != null && clientSocket.Connected == true)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    clientSocket.Dispose();
                }
            }
            catch
            {
                Growl.Error("远程客户端连接无法关闭");
                return;
            }

            try
            {

                //Server.Shutdown(SocketShutdown.Both);
                Server.Close();
                Server.Dispose();
                if (ListenThread != null) ListenThread.Abort();
            }
            catch
            {
                Growl.Error("本地服务端无法关闭");
                return;
            }

            Growl.Info("服务器关闭成功");

        }

        /// <summary>
        /// 开启监听
        /// </summary>
        /// <param name="serverIP"></param>
        /// <returns></returns>
        public bool Listen()
        {
            try
            {
                //1.0 实例化套接字(IP4寻找协议,流式协议,TCP协议)
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //2.0 创建IP对象
                IPAddress address = IPAddress.Parse(IP_Adress);
                //3.0 创建网络端口,包括ip和端口
                IPEndPoint endPoint = new IPEndPoint(address, nPort);
                //4.0 绑定套接字
                Server.Bind(endPoint);
                //5.0 设置最大连接数
                Server.Listen(1);
                //6.0 开始监听
                ListenThread = new Thread(ListenClientConnect);
                ListenThread.Start();

                WriteLine("服务器开启");
                //Growl.Success("服务器开启");
            }
            catch (Exception e)
            {
                WriteLine("服务器开启失败");
                //Growl.Warning("服务器开启失败");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 发送一条消息
        /// </summary>
        /// <param name="buf">信息文本</param>
        public void SocketSend(byte[] buf)
        {
            try
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    clientSocket.Send(buf);

                    WriteLine("信息发送成功：" + Encoding.UTF8.GetString(buf, 0, buf.Length), "Green");
                }
                else
                {
                    WriteLine("信息发送失败：" + Encoding.UTF8.GetString(buf, 0, buf.Length), "Red");
                }
            }
            catch
            {
                WriteLine("信息发送失败：" + Encoding.UTF8.GetString(buf, 0, buf.Length), "Red");
            }
        }
        #endregion

        #region 内部函数

        /// <summary>
        /// 监听客户端连接
        /// </summary>
        private void ListenClientConnect()
        {
            try
            {
                while (true)
                {
                    //这里是利用异常退出线程
                    try
                    {
                        //Socket创建的新连接
                        clientSocket = Server.Accept();
                        Thread thread = new Thread(ReceiveMessage);
                        thread.Start(clientSocket);
                        Growl.Success("服务器接受到连接请求");
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }
            catch
            {
                Growl.Info("监听线程退出");
            }
        }

        /// <summary>
        /// 接收客户端消息
        /// </summary>
        /// <param name="socket">来自客户端的socket</param>
        private void ReceiveMessage(object socket)
        {
            Socket clientSocket = (Socket)socket;
            while (true)
            {
                try
                {
                    //获取从客户端发来的数据
                    int length = clientSocket.Receive(buffer);
                    //if (length == 0)

                    //string str = Encoding.UTF8.GetString(buffer, 0, length);

                    //Growl.Info(str);
                    MV_GetMess(Encoding.UTF8.GetString(buffer, 0, length));
                }
                catch
                {
                    //Console.WriteLine(ex.Message);
                    Growl.Error("服务器接受失败");

                    if (clientSocket != null && clientSocket.Connected == true)
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                    }
                    break;
                }
            }
        }

        #endregion

        #region 外部接口

        /// <summary>
        /// 外部事件集
        /// </summary>
        public event EventHandler<string> MV_onMess;

        /// <summary>
        /// 信息外发
        /// </summary>
        /// <param name="s"></param>
        private void MV_GetMess(string s)
        {
            MV_onMess?.Invoke(this, s);
        }

        #endregion

        #region 控制台打印

        /// <summary>
        /// 首标
        /// </summary>
        public string CmdTag => "[" + DateTime.Now.ToString("MM/dd") + " " + DateTime.Now.ToString("HH:mm:ss:ff") + "]";

        /// <summary>
        /// 界面显示
        /// </summary>
        public static TextBlock OriginalTextBlock = null;

        /// <summary>
        /// 打印一条信息到界面
        /// </summary>
        /// <param name="str"></param>
        public void WriteLine(string str, string color = "white")
        {
            //Application.Current.Dispatcher.Invoke(new Action(() =>
            //{
            //    try
            //    {
            //        Run run = new Run(CmdTag + str + "\r");

            //        switch (color)
            //        {
            //            case "Blue": run.Foreground = new SolidColorBrush(Colors.Blue); break;
            //            case "Green": run.Foreground = new SolidColorBrush(Colors.Green); break;
            //            case "Red": run.Foreground = new SolidColorBrush(Colors.Red); break;
            //            default:
            //                run.Foreground = new SolidColorBrush(Colors.White); break;
            //        }

            //        OriginalTextBlock.Inlines.Add(run);

            //        (OriginalTextBlock.Parent as System.Windows.Controls.ScrollViewer).ScrollToEnd();
            //        while (OriginalTextBlock.Inlines.Count > 10)
            //            OriginalTextBlock.Inlines.Remove(OriginalTextBlock.Inlines.ElementAt(1));
            //    }
            //    catch
            //    {
            //        Growl.Error("控制台输出失败");
            //    }
            //}));
        }

        #endregion

        #region 配置读写

        /// <summary>
        /// 新建配置文件
        /// </summary>
        private void InitErr()
        {
            //获得文件路径
            string localFilePath = "";
            localFilePath = "./Para/NetConfig.xml";
            if (CamID >= 0) localFilePath = "./Para/Cam" + CamID + "/NetConfig.xml";

            try
            {
                XDocument xdoc = new XDocument();
                XDeclaration xdec = new XDeclaration("1.0", "utf-8", "yes");
                xdoc.Declaration = xdec;

                XElement rootEle;
                XElement classEle;
                //XElement childEle;

                //添加根节点
                rootEle = new XElement("NetConfig");
                xdoc.Add(rootEle);

                classEle = new XElement("IP_Adress", IP_Adress);
                rootEle.Add(classEle);

                classEle = new XElement("nPort", nPort);
                rootEle.Add(classEle);

                xdoc.Save(localFilePath);
            }
            catch
            {
                Growl.Error("[" + localFilePath + "]生成失败！");
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            int ret = 0;
            string Path = "./Para";
            string filePath = "./Para/NetConfig.xml";
            if (CamID >= 0)
            {
                filePath = "./Para/Cam" + CamID + "/NetConfig.xml";
                Path = "./Para/Cam" + CamID;
            }

            try
            {
                if (XmlHelper.Exists(Path, "NetConfig.xml"))
                {
                    XDocument Config = XDocument.Load(filePath);

                    IP_Adress = Config.Descendants("IP_Adress").ElementAt(0).Value;
                    nPort = int.Parse(Config.Descendants("nPort").ElementAt(0).Value);
                }
                else
                {
                    Growl.Error("网口配置丢失！");
                    InitErr();
                    ret++;
                }
            }
            catch
            {
                Growl.Error("网口配置损坏！");
                InitErr();
            }


        }

        #endregion

    }
}
