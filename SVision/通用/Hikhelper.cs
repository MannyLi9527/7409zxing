using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using HandyControl.Controls;
using MvCamCtrl.NET;

namespace Vision
{
    

    /// <summary>
    /// 海康相机
    /// </summary>
    public class HiKhelper : DispatcherObject
    {
        #region 析构函数

        public HiKhelper()
        {
            isConnect = false;
            isTrigger = false;

            isOK = true;
            isNG = false;
        }

        ~HiKhelper()
        {
            ///软件退出前断开相机
            if (isConnect == true)
            {
                Disconnect();
            }
        }

        #endregion

        #region 私有参数

        /// <summary>
        /// 海康实例
        /// </summary>
        private MyCamera device = new MyCamera();

        /// <summary>
        /// 设备列表
        /// </summary>
        private static MyCamera.MV_CC_DEVICE_INFO_LIST stDevList;
        //相机列表
        public string[] MV_CameraList;

        /// <summary>
        /// 
        /// </summary>
        private byte[] buf;

        /// <summary>
        /// 
        /// </summary>
        private IntPtr pBuf;

        #endregion

        #region 搜索相机

        /// <summary>
        /// 相机通讯方式
        /// </summary>
        static uint nTLayerType = MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE;//表示查找GigE和USB3.0设备

        /// <summary>
        /// 相机信息，用于下拉框显示
        /// </summary>
        public struct CamInfo
        {
            public string Name { set; get; }
            public int ID { set; get; }
        }

        /// <summary>
        /// 相机信息列表
        /// </summary>
        /// 扩展集合，使集合在添加、删除、值变更后触发事件
        public static ObservableCollection<CamInfo> CamInfos = new ObservableCollection<CamInfo>();

        /// <summary>
        /// 搜索本地相机
        /// </summary>
       public void Search()
 
