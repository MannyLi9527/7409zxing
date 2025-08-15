using Amib.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using HandyControl.Controls;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Vision.控件;
using Vision.通用;

namespace Vision
{
    public class LoginViewModel : ViewModelBase
    {
        #region 构造函数
        public LoginViewModel()
        {
            init();
        }
        #endregion

        #region 参数绑定

        /// <summary>
        /// 密码输入值
        /// </summary>
        public string pwshuru
        {
            get => GetProperty(() => pwshuru);
            set => SetProperty(() => pwshuru, value);
        }

        /// <summary>
        /// 密码修改值
        /// </summary>
        public string pwxiugai
        {
            get => GetProperty(() => pwxiugai);
            set => SetProperty(() => pwxiugai, value);
        }


        /// <summary>
        /// 密码保存
        /// </summary>
        public string pwsave
        {
            get => GetProperty(() => pwsave);
            set => SetProperty(() => pwsave, value, () =>
            {
                XDocument config = XDocument.Load(path + "/Config.xml");
                config.Descendants("pwsave").ElementAt(0).SetValue(value);
                config.Save(path + "/Config.xml");
            });
        }


        /// <summary>
        /// 密码修改确认按钮
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void pwxiugaiCommand(object obj)
        {
            if ((pwshuru == pwsave) && (pwxiugai != null))
            {
                pwsave = pwxiugai;
            }

            else
            {
                System.Windows.MessageBox.Show("请先输入旧密码或修改密码不能空！");
            }
        }

        #endregion

        #region 初始化相关
        public string path = "./Para";
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

                classEle = new XElement("pwsave", pwsave);
                rootEle.Add(classEle);

                xdoc.Save(localFilePath);
            }
            catch
            {
                Growl.Error("默认配置生成失败！");
            }
        }


        /// <summary>
        /// 读取参数文件
        /// </summary>
        public void init()
        {
            int ret = 0;
            string filePath = path + "/Config.xml";
            if (XmlHelper.Exists(path, "Config.xml"))
            {
                try
                {
                    XDocument Config = XDocument.Load(path + "/Config.xml");

                    pwsave = Config.Descendants("pwsave").ElementAt(0).Value;
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
        }
        #endregion

    }
}