        {
            int nRet = MyCamera.MV_OK;
            string err = "";

            //ref参数，能够将一个变量带入一个方法中进行改变，改变完成后，再将改变后的值带出方法。
            nRet = MyCamera.MV_CC_EnumDevices_NET(nTLayerType, ref stDevList);//枚举子网内指定的传输协议对应的所有设备 //nTLayerType相机通讯方式, ref stDevList设备列表
            if (MyCamera.MV_OK != nRet)
            {
                //err = "相机枚举失败\n";
                err = String.Format("相机枚举失败,错误码:{0:X}\n", nRet);
                Growl.Error(err);
                //return nRet;
            }

            if (0 == stDevList.nDeviceNum)
            {
                err = String.Format("未找到相机\n");
                Growl.Error(err);
            }

            //以下这段新增加相机选择数组
            string[] Cameralist = new string[stDevList.nDeviceNum];
            for (int i = 0; i < stDevList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(stDevList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        Cameralist[i] = ("Camera" + i.ToString() + " :GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        Cameralist[i] = ("Camera" + i.ToString() + " :GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        Cameralist[i] = ("Camera" + i.ToString() + " :USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        Cameralist[i] = ("Camera" + i.ToString() + " :USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                    }
                }
            }
            MV_CameraList = Cameralist;


            // ch:在窗体列表中显示设备名
            string name = "";
            for (int i = 0; i < stDevList.nDeviceNum; i++)//stDevList设备列表
            {
                MyCamera.MV_CC_DEVICE_INFO stDevInfo;//设备信息结构体
                stDevInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(stDevList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (stDevInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(stDevInfo.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        name = ("Camera" + i.ToString() + " :GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")"));
                    }
                    else
                    {
                        name = ("Camera" + i.ToString() + " :GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")"));
                    }
                }
                else if (stDevInfo.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(stDevInfo.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        name = ("Camera" + i.ToString() + " :USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")"));
                    }
                    else
                    {
                        name = ("Camera" + i.ToString() + " :USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")"));
                    }
                }

                CamInfos.Add(new CamInfo() { Name = name, ID = i });//将对象添加到Collection<T>的结尾处              
            }
        }

        #endregion

        #region 相机状态

        /// <summary>
        /// 判断相机是否连接成功 true表示相机连接成功
        /// </summary>
        public bool isConnect{ set; get; } // 就是属性，见下面例程，类似于 public bool isConnect;

        /// <summary>
        /// 判断相机是否处于触发模式 true表示处于触发模式
        /// </summary>
        public bool isTrigger { set; get; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string err = "";

        public bool isOK { set; get; }

        public bool isNG { set; get; }

        #endregion

        #region 相机操作

        /// <summary>
        /// 海康相机连接 连接后默认进入外部触发模式(软触发)
        /// </summary>
        /// <returns>错误码</returns>
        public int Connect(int n)
        {
            if (isConnect)
            {
                Growl.Warning("重复连接相机");
                return -1;
            }

            if (stDevList.nDeviceNum <= n)
            {
                err = String.Format("相机数目异常，请检查后重新连接\n");
                Growl.Error(err);
                return -1;
            }

            int nRet = MyCamera.MV_OK;
            
            MyCamera.MV_CC_DEVICE_INFO stDevInfo;
            //设备信息结构体指针 转 设备信息结构体
            stDevInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(stDevList.pDeviceInfo[n], typeof(MyCamera.MV_CC_DEVICE_INFO));

            nRet = device.MV_CC_CreateDevice_NET(ref stDevInfo);
            if (MyCamera.MV_OK != nRet)
            {
                //err = "设备创建失败\n";
                err = String.Format("设备创建失败,错误码:{0:X}\n", nRet);
                Growl.Error(err);
                return nRet;
            }

            // Open device
            nRet = device.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                //err = "开启失败\n";
                err = String.Format("开启失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }

            // Register Exception Callback
            pCallBackFunc = new MyCamera.cbOutputExdelegate(CallbackRGB);
            nRet = device.MV_CC_RegisterImageCallBackEx_NET(pCallBackFunc, IntPtr.Zero);
            if (MyCamera.MV_OK != nRet)
            {
                //err = "回调设置失败\n";
                err = String.Format("回调设置失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }
            GC.KeepAlive(pCallBackFunc);

            nRet = device.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                //err = "采集开启失败\n";
                err = String.Format("采集开启失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }

            ////软触发
            nRet += device.MV_CC_SetEnumValue_NET("TriggerMode", (uint)1);
            nRet += device.MV_CC_SetEnumValue_NET("TriggerSource", 7);
            //nRet += device.MV_CC_SetBoolValue_NET("TriggerCacheEnable", true);
            //nRet += device.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", (float)2);
            nRet += device.MV_CC_SetIntValue_NET("GevHeartbeatTimeout", (uint)500);//心跳时间
            if (MyCamera.MV_OK != nRet)
            {
                //err = "触发模式开启失败\n";
                err = String.Format("触发模式开启失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }
            isConnect = true;
            isTrigger = true;
            return nRet;
        }

        /// <summary>
        /// 海康相机断开
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            int nRet = MyCamera.MV_OK;

            nRet = device.MV_CC_StopGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                err = String.Format("设备停止采集失败,错误码:{0:X}\n", nRet);
                return nRet;
            }

            nRet = device.MV_CC_CloseDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                err = String.Format("设备关闭失败,错误码:{0:X}\n", nRet);
                return nRet;
            }

            nRet = device.MV_CC_DestroyDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                err = String.Format("设备销毁资源失败,错误码:{0:X}\n", nRet);
                return nRet;
            }
            isConnect = false;
            isTrigger = false;
            return nRet;
        }


        /// <summary>
        /// 相机触发模式选择
        /// </summary>
        public void TriggerMode(int TriggerModeflag)
        {
            if(TriggerModeflag==0)
            {
                try
                {                                             
                    device.MV_CC_SetEnumValue_NET("TriggerMode", (uint)1);//软触发
                    device.MV_CC_SetEnumValue_NET("TriggerSource", 7);// 触发源:0-Line0;1-Line1;2-Line2;3-Line3;4-Counter;7-Software;
                }
                catch
                {

                }
            }

            if (TriggerModeflag == 1)
            {
                try
                {                   
                    device.MV_CC_SetEnumValue_NET("TriggerMode", (uint)0);//连续采集
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// 触发一次拍照
        /// </summary>
        public int TriggerOnce()
        {
            return device.MV_CC_SetCommandValue_NET("TriggerSoftware");
        }

        /// <summary>
        /// 相机曝光设置
        /// </summary>
        public void SetExposureTime(float ValueExposureTime)
        {    
            if(ValueExposureTime<100)
            {
                ValueExposureTime = 100;
            }

            if (isConnect)
            {
                int nRet = 0;
                nRet = device.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                nRet = device.MV_CC_SetFloatValue_NET("ExposureTime", ValueExposureTime);

                if (MyCamera.MV_OK != nRet)
                {
                    err = String.Format("曝光时间设置失败,错误码:{0:X}\n", nRet);
                    Growl.Error(err);
                }
            }
        }

        /// <summary>
        /// 相机增益设置
        /// </summary>
        public void SetGainValue(float ValueGain)
        {
            if (isConnect)
            {
                int nRet = 0;
                nRet = device.MV_CC_SetEnumValue_NET("GainAuto", 0);
                nRet = device.MV_CC_SetFloatValue_NET("Gain", ValueGain);

                if (MyCamera.MV_OK != nRet)
                {
                    err = String.Format("相机增益设置失败,错误码:{0:X}\n", nRet);
                    Growl.Error(err);
                }
            }
        }

        /// <summary>
        /// 相机曝光读取
        /// </summary>
        public float GetExposureTime()
        {
            if (isConnect)
            {
                int nRet = 0;
                MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
                nRet = device.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);

                if (MyCamera.MV_OK != nRet)
                {
                    err = String.Format("曝光时间读取失败,错误码:{0:X}\n", nRet);
                    Growl.Error(err);
                }
                return stParam.fCurValue;
            }
            else
            {
                return 0;
            }
        }


        #endregion

        #region 外部设置

        /// <summary>
        /// 路由事件，回调原始帧
        /// </summary>
        public event EventHandler<MV_IM_INFO> MV_OnOriFrameInvoked;

        #endregion

        #region 回调函数

        /// <summary>
        /// 回调函数
        /// </summary>
        MyCamera.cbOutputExdelegate pCallBackFunc;

        /// <summary>
        /// 图像结构体
        /// </summary>
        public struct MV_IM_INFO
        {
            public IntPtr pData;
            public int width;
            public int height;
            public int pUser;
            public int CameraNum;
            public int nFrameLen;

        }

        /// <summary>
        /// 外部委托
        /// </summary>
        /// <param name="imInfo"></param>
        private void MV_Show(MV_IM_INFO imInfo)//imInfo对应画面显示里的e
        {
            MV_OnOriFrameInvoked?.Invoke(this, imInfo);
        }

        /// <summary>
        /// 外部委托
        /// </summary>
        /// <param name="imInfo"></param>
        private delegate void MV_MShow(MV_IM_INFO imInfo);

        /// <summary>
        /// 单相机回调函数
        /// </summary>
        /// <param name="pData"></param>
        /// <param name="pFrameInfo"></param>
        /// <param name="pUser"></param>
        private void CallbackRGB(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            if ((int)pUser == 0)
            {
                lock (this)
                {
                    // 拷贝中间内存区域、、从相机拷贝到电脑
                    if (buf == null)
                    {
                        buf = new byte[pFrameInfo.nFrameLen];
                        pBuf = Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0);
                    }
                    Marshal.Copy(pData, buf, 0, (int)pFrameInfo.nFrameLen);
                }


                Dispatcher.BeginInvoke(new MV_MShow(MV_Show),
                              new MV_IM_INFO
                              {
                                  pData = pBuf,
                                  width = (int)pFrameInfo.nWidth,
                                  height = (int)pFrameInfo.nHeight,
                                  pUser = (int)0,
                                  nFrameLen = (int)pFrameInfo.nFrameLen
                              });
            }
        }


        #endregion

    }
}
