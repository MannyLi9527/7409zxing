// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"
#include "metrogolyCircle.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

struct BmpBuf
{
	unsigned char* data_Output;
	int size;
	unsigned char* data_Input;
	int h;
	int w;
};

#include <opencv.hpp>
#include <opencv2/imgproc/types_c.h>
#include "putText.h"
#include <ReadBarcode.h>
#include <BarcodeFormat.h>
#include <ReaderOptions.h>
#include <TextUtfEncoding.h>  // 确保路径正确
#include<ctime> //系统时间获取头文件
using namespace cv;
using namespace std;


string DecodeDataMatrixFromMat(const Mat& image) 
{
	// 1. 检查输入图像是否有效
	if (image.empty()) {
		return "";
	}

	// 2. 将 OpenCV Mat 转换为 ZXing 兼容的格式（灰度图像）
	Mat grayMat;
	if (image.channels() == 3) {
		cvtColor(image, grayMat, cv::COLOR_BGR2GRAY);
	}
	else if (image.channels() == 1) {
		grayMat = image;
	}
	else {
		return ""; // 不支持的通道数
	}

	// 3. 创建 ZXing 的 ImageView 对象
	ZXing::ImageView imageView{
		grayMat.data,            // 图像数据指针
		grayMat.cols,            // 宽度
		grayMat.rows,            // 高度
		ZXing::ImageFormat::Lum  // 灰度格式
	};

	// 4. 配置解码选项（仅识别 DataMatrix）
	ZXing::ReaderOptions hints;
	hints.setFormats({ ZXing::BarcodeFormat::DataMatrix });
	hints.setTryHarder(true);

	// 5. 调用 ZXing 解码
	auto result = ZXing::ReadBarcode(imageView, hints);

	// 6. 返回解码结果
	if (result.isValid()) {
		return result.text();
	}

	return ""; // 解码失败
}


//获取圆周像素算法
float CircleTraverse(Mat mat1, Mat mat2,float CenterX, float CenterY, float MinRadius, float MaxRadius, float GrayValue)
{
	float light=0, lightnum=0;

	circle(mat1, Point2f(CenterX, CenterY), MaxRadius, Scalar(0, 255, 0), 1, 1);//外界限，线宽=1
	circle(mat1, Point2f(CenterX, CenterY), MinRadius, Scalar(0, 255, 0), 1, 1);//内界限，线宽=1

	for (int ang = 0; ang < 360; ang++)
	{
		float sum = 0;

		if ((MaxRadius > MinRadius) && ((CenterX - MaxRadius) > 0) && ((CenterX + MaxRadius) < mat2.cols) && ((CenterY - MaxRadius) > 0 ) && ((CenterY + MaxRadius) < mat2.rows))
		{
			for (float r = MinRadius; r < MaxRadius; r++)//圆周坐标
			{
				float x = CenterX + r * cos(ang*CV_PI / 180);
				float y = CenterY + r * sin(ang*CV_PI / 180);
				sum += mat2.at<BYTE>(y, x);//(y, x)坐标的亮度值
			}

		    light = sum / (MaxRadius - MinRadius);
		}
		else
		{
			light = 0;
		}
 
		if (light >= GrayValue)
		{
			float x1 = CenterX + MinRadius * cos(ang*CV_PI / 180);
			float y1 = CenterY + MinRadius * sin(ang*CV_PI / 180);
			float x2 = CenterX + MaxRadius * cos(ang*CV_PI / 180);
			float y2 = CenterY + MaxRadius * sin(ang*CV_PI / 180);
			Point2f light1 = Point2f(x1, y1);
			Point2f light2 = Point2f(x2, y2);
			line(mat1, light1, light2, Scalar(0, 255, 255), 1); //线宽 = 1
			lightnum = lightnum + 1;
		}
	}
	return lightnum;
}

//算法异常退出
void ErrOutput(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	Mat src = Mat(data.h, data.w, CV_8UC1, data.data_Input);
	Mat	output;
	stringstream str;
	cvtColor(src, output, COLOR_GRAY2RGB);
	str << "算法异常退出！";

#pragma region 文字输入
	//字体大小
	int text_Size = (int)((data.w* data.h / 10000 - 30) * 0.078 + 25) * 2;
	//位置
	Point text_Localtion01;
	text_Localtion01.x = text_Size / 3;
	text_Localtion01.y = text_Size / 3;
	Point text_Localtion02;
	text_Localtion02.x = text_Size / 3;
	text_Localtion02.y = data.h - text_Size * 4;
	Point text_Localtion03;
	text_Localtion03.x = text_Size / 3;
	text_Localtion03.y = data.h - text_Size * 3;
	//颜色
	Scalar fontColor = Scalar(0, 255, 255);
	//输出
	std::string text = str.str();
	putTextZH(output, text.c_str(), text_Localtion01, fontColor, text_Size, "黑体", 0);
#pragma endregion

#pragma region 结果返回
	output_Parameter_Float[0] = false;
#pragma endregion

#pragma region 图片返回
	int size = output.total() * output.elemSize();//计算图片总像素大小
	data.size = size;
	data.h = output.rows;
	data.w = output.cols;
	data.data_Output = (uchar *)calloc(size, sizeof(uchar));//申请空间，固定格式
	std::memcpy(data.data_Output, output.data, size * sizeof(BYTE));//内存拷贝，将opencv的图像复制到输出内存output去
#pragma endregion
}


//镜筒图像处理算法
bool locationJT(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	try
	{
#pragma region 参数转换
		int IsShow = atoi(input_Parameter[0]);
		int ScanDirection = atoi(input_Parameter[1]);

		float DefaultX = atof(input_Parameter[2]);
		float DefaultY = atof(input_Parameter[3]);
		float MinRadius = atof(input_Parameter[4]);
		float MaxRadius = atof(input_Parameter[5]);
		float GrayValue = atof(input_Parameter[6]);

		float MinRadiusAng = atof(input_Parameter[8]);
		float MaxRadiusAng = atof(input_Parameter[9]);
		float GrayValueAng = atoi(input_Parameter[10]);
		float MarkMode = atoi(input_Parameter[11]);
		float SpecAreaAng = atoi(input_Parameter[12]);

		float ReferenceX = atof(input_Parameter[13]);
		float ReferenceY = atof(input_Parameter[14]);
		float ReferenceSpec = atoi(input_Parameter[15]);

		float MinRadiusA1 = atof(input_Parameter[16]);
		float MaxRadiusA1 = atof(input_Parameter[17]);
		float GrayValueA1 = atoi(input_Parameter[18]);
		float CompareA1 = atoi(input_Parameter[19]);
		float SpecValueA1 = atoi(input_Parameter[20]);

		float MinRadiusA2 = atof(input_Parameter[21]);
		float MaxRadiusA2 = atof(input_Parameter[22]);
		float GrayValueA2 = atoi(input_Parameter[23]);
		float CompareA2 = atoi(input_Parameter[24]);
		float SpecValueA2 = atoi(input_Parameter[25]);

		float MinRadiusA3 = atof(input_Parameter[26]);
		float MaxRadiusA3 = atof(input_Parameter[27]);
		float GrayValueA3 = atoi(input_Parameter[28]);
		float CompareA3 = atoi(input_Parameter[29]);
		float SpecValueA3 = atoi(input_Parameter[30]);

		float MinRadiusA4 = atof(input_Parameter[31]);
		float MaxRadiusA4 = atof(input_Parameter[32]);
		float GrayValueA4 = atoi(input_Parameter[33]);
		float CompareA4 = atoi(input_Parameter[34]);
		float SpecValueA4 = atoi(input_Parameter[35]);

		float SpecAreaAngSX = atoi(input_Parameter[36]);
		float MarkErroMode = atoi(input_Parameter[37]);

		int TiduDirection = atoi(input_Parameter[38]);

		float ActualPianyiJT = 0;
		float ActualLouzu = 0;
		float ActualDGP = 0;
		float ActualZuzhuang = 0;
		float ActualZuzhuang2 = 0;

#pragma endregion

#pragma region 本地参数		
		Mat dst;
		Mat _temp;
		Mat src;
		Mat _src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//相机
		flip(_src, src, 0);//flip(inputArry,outputArry,type):0垂直翻转，1水平翻转，-1水平和垂直翻转
		Mat mask = Mat::zeros(src.size(), src.type());
		Mat temp = Mat::zeros(src.size(), src.type());
		Mat srcblur;

		float ActualPianyiJTflag = true;
		float ActualLouzuflag = true;
		float ActualDGPflag = true;
		float ActualZuzhuangflag = true;
		float ActualZuzhuang2flag = true;

		float JTResultflag = false;
		float ActualX = 0;
		float ActualY = 0;
		Point2f center = Point2f(DefaultX, DefaultY);//捕捉圆预输入圆心
		stringstream str;
		
		int index = 0;//寻找角度轮廓编号
		float maxAreaAng = 0;//寻找角度最大轮廓面积		
		float a_out = 0;//角度
		vector<vector<Point2i>> BestMatch;//寻找角度轮廓集合
		vector<vector<Point2i>> contours;
		vector<Vec4i> hierarchy;
#pragma endregion

#pragma region 图像预处理
		
		blur(src, srcblur, Size(3, 3));
		threshold(srcblur, temp, GrayValue, 255, THRESH_BINARY);
		
#pragma endregion

#pragma region 输出图片选择
		if (IsShow == 1)
		{
			cvtColor(temp, dst, COLOR_GRAY2RGB);
		}
		else
		{
			cvtColor(src, dst, COLOR_GRAY2RGB);
		}
		circle(dst, center, MaxRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆外界限,线宽=2
		circle(dst, center, MinRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆内界限,线宽=2
#pragma endregion

#pragma region 卡尺圆定位
		cv::Vec3i vecTmp = { (int)center.x, (int)center.y, (int)(MaxRadius + MinRadius) / 2 };//输入预设的圆心、半径
		int nV = (MaxRadius - MinRadius) / 2, nH = 5, nThreshold = GrayValue;//输入预设的扫描范围
		std::vector<cv::Point> stOutPt;//定义一个容器用来装扫描到的点


		//由内到外
		if (ScanDirection == 0)
		{
			if (TiduDirection == 0)
			{
				temp = 255 - temp;
			}
		}
		else
		{
			if (TiduDirection == 1)//
			{
				temp = 255 - temp;
			}
		}

		gen_Metrology_Model_circle(temp, vecTmp, nV, nH, nThreshold, stOutPt, ScanDirection);//调用函数查找圆上的点
		for (size_t i = 0; i < stOutPt.size(); i++)
		{
			line(dst, stOutPt[i], stOutPt[i], Scalar(255, 255, 0), 2);//查找到的点画圆1，宽度=2
		}

		std::vector < cv::Point> inPoints; inPoints.clear();//新建一个容器装拟合后的点
		circleInfo fitCircle;//定义一个拟合出来的圆的参数

		if (3 > stOutPt.size())
		{
			str << "有效点数不足，定位失败！" << endl;
			goto OUTPUT;
		}
		
		ransc_fit_circle(src, stOutPt, 816, 1.5, 60, inPoints, fitCircle);//对查找到的圆上的点进行拟合
		for (size_t i = 0; i < inPoints.size(); i++)
		{
			line(dst, inPoints[i], inPoints[i], Scalar(0, 255, 255), 2);//拟合后的点画圆2，宽度=2
		}

		if (3 > inPoints.size())
		{
			str << "拟合分数过低！定位失败！" << endl;
			goto OUTPUT;
		}
		ActualX = fitCircle.A;//捕捉到的圆心坐标X
		ActualY = fitCircle.B;//捕捉到的圆心坐标Y

		circle(dst, cv::Point2f(fitCircle.A, fitCircle.B), fitCircle.C, Scalar(255, 0, 0), 2);//捕捉到的圆，宽度=2
		str << "圆心:(" << ActualX << "," << ActualY << ")" << endl;
		output_Parameter_Float[1] = ActualX;
		output_Parameter_Float[2] = ActualY;

//角度识别
#pragma region ROI获取  
		mask *= 0;
		circle(mask, Point(ActualX, ActualY), MaxRadiusAng, Scalar(255), -1);
		circle(mask, Point(ActualX, ActualY), MinRadiusAng, Scalar(000), -1);

		if (MarkMode == 0)
		{
			threshold(src, temp, GrayValueAng, 255, THRESH_BINARY_INV);//反二值化大于阀值设0小于阀值设255			
		}
		else
		{
			threshold(src, temp, GrayValueAng, 255, THRESH_BINARY);//二值化大于阀值设255小于阀值设0
		}
			
		temp.copyTo(_temp, mask);

		circle(dst, Point2f(ActualX, ActualY), MaxRadiusAng, Scalar(0, 255, 0), 1, 1);//角度外界限，宽度=1
		circle(dst, Point2f(ActualX, ActualY), MinRadiusAng, Scalar(0, 255, 0), 1, 1);//角度内界限，宽度=1
#pragma endregion

#pragma region 轮廓筛选

		findContours(_temp, contours, hierarchy, CV_RETR_TREE, CV_CHAIN_APPROX_NONE, Point(0, 0));

		for (size_t i = 0; i < contours.size(); i++)//整张图里面遍历找到最大轮廓
		{
			double area = contourArea(contours[i], false);
			if ((area > SpecAreaAng) && (area < SpecAreaAngSX))
			{
				index++;
				BestMatch.push_back(contours[i]);

				if (area > maxAreaAng)
				{
					maxAreaAng = area;
				}
			}
		}

		drawContours(dst, BestMatch, -1, Scalar(0, 255, 255), 2);//找到的角度轮廓，宽度=2

		if (index > 0)
		{
			if (index == 1)
			{
				RotatedRect rect = minAreaRect(BestMatch[0]);

				line(dst, Point2f(ActualX, ActualY), rect.center, Scalar(0, 255, 255), 2);//画线指向缺口中心:rect.center，宽度=2

				//角度计算
				Point2f p2 = rect.center - Point2f(ActualX, ActualY);
				float c = -p2.y / (sqrt(p2.x*p2.x + p2.y*p2.y));
				a_out = acos(c) * 180 / CV_PI;
				if (ActualX > rect.center.x)
				{
					a_out = 360 - a_out;
				}				
			}

			else
			{
				RotatedRect rect0 = minAreaRect(BestMatch[0]);
				RotatedRect rect1 = minAreaRect(BestMatch[1]);
				Point2f AngPoint = (rect0.center + rect1.center) / 2;

				line(dst, Point2f(ActualX, ActualY), AngPoint, Scalar(0, 255, 255), 2);//画线指向缺口中心,宽度=2

				//角度计算
				Point2f p2 = AngPoint - Point2f(ActualX, ActualY);
				float c = -p2.y / (sqrt(p2.x*p2.x + p2.y*p2.y));
				a_out = acos(c) * 180 / CV_PI;
				if (ActualX > AngPoint.x)
				{
					a_out = 360 - a_out;
				}
			}

		}
					
		else
		{
			a_out = 0;

			if (MarkErroMode == 0)
			{
				str << "未找到缺口";
				goto OUTPUT;
			}
		}

		output_Parameter_Float[3] = maxAreaAng;
		output_Parameter_Float[4] = index;
		output_Parameter_Float[5] = a_out;

#pragma endregion

#pragma region 检测区域

		ActualPianyiJT = sqrt((ActualX - ReferenceX)*(ActualX - ReferenceX) + (ActualY - ReferenceY)*(ActualY - ReferenceY));
		output_Parameter_Float[6] = ActualPianyiJT;
		if (ActualPianyiJT > ReferenceSpec)
		{
			str << "偏移量NG" << endl;
			ActualPianyiJTflag = false;
		}


		ActualLouzu = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA1, MaxRadiusA1, GrayValueA1);
		output_Parameter_Float[7] = ActualLouzu;
		if (CompareA1 == 0)
		{
			if (ActualLouzu < SpecValueA1)
			{
				str << "漏组检测NG" << endl;
				ActualLouzuflag = false;
			}
		}
		else
		{
			if (ActualLouzu > SpecValueA1)
			{
				str << "漏组检测NG" << endl;
				ActualLouzuflag = false;
			}
		}


		ActualDGP = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA2, MaxRadiusA2, GrayValueA2);
		output_Parameter_Float[8] = ActualDGP;
		if (CompareA2 == 0)
		{
			if (ActualDGP < SpecValueA2)
			{
				str << "挡光片检测NG" << endl;
				ActualDGPflag = false;
			}
		}
		else
		{
			if (ActualDGP > SpecValueA2)
			{
				str << "挡光片检测NG" << endl;
				ActualDGPflag = false;
			}
		}
		

		ActualZuzhuang = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA3, MaxRadiusA3, GrayValueA3);
		output_Parameter_Float[9] = ActualZuzhuang;
		if (CompareA3 == 0)
		{
			if (ActualZuzhuang < SpecValueA3)
			{
				str << "组装检测NG" << endl;
				ActualZuzhuangflag = false;
			}
		}
		else
		{
			if (ActualZuzhuang > SpecValueA3)
			{
				str << "组装检测NG" << endl;
				ActualZuzhuangflag = false;
			}
		}
		

		ActualZuzhuang2 = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA4, MaxRadiusA4, GrayValueA4);
		output_Parameter_Float[10] = ActualZuzhuang2;
		if (CompareA4 == 0)
		{
			if (ActualZuzhuang2 < SpecValueA4)
			{
				str << "组装检测2 NG" << endl;
				ActualZuzhuang2flag = false;
			}
		}
		else
		{
			if (ActualZuzhuang2 > SpecValueA4)
			{
				str << "组装检测2 NG" << endl;
				ActualZuzhuang2flag = false;
			}
		}
		
		
#pragma endregion

#pragma region 结果返回

		if ( ActualPianyiJTflag && ActualLouzuflag && ActualDGPflag && ActualZuzhuangflag && ActualZuzhuang2flag)
		{
			JTResultflag = true;
			output_Parameter_Float[0] = JTResultflag;
		}
		else
		{
			JTResultflag = false;
			output_Parameter_Float[0] = JTResultflag;
		}

#pragma endregion


OUTPUT:

#pragma region 文字输入
		//字体大小
		int text_Size = (int)((data.w* data.h / 10000 - 30) * 0.078 + 50); 
		//位置
		Point text_Localtion01;
		text_Localtion01.x = text_Size / 3;
		text_Localtion01.y = text_Size / 3;
		Point text_Localtion02;
		text_Localtion02.x = text_Size / 3;
		text_Localtion02.y = data.h - text_Size * 4;
		Point text_Localtion03;
		text_Localtion03.x = text_Size / 3;
		text_Localtion03.y = data.h - text_Size * 3;

		if (!JTResultflag)
		{
			putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(255, 0, 0), text_Size, "黑体", 0);//RGB
			output_Parameter_Float[0] = false;
		}
		else
		{
			putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(0, 255, 0), text_Size, "黑体", 0);
		}
#pragma endregion

#pragma region 图片返回
		Mat output;   
		cvtColor(dst, output, CV_BGR2RGB);
		int size = output.total() * output.elemSize();
		data.size = size;
		data.h = output.rows;
		data.w = output.cols;
		data.data_Output = (uchar *)calloc(size, sizeof(uchar));
		std::memcpy(data.data_Output, output.data, size * sizeof(BYTE));
#pragma endregion

	}

	catch (const std::exception&)//exception&可能存在错误代码，C++里面的固定语法
	{
		output_Parameter_Float[0] = false;
		ErrOutput(data, input_Parameter, output_Parameter_Float); 
		return 1;
	}
}


//镜片图像处理算法
bool locationJP(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	try
	{
#pragma region 参数转换
		int IsShow = atoi(input_Parameter[0]);
		int ScanDirection2 = atoi(input_Parameter[1]);

		float DefaultX2 = atof(input_Parameter[2]);
		float DefaultY2 = atof(input_Parameter[3]);
		float MinRadius2 = atof(input_Parameter[4]);
		float MaxRadius2 = atof(input_Parameter[5]);
		float GrayValue2 = atof(input_Parameter[6]);

		float MinRadiusAng2 = atof(input_Parameter[8]);
		float MaxRadiusAng2 = atof(input_Parameter[9]);
		float GrayValueAng2 = atoi(input_Parameter[10]);
		float MarkMode2 = atoi(input_Parameter[11]);
		float SpecAreaAng2 = atoi(input_Parameter[12]);

		float ReferenceX2 = atof(input_Parameter[13]);
		float ReferenceY2 = atof(input_Parameter[14]);
		float ReferenceSpec2 = atoi(input_Parameter[15]);

		float MinRadiusB1 = atof(input_Parameter[16]);
		float MaxRadiusB1 = atof(input_Parameter[17]);
		float GrayValueB1 = atoi(input_Parameter[18]);
		float CompareB1 = atoi(input_Parameter[19]);
		float SpecValueB1 = atoi(input_Parameter[20]);

		float MinRadiusB2 = atof(input_Parameter[21]);
		float MaxRadiusB2 = atof(input_Parameter[22]);
		float GrayValueB2 = atoi(input_Parameter[23]);
		float CompareB2 = atoi(input_Parameter[24]);
		float SpecValueB2 = atoi(input_Parameter[25]);

		float MinRadiusB3 = atof(input_Parameter[26]);
		float MaxRadiusB3 = atof(input_Parameter[27]);
		float GrayValueB3 = atoi(input_Parameter[28]);
		float CompareB3 = atoi(input_Parameter[29]);
		float SpecValueB3 = atoi(input_Parameter[30]);

		float SpecAreaAngSX2 = atoi(input_Parameter[31]);
		float MarkErroMode2 = atoi(input_Parameter[32]);

		int TiduDirection2 = atoi(input_Parameter[33]);

		float ActualPianyiJP = 0;
		float ActualZhengfan = 0;
		float ActualTuhei = 0;
		float ActualBupin = 0;

#pragma endregion

#pragma region 本地参数		
		Mat dst;
		Mat _temp;
		Mat src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//相机
		Mat mask = Mat::zeros(src.size(), src.type());
		Mat temp = Mat::zeros(src.size(), src.type());
		Mat srcblur;

		float ActualPianyiJPflag = true;
		float ActualZhengfanflag = true;
		float ActualTuheiflag = true;
		float ActualBupinflag = true;

		float JPResultflag = false;
		float ActualX = 0;
		float ActualY = 0;
		Point2f center = Point2f(DefaultX2, DefaultY2);//捕捉圆预输入圆心
		stringstream str;

		int index = 0;//寻找角度轮廓编号
		float maxAreaAng = 0;//寻找角度最大轮廓面积		
		float a_out = 0;//角度
		vector<vector<Point2i>> BestMatch;//寻找角度轮廓集合
		vector<vector<Point2i>> contours;
		vector<Vec4i> hierarchy;
#pragma endregion

#pragma region 图像预处理
		
		blur(src, srcblur, Size(3, 3));
		threshold(srcblur, temp, GrayValue2, 255, THRESH_BINARY);
		
#pragma endregion

#pragma region 输出图片选择
		if (IsShow == 1)
		{
			cvtColor(temp, dst, COLOR_GRAY2RGB);
		}
		else
		{
			cvtColor(src, dst, COLOR_GRAY2RGB);
		}
		circle(dst, center, MaxRadius2, Scalar(0, 0, 255), 2, 1);//捕捉圆外界限,线宽=2
		circle(dst, center, MinRadius2, Scalar(0, 0, 255), 2, 1);//捕捉圆内界限,线宽=2
#pragma endregion

#pragma region 卡尺圆定位
		cv::Vec3i vecTmp = { (int)center.x, (int)center.y, (int)(MaxRadius2 + MinRadius2) / 2 };//输入预设的圆心、半径
		int nV = (MaxRadius2 - MinRadius2) / 2, nH = 5, nThreshold = GrayValue2;//输入预设的扫描范围
		std::vector<cv::Point> stOutPt;//定义一个容器用来装扫描到的点

		//由内到外
		if (ScanDirection2 == 0)
		{
			if (TiduDirection2 == 0)
			{
				temp = 255 - temp;
			}
		}
		else
		{
			if (TiduDirection2 == 1)//
			{
				temp = 255 - temp;
			}
		}

		gen_Metrology_Model_circle(temp, vecTmp, nV, nH, nThreshold, stOutPt, ScanDirection2);//调用函数查找圆上的点
		for (size_t i = 0; i < stOutPt.size(); i++)
		{
			line(dst, stOutPt[i], stOutPt[i], Scalar(255, 255, 0), 2);//查找到的点画圆1，宽度=2
		}

		std::vector < cv::Point> inPoints; inPoints.clear();//新建一个容器装拟合后的点
		circleInfo fitCircle;//定义一个拟合出来的圆的参数

		if (3 > stOutPt.size())
		{
			str << "有效点数不足，定位失败！" << endl;
			goto OUTPUT;
		}

		ransc_fit_circle(src, stOutPt, 816, 1.5, 60, inPoints, fitCircle);//对查找到的圆上的点进行拟合
		for (size_t i = 0; i < inPoints.size(); i++)
		{
			line(dst, inPoints[i], inPoints[i], Scalar(0, 255, 255), 2);//拟合后的点画圆2，宽度=2
		}

		if (3 > inPoints.size())
		{
			str << "拟合分数过低！定位失败！" << endl;
			goto OUTPUT;
		}
		ActualX = fitCircle.A;//捕捉到的圆心坐标X
		ActualY = fitCircle.B;//捕捉到的圆心坐标Y

		circle(dst, cv::Point2f(fitCircle.A, fitCircle.B), fitCircle.C, Scalar(255, 0, 0), 2);//捕捉到的圆，宽度=2
		str << "圆心:(" << ActualX << "," << ActualY << ")" << endl;
		output_Parameter_Float[1] = ActualX;
		output_Parameter_Float[2] = ActualY;

		//角度识别
#pragma region ROI获取  
		mask *= 0;
		circle(mask, Point(ActualX, ActualY), MaxRadiusAng2, Scalar(255), -1);
		circle(mask, Point(ActualX, ActualY), MinRadiusAng2, Scalar(000), -1);

		if (MarkMode2 == 0)
		{
			threshold(src, temp, GrayValueAng2, 255, THRESH_BINARY_INV);//反二值化大于阀值设0小于阀值设255			
		}
		else
		{
			threshold(src, temp, GrayValueAng2, 255, THRESH_BINARY);//二值化大于阀值设255小于阀值设0
		}

		temp.copyTo(_temp, mask);

		circle(dst, Point2f(ActualX, ActualY), MaxRadiusAng2, Scalar(0, 255, 0), 1, 1);//角度外界限，宽度=1
		circle(dst, Point2f(ActualX, ActualY), MinRadiusAng2, Scalar(0, 255, 0), 1, 1);//角度内界限，宽度=1
#pragma endregion

#pragma region 轮廓筛选

		findContours(_temp, contours, hierarchy, CV_RETR_TREE, CV_CHAIN_APPROX_NONE, Point(0, 0));

		for (size_t i = 0; i < contours.size(); i++)//整张图里面遍历找到最大轮廓
		{
			double area = contourArea(contours[i], false);
			if ((area > SpecAreaAng2) && (area < SpecAreaAngSX2))
			{
				index++;
				BestMatch.push_back(contours[i]);

				if (area > maxAreaAng)
				{
					maxAreaAng = area;
				}
			}
		}

		drawContours(dst, BestMatch, -1, Scalar(0, 255, 255), 2);//找到的角度轮廓，宽度=2

		if (index > 0)
		{
			if (index == 1)
			{
				RotatedRect rect = minAreaRect(BestMatch[0]);

				line(dst, Point2f(ActualX, ActualY), rect.center, Scalar(0, 255, 255), 2);//画线指向缺口中心: rect.center，宽度=2

				//角度计算
				Point2f p2 = rect.center - Point2f(ActualX, ActualY);
				float c = -p2.y / (sqrt(p2.x*p2.x + p2.y*p2.y));
				a_out = acos(c) * 180 / CV_PI;
				if (ActualX > rect.center.x)
				{
					a_out = 360 - a_out;
				}
			}

			else
			{
				RotatedRect rect0 = minAreaRect(BestMatch[0]);
				RotatedRect rect1 = minAreaRect(BestMatch[1]);
				Point2f AngPoint = (rect0.center + rect1.center) / 2;

				line(dst, Point2f(ActualX, ActualY), AngPoint, Scalar(0, 255, 255), 2);//画线指向缺口中心,宽度=2

				//角度计算
				Point2f p2 = AngPoint - Point2f(ActualX, ActualY);
				float c = -p2.y / (sqrt(p2.x*p2.x + p2.y*p2.y));
				a_out = acos(c) * 180 / CV_PI;
				if (ActualX > AngPoint.x)
				{
					a_out = 360 - a_out;
				}
			}

		}

		else
		{
			a_out = 0;

			if (MarkErroMode2 == 0)
			{
				str << "未找到缺口";
				goto OUTPUT;
			}
		}

		output_Parameter_Float[3] = maxAreaAng;
		output_Parameter_Float[4] = index;
		output_Parameter_Float[5] = a_out;

#pragma endregion

#pragma region 检测区域

		ActualPianyiJP = sqrt((ActualX - ReferenceX2)*(ActualX - ReferenceX2) + (ActualY - ReferenceY2)*(ActualY - ReferenceY2));
		output_Parameter_Float[6] = ActualPianyiJP;
		if (ActualPianyiJP > ReferenceSpec2)
		{
			str << "偏移量NG" <<  endl;
			ActualPianyiJPflag = false;
		}

		ActualZhengfan = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusB1, MaxRadiusB1, GrayValueB1);
		output_Parameter_Float[7] = ActualZhengfan;
		if (CompareB1 == 0)
		{
			if (ActualZhengfan < SpecValueB1)
			{
				str << "正反检测NG" << endl;
				ActualZhengfanflag = false;
			}
		}
		else
		{
			if (ActualZhengfan > SpecValueB1)
			{
				str << "正反检测NG" << endl;
				ActualZhengfanflag = false;
			}
		}
		

		ActualTuhei = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusB2, MaxRadiusB2, GrayValueB2);
		output_Parameter_Float[8] = ActualTuhei;
		if (CompareB2 == 0)
		{
			if (ActualTuhei < SpecValueB2)
			{
				str << "涂黑检测NG" << endl;
				ActualTuheiflag = false;
			}
		}
		else
		{
			if (ActualTuhei > SpecValueB2)
			{
				str << "涂黑检测NG" << endl;
				ActualTuheiflag = false;
			}
		}
		

		ActualBupin = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusB3, MaxRadiusB3, GrayValueB3);
		output_Parameter_Float[9] = ActualBupin;
		if (CompareB3 == 0)
		{
			if (ActualBupin < SpecValueB3)
			{
				str << "部品检测NG" << endl;
				ActualBupinflag = false;
			}
		}
		else
		{
			if (ActualBupin > SpecValueB3)
			{
				str << "部品检测NG" << endl;
				ActualBupinflag = false;
			}
		}
		
#pragma endregion

#pragma region 结果返回

		if ( ActualPianyiJPflag && ActualZhengfanflag && ActualTuheiflag && ActualBupinflag)
		{
			JPResultflag = true;
			output_Parameter_Float[0] = JPResultflag;
		}
		else
		{
			JPResultflag = false;
			output_Parameter_Float[0] = JPResultflag;
		}
#pragma endregion


		OUTPUT:

#pragma region 文字输入
			  //字体大小
			  int text_Size = (int)((data.w* data.h / 10000 - 30) * 0.078 + 50);
			  //位置
			  Point text_Localtion01;
			  text_Localtion01.x = text_Size / 3;
			  text_Localtion01.y = text_Size / 3;
			  Point text_Localtion02;
			  text_Localtion02.x = text_Size / 3;
			  text_Localtion02.y = data.h - text_Size * 4;
			  Point text_Localtion03;
			  text_Localtion03.x = text_Size / 3;
			  text_Localtion03.y = data.h - text_Size * 3;

			  if (!JPResultflag)
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(255, 0, 0), text_Size, "黑体", 0);//RGB
				  output_Parameter_Float[0] = false;
			  }
			  else
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(0, 255, 0), text_Size, "黑体", 0);
			  }
#pragma endregion

#pragma region 图片返回
			  Mat output;
			  cvtColor(dst, output, CV_BGR2RGB);
			  int size = output.total() * output.elemSize();
			  data.size = size;
			  data.h = output.rows;
			  data.w = output.cols;
			  data.data_Output = (uchar *)calloc(size, sizeof(uchar));
			  std::memcpy(data.data_Output, output.data, size * sizeof(BYTE));
#pragma endregion

	}

	catch (const std::exception&)//exception&可能存在错误代码，C++里面的固定语法
	{
		output_Parameter_Float[0] = false;
		ErrOutput(data, input_Parameter, output_Parameter_Float);
		return 1;
	}
}


//挡光片图像处理算法
bool locationSM(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	try
	{
#pragma region 参数转换
		int IsShow = atoi(input_Parameter[0]);
		int ScanDirection = atoi(input_Parameter[1]);

		float DefaultX = atof(input_Parameter[2]);
		float DefaultY = atof(input_Parameter[3]);
		float MinRadius = atof(input_Parameter[4]);
		float MaxRadius = atof(input_Parameter[5]);
		float GrayValue = atof(input_Parameter[6]);
		float ReferenceX = atof(input_Parameter[7]);
		float ReferenceY = atof(input_Parameter[8]);
		float ReferenceSpec = atoi(input_Parameter[9]);

		float MinRadiusA1 = atof(input_Parameter[10]);
		float MaxRadiusA1 = atof(input_Parameter[11]);
		float GrayValueA1 = atoi(input_Parameter[12]);
		float SpecAreaCD = atoi(input_Parameter[13]);
		float SpecAreaZD = atoi(input_Parameter[14]);

		float MinRadiusA2 = atof(input_Parameter[15]);
		float MaxRadiusA2 = atof(input_Parameter[16]);
		float GrayValueA2 = atoi(input_Parameter[17]);
		float CompareA2 = atoi(input_Parameter[18]);
		float SpecValueA2 = atoi(input_Parameter[19]);

		float MinRadiusA3 = atof(input_Parameter[20]);
		float MaxRadiusA3 = atof(input_Parameter[21]);
		float GrayValueA3 = atoi(input_Parameter[22]);
		float CompareA3 = atoi(input_Parameter[23]);
		float SpecValueA3 = atoi(input_Parameter[24]);

		int TiduDirection = atoi(input_Parameter[25]);

		float ActualPianyiSM = 0;
		float ActualCDZD = 0;
		float ActualQuyu2 = 0;
		float ActualQuyu3 = 0;

#pragma endregion

#pragma region 本地参数		
		Mat dst;
		Mat src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//相机
		Mat mask = Mat::zeros(src.size(), src.type());
		Mat temp = Mat::zeros(src.size(), src.type());
		Mat srcblur;
		string result = DecodeDataMatrixFromMat(src);
		float ActualPianyiSMflag = true;
		float ActualCDZDflag = true;
		float ActualQuyu2flag = true;
		float ActualQuyu3flag = true;

		float SMResultflag = false;
		float ActualX = 0;
		float ActualY = 0;
		Point2f center = Point2f(DefaultX, DefaultY);//捕捉圆预输入圆心
		stringstream str;
#pragma endregion

#pragma region 图像预处理
		
		blur(src, srcblur, Size(3, 3));
		threshold(srcblur, temp, GrayValue, 255, THRESH_BINARY);
		
#pragma endregion

#pragma region 输出图片选择
		if (IsShow == 1)
		{
			cvtColor(temp, dst, COLOR_GRAY2RGB);
		}
		else
		{
			cvtColor(src, dst, COLOR_GRAY2RGB);
		}
		circle(dst, center, MaxRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆外界限,线宽=2
		circle(dst, center, MinRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆内界限,线宽=2
#pragma endregion

#pragma region 卡尺圆定位
		cv::Vec3i vecTmp = { (int)center.x, (int)center.y, (int)(MaxRadius + MinRadius) / 2 };//输入预设的圆心、半径
		int nV = (MaxRadius - MinRadius) / 2, nH = 5, nThreshold = GrayValue;//输入预设的扫描范围
		std::vector<cv::Point> stOutPt;//定义一个容器用来装扫描到的点

		//由内到外
		if (ScanDirection == 0)
		{
			if (TiduDirection == 0)
			{
				temp = 255 - temp;
			}
		}
		else
		{
			if (TiduDirection == 1)//
			{
				temp = 255 - temp;
			}
		}

		gen_Metrology_Model_circle(temp, vecTmp, nV, nH, nThreshold, stOutPt, ScanDirection);//调用函数查找圆上的点
		for (size_t i = 0; i < stOutPt.size(); i++)
		{
			line(dst, stOutPt[i], stOutPt[i], Scalar(255, 255, 0), 2);//查找到的点画圆1，宽度=2
		}

		std::vector < cv::Point> inPoints; inPoints.clear();//新建一个容器装拟合后的点
		circleInfo fitCircle;//定义一个拟合出来的圆的参数

		if (3 > stOutPt.size())
		{
			str << "有效点数不足，定位失败！" << endl;
			goto OUTPUT;
		}

		ransc_fit_circle(src, stOutPt, 816, 1.5, 60, inPoints, fitCircle);//对查找到的圆上的点进行拟合
		for (size_t i = 0; i < inPoints.size(); i++)
		{
			line(dst, inPoints[i], inPoints[i], Scalar(0, 255, 255), 2);//拟合后的点画圆2，宽度=2
		}

		if (3 > inPoints.size())
		{
			str << "拟合分数过低！定位失败！" << endl;
			goto OUTPUT;
		}
		ActualX = fitCircle.A;//捕捉到的圆心坐标X
		ActualY = fitCircle.B;//捕捉到的圆心坐标Y

		circle(dst, cv::Point2f(fitCircle.A, fitCircle.B), fitCircle.C, Scalar(255, 0, 0), 2);//捕捉到的圆，宽度=2
		str << "圆心:(" << ActualX << "," << ActualY << ")" << endl;
		output_Parameter_Float[1] = ActualX;
		output_Parameter_Float[2] = ActualY;

#pragma region 检测区域

		ActualPianyiSM = sqrt((ActualX - ReferenceX)*(ActualX - ReferenceX) + (ActualY - ReferenceY)*(ActualY - ReferenceY));
		output_Parameter_Float[3] = ActualPianyiSM;
		if (ActualPianyiSM > ReferenceSpec)
		{
			str << "偏移量NG" <<  endl;
			ActualPianyiSMflag = false;
		}

		ActualCDZD = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA1, MaxRadiusA1, GrayValueA1);
		output_Parameter_Float[4] = ActualCDZD;
		if ((ActualCDZD < SpecAreaCD) || (ActualCDZD > SpecAreaZD))
		{
			str << "重叠折叠NG" <<  endl;
			ActualCDZDflag = false;
		}

		ActualQuyu2 = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA2, MaxRadiusA2, GrayValueA2);
		output_Parameter_Float[5] = ActualQuyu2;
		if (CompareA2 == 0)
		{
			if (ActualQuyu2 < SpecValueA2)
			{
				str << "区域2检测NG" << endl;
				ActualQuyu2flag = false;
			}
		}
		else
		{
			if (ActualQuyu2 > SpecValueA2)
			{
				str << "区域2检测NG" << endl;
				ActualQuyu2flag = false;
			}
		}
		

		ActualQuyu3 = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA3, MaxRadiusA3, GrayValueA3);
		output_Parameter_Float[6] = ActualQuyu3;
		if (CompareA3 == 0)
		{
			if (ActualQuyu3 < SpecValueA3)
			{
				str << "区域3检测NG" << endl;
				ActualQuyu3flag = false;
			}
		}
		else
		{
			if (ActualQuyu3 > SpecValueA3)
			{
				str << "区域3检测NG" << endl;
				ActualQuyu3flag = false;
			}
		}
		
#pragma endregion

#pragma region 结果返回

		if (ActualPianyiSMflag && ActualCDZDflag && ActualQuyu2flag && ActualQuyu3flag)
		{
			SMResultflag = true;
			output_Parameter_Float[0] = SMResultflag;
		}
		else
		{
			SMResultflag = false;
			output_Parameter_Float[0] = SMResultflag;
		}

#pragma endregion


		OUTPUT:

#pragma region 文字输入
			  //字体大小
			  int text_Size = (int)((data.w* data.h / 10000 - 30) * 0.078 + 50); 
			  //位置
			  Point text_Localtion01;
			  text_Localtion01.x = text_Size / 3;
			  text_Localtion01.y = text_Size / 3;
			  Point text_Localtion02;
			  text_Localtion02.x = text_Size / 3;
			  text_Localtion02.y = data.h - text_Size * 4;
			  Point text_Localtion03;
			  text_Localtion03.x = text_Size / 3;
			  text_Localtion03.y = data.h - text_Size * 3;

			  if (!SMResultflag)
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(255, 0, 0), text_Size, "黑体", 0);//RGB
				  output_Parameter_Float[0] = false;
			  }
			  else
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(0, 255, 0), text_Size, "黑体", 0);
			  }
#pragma endregion

#pragma region 图片返回
			  Mat output;
			  cvtColor(dst, output, CV_BGR2RGB);
			  int size = output.total() * output.elemSize();
			  data.size = size;
			  data.h = output.rows;
			  data.w = output.cols;
			  data.data_Output = (uchar *)calloc(size, sizeof(uchar));
			  std::memcpy(data.data_Output, output.data, size * sizeof(BYTE));
#pragma endregion

	}

	catch (const std::exception&)//exception&可能存在错误代码，C++里面的固定语法
	{
		output_Parameter_Float[0] = false;
		ErrOutput(data, input_Parameter, output_Parameter_Float);
		return 1;
	}
}

//判胶图像处理算法
bool locationPJ(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	try
	{
#pragma region 参数转换
		int IsShow = atoi(input_Parameter[0]);
		int ScanDirection = atoi(input_Parameter[1]);

		float DefaultX = atof(input_Parameter[2]);
		float DefaultY = atof(input_Parameter[3]);
		float MinRadius = atof(input_Parameter[4]);
		float MaxRadius = atof(input_Parameter[5]);
		float GrayValue = atof(input_Parameter[6]);

		float ReferenceX = atof(input_Parameter[13]);
		float ReferenceY = atof(input_Parameter[14]);
		float ReferenceSpec = atoi(input_Parameter[15]);

		float MinRadiusA1 = atof(input_Parameter[16]);
		float MaxRadiusA1 = atof(input_Parameter[17]);
		float GrayValueA1 = atoi(input_Parameter[18]);
		float CompareA1 = atoi(input_Parameter[19]);
		float SpecValueA1 = atoi(input_Parameter[20]);

		float MinRadiusA2 = atof(input_Parameter[21]);
		float MaxRadiusA2 = atof(input_Parameter[22]);
		float GrayValueA2 = atoi(input_Parameter[23]);
		float CompareA2 = atoi(input_Parameter[24]);
		float SpecValueA2 = atoi(input_Parameter[25]);

		float MinRadiusA3 = atof(input_Parameter[26]);
		float MaxRadiusA3 = atof(input_Parameter[27]);
		float GrayValueA3 = atoi(input_Parameter[28]);
		float CompareA3 = atoi(input_Parameter[29]);
		float SpecValueA3 = atoi(input_Parameter[30]);

		float MinRadiusA4 = atof(input_Parameter[31]);
		float MaxRadiusA4 = atof(input_Parameter[32]);
		float GrayValueA4 = atoi(input_Parameter[33]);
		float CompareA4 = atoi(input_Parameter[34]);
		float SpecValueA4 = atoi(input_Parameter[35]);

		int ImageCurrentCount = atoi(input_Parameter[36]);
		int PJimagesaveflag = atoi(input_Parameter[37]);

		int TiduDirection = atoi(input_Parameter[38]);

		float JiaoShuiValueA2 = atoi(input_Parameter[39]);
	
		float ActualPianyiJT = 0;
		float ActualDJnei = 0;
		float ActualDJwai = 0;
		float ActualYJnei = 0;
		float ActualYJwai = 0;

#pragma endregion

#pragma region 本地参数		
		Mat dst;
		Mat _temp;
		Mat src;
		Mat _src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//相机
		flip(_src, src, 0);//flip(inputArry,outputArry,type):0垂直翻转，1水平翻转，-1水平和垂直翻转
		Mat mask = Mat::zeros(src.size(), src.type());
		Mat temp = Mat::zeros(src.size(), src.type());
		Mat srcblur;

		float ActualPianyiJTflag = true;
		float ActualDJneiflag = true;
		float ActualDJwaiflag = true;
		float ActualYJneiflag = true;
		float ActualYJwaiflag = true;

		float JTResultflag = false;
		float ActualX = 0;
		float ActualY = 0;
		Point2f center = Point2f(DefaultX, DefaultY);//捕捉圆预输入圆心
		stringstream str;

		//新增的系统时间获取用于保存图片信息
		time_t nowtime;
		time(&nowtime);
		tm p;
		localtime_s(&p, &nowtime);

#pragma endregion

#pragma region 图像预处理
		
		blur(src, srcblur, Size(3, 3));
		threshold(srcblur, temp, GrayValue, 255, THRESH_BINARY);
		
#pragma endregion

#pragma region 输出图片选择
		if (IsShow == 1)
		{
			cvtColor(temp, dst, COLOR_GRAY2RGB);
		}
		else
		{
			cvtColor(src, dst, COLOR_GRAY2RGB);
		}
		circle(dst, center, MaxRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆外界限,线宽=2
		circle(dst, center, MinRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆内界限,线宽=2
#pragma endregion

#pragma region 卡尺圆定位
		cv::Vec3i vecTmp = { (int)center.x, (int)center.y, (int)(MaxRadius + MinRadius) / 2 };//输入预设的圆心、半径
		int nV = (MaxRadius - MinRadius) / 2, nH = 5, nThreshold = GrayValue;//输入预设的扫描范围
		std::vector<cv::Point> stOutPt;//定义一个容器用来装扫描到的点

		//由内到外
		if (ScanDirection == 0)
		{
			if (TiduDirection == 0)
			{
				temp = 255 - temp;
			}
		}
		else
		{
			if (TiduDirection == 1)//
			{
				temp = 255 - temp;
			}
		}

		gen_Metrology_Model_circle(temp, vecTmp, nV, nH, nThreshold, stOutPt, ScanDirection);//调用函数查找圆上的点
		for (size_t i = 0; i < stOutPt.size(); i++)
		{
			line(dst, stOutPt[i], stOutPt[i], Scalar(255, 255, 0), 2);//查找到的点画圆1，宽度=2
		}

		std::vector < cv::Point> inPoints; inPoints.clear();//新建一个容器装拟合后的点
		circleInfo fitCircle;//定义一个拟合出来的圆的参数

		if (3 > stOutPt.size())
		{
			str << "有效点数不足，定位失败！" << p.tm_mon + 1 << "月" << p.tm_mday << "日" << p.tm_hour << ":" << p.tm_min << ":" << p.tm_sec << endl;
			goto OUTPUT;
		}

		ransc_fit_circle(src, stOutPt, 816, 1.5, 60, inPoints, fitCircle);//对查找到的圆上的点进行拟合
		for (size_t i = 0; i < inPoints.size(); i++)
		{
			line(dst, inPoints[i], inPoints[i], Scalar(0, 255, 255), 2);//拟合后的点画圆2，宽度=2
		}

		if (3 > inPoints.size())
		{
			str << "拟合分数过低！定位失败！" << p.tm_mon + 1 << "月" << p.tm_mday << "日" << p.tm_hour << ":" << p.tm_min << ":" << p.tm_sec << endl;
			goto OUTPUT;
		}
		ActualX = fitCircle.A;//捕捉到的圆心坐标X
		ActualY = fitCircle.B;//捕捉到的圆心坐标Y

		circle(dst, cv::Point2f(fitCircle.A, fitCircle.B), fitCircle.C, Scalar(255, 0, 0), 2);//捕捉到的圆，宽度=2
		str << "圆心:(" << ActualX << "," << ActualY << ")" << p.tm_mon + 1 << "月" << p.tm_mday << "日" << p.tm_hour << ":" << p.tm_min << ":" << p.tm_sec << endl;
		output_Parameter_Float[1] = ActualX;
		output_Parameter_Float[2] = ActualY;


#pragma region 检测区域

		ActualPianyiJT = sqrt((ActualX - ReferenceX)*(ActualX - ReferenceX) + (ActualY - ReferenceY)*(ActualY - ReferenceY));
		output_Parameter_Float[6] = ActualPianyiJT;
		if (ActualPianyiJT > ReferenceSpec)
		{
			str << "偏移量NG" << endl;
			ActualPianyiJTflag = false;
		}


		ActualDJnei = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA1, MaxRadiusA1, GrayValueA1);
		output_Parameter_Float[7] = ActualDJnei;
		if (CompareA1 == 0)
		{
			if (ActualDJnei < SpecValueA1)
			{
				str << "点胶内检测NG" << endl;
				ActualDJneiflag = false;
			}
		}
		else
		{
			if (ActualDJnei > SpecValueA1)
			{
				str << "点胶内检测NG" << endl;
				ActualDJneiflag = false;
			}
		}


		ActualDJwai = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA2, MaxRadiusA2, GrayValueA2);
		output_Parameter_Float[8] = ActualDJwai;
		if (CompareA2 == 0)
		{
			if ((ActualDJwai < SpecValueA2) || (ActualDJwai > JiaoShuiValueA2))
			{
				str << "点胶外检测NG或点胶角度过大" << endl;
				ActualDJwaiflag = false;
			}
		}
		else
		{
			if ((ActualDJwai > SpecValueA2) || (ActualDJwai < JiaoShuiValueA2))
			{
				str << "点胶外检测NG或点胶角度过大" << endl;
				ActualDJwaiflag = false;
			}
		}


		ActualYJnei = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA3, MaxRadiusA3, GrayValueA3);
		output_Parameter_Float[9] = ActualYJnei;
		if (CompareA3 == 0)
		{
			if (ActualYJnei < SpecValueA3)
			{
				str << "溢胶内检测NG" << endl;
				ActualYJneiflag = false;
			}
		}
		else
		{
			if (ActualYJnei > SpecValueA3)
			{
				str << "溢胶内检测NG" << endl;
				ActualYJneiflag = false;
			}
		}


		ActualYJwai = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA4, MaxRadiusA4, GrayValueA4);
		output_Parameter_Float[10] = ActualYJwai;
		if (CompareA4 == 0)
		{
			if (ActualYJwai < SpecValueA4)
			{
				str << "溢胶外检测NG" << endl;
				ActualYJwaiflag = false;
			}
		}
		else
		{
			if (ActualYJwai > SpecValueA4)
			{
				str << "溢胶外检测NG" << endl;
				ActualYJwaiflag = false;
			}
		}


#pragma endregion

#pragma region 结果返回

		if (ActualPianyiJTflag && ActualDJneiflag && ActualDJwaiflag && ActualYJneiflag && ActualYJwaiflag)
		{
			JTResultflag = true;
			output_Parameter_Float[0] = JTResultflag;
		}
		else
		{
			JTResultflag = false;
			output_Parameter_Float[0] = JTResultflag;
		}

#pragma endregion


		OUTPUT:

#pragma region 文字输入
			  //字体大小
			  int text_Size = (int)((data.w* data.h / 10000 - 30) * 0.078 + 50);
			  //位置
			  Point text_Localtion01;
			  text_Localtion01.x = text_Size / 3;
			  text_Localtion01.y = text_Size / 3;
			  Point text_Localtion02;
			  text_Localtion02.x = text_Size / 3;
			  text_Localtion02.y = data.h - text_Size * 4;
			  Point text_Localtion03;
			  text_Localtion03.x = text_Size / 3;
			  text_Localtion03.y = data.h - text_Size * 3;

			  if (!JTResultflag)
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(255, 0, 0), text_Size, "黑体", 0);//RGB
				  output_Parameter_Float[0] = false;
			  }
			  else
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(0, 255, 0), text_Size, "黑体", 0);
			  }
#pragma endregion

#pragma region 图片返回
			  Mat output;
			  cvtColor(dst, output, CV_BGR2RGB);
			  int size = output.total() * output.elemSize();
			  data.size = size;
			  data.h = output.rows;
			  data.w = output.cols;
			  data.data_Output = (uchar *)calloc(size, sizeof(uchar));
			  std::memcpy(data.data_Output, output.data, size * sizeof(BYTE));

			  //仅在通信触发时保存图片
			  if (PJimagesaveflag == 1)
			  {
				  string _ImageCurrentCount = to_string(ImageCurrentCount);
				  string imagesavepath = "PJimagesave\\" + _ImageCurrentCount + ".jpg";
				  imwrite(imagesavepath, dst);
			  }
			  
#pragma endregion

	}

	catch (const std::exception&)//exception&可能存在错误代码，C++里面的固定语法
	{
		output_Parameter_Float[0] = false;
		ErrOutput(data, input_Parameter, output_Parameter_Float);
		return 1;
	}
}

//点胶图像处理算法
bool locationDJ(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	try
	{
#pragma region 参数转换
		int IsShow = atoi(input_Parameter[0]);
		int ScanDirection = atoi(input_Parameter[1]);

		float DefaultX = atof(input_Parameter[2]);
		float DefaultY = atof(input_Parameter[3]);
		float MinRadius = atof(input_Parameter[4]);
		float MaxRadius = atof(input_Parameter[5]);
		float GrayValue = atof(input_Parameter[6]);

		float MinRadiusAng = atof(input_Parameter[8]);
		float MaxRadiusAng = atof(input_Parameter[9]);
		float GrayValueAng = atoi(input_Parameter[10]);
		float MarkMode = atoi(input_Parameter[11]);
		float SpecAreaAng = atoi(input_Parameter[12]);

		float ReferenceX = atof(input_Parameter[13]);
		float ReferenceY = atof(input_Parameter[14]);
		float ReferenceSpec = atoi(input_Parameter[15]);

		float MinRadiusA1 = atof(input_Parameter[16]);
		float MaxRadiusA1 = atof(input_Parameter[17]);
		float GrayValueA1 = atoi(input_Parameter[18]);
		float CompareA1 = atoi(input_Parameter[19]);
		float SpecValueA1 = atoi(input_Parameter[20]);

		float MinRadiusA2 = atof(input_Parameter[21]);
		float MaxRadiusA2 = atof(input_Parameter[22]);
		float GrayValueA2 = atoi(input_Parameter[23]);
		float CompareA2 = atoi(input_Parameter[24]);
		float SpecValueA2 = atoi(input_Parameter[25]);

		float MinRadiusA3 = atof(input_Parameter[26]);
		float MaxRadiusA3 = atof(input_Parameter[27]);
		float GrayValueA3 = atoi(input_Parameter[28]);
		float CompareA3 = atoi(input_Parameter[29]);
		float SpecValueA3 = atoi(input_Parameter[30]);

		float MinRadiusA4 = atof(input_Parameter[31]);
		float MaxRadiusA4 = atof(input_Parameter[32]);
		float GrayValueA4 = atoi(input_Parameter[33]);
		float CompareA4 = atoi(input_Parameter[34]);
		float SpecValueA4 = atoi(input_Parameter[35]);

		int ImageCurrentCount = atoi(input_Parameter[36]);
		int DJimagesaveflag = atoi(input_Parameter[37]);

		float SpecAreaAngSX = atoi(input_Parameter[38]);
		float MarkErroMode = atoi(input_Parameter[39]);

		int TiduDirection = atoi(input_Parameter[40]);

		float ActualPianyiJT = 0;
		float ActualLouzu = 0;
		float ActualDGP = 0;
		float ActualZuzhuang = 0;
		float ActualZuzhuang2 = 0;

#pragma endregion

#pragma region 本地参数		
		Mat dst;
		Mat _temp;
		Mat src;
		Mat _src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//相机
		flip(_src, src, 0);//flip(inputArry,outputArry,type):0垂直翻转，1水平翻转，-1水平和垂直翻转
		Mat mask = Mat::zeros(src.size(), src.type());
		Mat temp = Mat::zeros(src.size(), src.type());
		Mat srcblur;

		float ActualPianyiJTflag = true;
		float ActualLouzuflag = true;
		float ActualDGPflag = true;
		float ActualZuzhuangflag = true;
		float ActualZuzhuang2flag = true;

		float JTResultflag = false;
		float ActualX = 0;
		float ActualY = 0;
		Point2f center = Point2f(DefaultX, DefaultY);//捕捉圆预输入圆心
		stringstream str;

		int index = 0;//寻找角度轮廓编号
		float maxAreaAng = 0;//寻找角度最大轮廓面积		
		float a_out = 0;//角度
		vector<vector<Point2i>> BestMatch;//寻找角度轮廓集合
		vector<vector<Point2i>> contours;
		vector<Vec4i> hierarchy;

		//新增的系统时间获取用于保存图片信息
		time_t nowtime;
		time(&nowtime);
		tm p;
		localtime_s(&p, &nowtime);

#pragma endregion

#pragma region 图像预处理
		
		blur(src, srcblur, Size(3, 3));
		threshold(srcblur, temp, GrayValue, 255, THRESH_BINARY);
		
#pragma endregion

#pragma region 输出图片选择
		if (IsShow == 1)
		{
			cvtColor(temp, dst, COLOR_GRAY2RGB);
		}
		else
		{
			cvtColor(src, dst, COLOR_GRAY2RGB);
		}
		circle(dst, center, MaxRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆外界限,线宽=2
		circle(dst, center, MinRadius, Scalar(0, 0, 255), 2, 1);//捕捉圆内界限,线宽=2
#pragma endregion

#pragma region 卡尺圆定位
		cv::Vec3i vecTmp = { (int)center.x, (int)center.y, (int)(MaxRadius + MinRadius) / 2 };//输入预设的圆心、半径
		int nV = (MaxRadius - MinRadius) / 2, nH = 5, nThreshold = GrayValue;//输入预设的扫描范围
		std::vector<cv::Point> stOutPt;//定义一个容器用来装扫描到的点

		//由内到外
		if (ScanDirection == 0)
		{
			if (TiduDirection == 0)
			{
				temp = 255 - temp;
			}
		}
		else
		{
			if (TiduDirection == 1)//
			{
				temp = 255 - temp;
			}
		}

		gen_Metrology_Model_circle(temp, vecTmp, nV, nH, nThreshold, stOutPt, ScanDirection);//调用函数查找圆上的点
		for (size_t i = 0; i < stOutPt.size(); i++)
		{
			line(dst, stOutPt[i], stOutPt[i], Scalar(255, 255, 0), 2);//查找到的点画圆1，宽度=2
		}

		std::vector < cv::Point> inPoints; inPoints.clear();//新建一个容器装拟合后的点
		circleInfo fitCircle;//定义一个拟合出来的圆的参数

		if (3 > stOutPt.size())
		{
			str << "有效点数不足，定位失败！" << p.tm_mon + 1 << "月" << p.tm_mday << "日" << p.tm_hour << ":" << p.tm_min << ":" << p.tm_sec << endl;
			goto OUTPUT;
		}

		ransc_fit_circle(src, stOutPt, 816, 1.5, 60, inPoints, fitCircle);//对查找到的圆上的点进行拟合
		for (size_t i = 0; i < inPoints.size(); i++)
		{
			line(dst, inPoints[i], inPoints[i], Scalar(0, 255, 255), 2);//拟合后的点画圆2，宽度=2
		}

		if (3 > inPoints.size())
		{
			str << "拟合分数过低！定位失败！" << p.tm_mon + 1 << "月" << p.tm_mday << "日" << p.tm_hour << ":" << p.tm_min << ":" << p.tm_sec << endl;
			goto OUTPUT;
		}
		ActualX = fitCircle.A;//捕捉到的圆心坐标X
		ActualY = fitCircle.B;//捕捉到的圆心坐标Y

		circle(dst, cv::Point2f(fitCircle.A, fitCircle.B), fitCircle.C, Scalar(255, 0, 0), 2);//捕捉到的圆，宽度=2
		str << "圆心:(" << ActualX << "," << ActualY << ")" << p.tm_mon + 1 << "月" << p.tm_mday << "日" << p.tm_hour << ":" << p.tm_min << ":" << p.tm_sec << endl;
		output_Parameter_Float[1] = ActualX;
		output_Parameter_Float[2] = ActualY;

		//角度识别
#pragma region ROI获取  
		mask *= 0;
		circle(mask, Point(ActualX, ActualY), MaxRadiusAng, Scalar(255), -1);
		circle(mask, Point(ActualX, ActualY), MinRadiusAng, Scalar(000), -1);

		if (MarkMode == 0)
		{
			threshold(src, temp, GrayValueAng, 255, THRESH_BINARY_INV);//反二值化大于阀值设0小于阀值设255			
		}
		else
		{
			threshold(src, temp, GrayValueAng, 255, THRESH_BINARY);//二值化大于阀值设255小于阀值设0
		}

		temp.copyTo(_temp, mask);

		circle(dst, Point2f(ActualX, ActualY), MaxRadiusAng, Scalar(0, 255, 0), 1, 1);//角度外界限，宽度=1
		circle(dst, Point2f(ActualX, ActualY), MinRadiusAng, Scalar(0, 255, 0), 1, 1);//角度内界限，宽度=1
#pragma endregion

#pragma region 轮廓筛选

		findContours(_temp, contours, hierarchy, CV_RETR_TREE, CV_CHAIN_APPROX_NONE, Point(0, 0));

		for (size_t i = 0; i < contours.size(); i++)//整张图里面遍历找到最大轮廓
		{
			double area = contourArea(contours[i], false);
			if ((area > SpecAreaAng) && (area < SpecAreaAngSX))
			{
				index++;
				BestMatch.push_back(contours[i]);

				if (area > maxAreaAng)
				{
					maxAreaAng = area;
				}
			}
		}

		drawContours(dst, BestMatch, -1, Scalar(0, 255, 255), 2);//找到的角度轮廓，宽度=2

		if (index > 0)
		{
			if (index == 1)
			{
				RotatedRect rect = minAreaRect(BestMatch[0]);

				line(dst, Point2f(ActualX, ActualY), rect.center, Scalar(0, 255, 255), 2);//画线指向缺口中心:rect.center，宽度=2

				//角度计算
				Point2f p2 = rect.center - Point2f(ActualX, ActualY);
				float c = -p2.y / (sqrt(p2.x*p2.x + p2.y*p2.y));
				a_out = acos(c) * 180 / CV_PI;
				if (ActualX > rect.center.x)
				{
					a_out = 360 - a_out;
				}
			}

			else
			{
				RotatedRect rect0 = minAreaRect(BestMatch[0]);
				RotatedRect rect1 = minAreaRect(BestMatch[1]);
				Point2f AngPoint = (rect0.center + rect1.center) / 2;

				line(dst, Point2f(ActualX, ActualY), AngPoint, Scalar(0, 255, 255), 2);//画线指向缺口中心,宽度=2

				//角度计算
				Point2f p2 = AngPoint - Point2f(ActualX, ActualY);
				float c = -p2.y / (sqrt(p2.x*p2.x + p2.y*p2.y));
				a_out = acos(c) * 180 / CV_PI;
				if (ActualX > AngPoint.x)
				{
					a_out = 360 - a_out;
				}
			}

		}

		else
		{
			a_out = 0;

			if (MarkErroMode == 0)
			{
				str << "未找到缺口";
				goto OUTPUT;
			}
		}

		output_Parameter_Float[3] = maxAreaAng;
		output_Parameter_Float[4] = index;
		output_Parameter_Float[5] = a_out;

#pragma endregion

#pragma region 检测区域

		ActualPianyiJT = sqrt((ActualX - ReferenceX)*(ActualX - ReferenceX) + (ActualY - ReferenceY)*(ActualY - ReferenceY));
		output_Parameter_Float[6] = ActualPianyiJT;
		if (ActualPianyiJT > ReferenceSpec)
		{
			str << "偏移量NG" << endl;
			ActualPianyiJTflag = false;
		}


		ActualLouzu = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA1, MaxRadiusA1, GrayValueA1);
		output_Parameter_Float[7] = ActualLouzu;
		if (CompareA1 == 0)
		{
			if (ActualLouzu < SpecValueA1)
			{
				str << "漏组检测NG" << endl;
				ActualLouzuflag = false;
			}
		}
		else
		{
			if (ActualLouzu > SpecValueA1)
			{
				str << "漏组检测NG" << endl;
				ActualLouzuflag = false;
			}
		}


		ActualDGP = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA2, MaxRadiusA2, GrayValueA2);
		output_Parameter_Float[8] = ActualDGP;
		if (CompareA2 == 0)
		{
			if (ActualDGP < SpecValueA2)
			{
				str << "挡光片检测NG" << endl;
				ActualDGPflag = false;
			}
		}
		else
		{
			if (ActualDGP > SpecValueA2)
			{
				str << "挡光片检测NG" << endl;
				ActualDGPflag = false;
			}
		}


		ActualZuzhuang = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA3, MaxRadiusA3, GrayValueA3);
		output_Parameter_Float[9] = ActualZuzhuang;
		if (CompareA3 == 0)
		{
			if (ActualZuzhuang < SpecValueA3)
			{
				str << "组装检测NG" << endl;
				ActualZuzhuangflag = false;
			}
		}
		else
		{
			if (ActualZuzhuang > SpecValueA3)
			{
				str << "组装检测NG" << endl;
				ActualZuzhuangflag = false;
			}
		}


		ActualZuzhuang2 = CircleTraverse(dst, src, ActualX, ActualY, MinRadiusA4, MaxRadiusA4, GrayValueA4);
		output_Parameter_Float[10] = ActualZuzhuang2;
		if (CompareA4 == 0)
		{
			if (ActualZuzhuang2 < SpecValueA4)
			{
				str << "组装检测2 NG" << endl;
				ActualZuzhuang2flag = false;
			}
		}
		else
		{
			if (ActualZuzhuang2 > SpecValueA4)
			{
				str << "组装检测2 NG" << endl;
				ActualZuzhuang2flag = false;
			}
		}


#pragma endregion

#pragma region 结果返回

		if (ActualPianyiJTflag && ActualLouzuflag && ActualDGPflag && ActualZuzhuangflag && ActualZuzhuang2flag)
		{
			JTResultflag = true;
			output_Parameter_Float[0] = JTResultflag;
		}
		else
		{
			JTResultflag = false;
			output_Parameter_Float[0] = JTResultflag;
		}

#pragma endregion


		OUTPUT:

#pragma region 文字输入
			  //字体大小
			  int text_Size = (int)((data.w* data.h / 10000 - 30) * 0.078 + 50);
			  //位置
			  Point text_Localtion01;
			  text_Localtion01.x = text_Size / 3;
			  text_Localtion01.y = text_Size / 3;
			  Point text_Localtion02;
			  text_Localtion02.x = text_Size / 3;
			  text_Localtion02.y = data.h - text_Size * 4;
			  Point text_Localtion03;
			  text_Localtion03.x = text_Size / 3;
			  text_Localtion03.y = data.h - text_Size * 3;

			  if (!JTResultflag)
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(255, 0, 0), text_Size, "黑体", 0);//RGB
				  output_Parameter_Float[0] = false;
			  }
			  else
			  {
				  putTextZH(dst, str.str().c_str(), text_Localtion01, Scalar(0, 255, 0), text_Size, "黑体", 0);
			  }
#pragma endregion

#pragma region 图片返回
			  Mat output;
			  cvtColor(dst, output, CV_BGR2RGB);
			  int size = output.total() * output.elemSize();
			  data.size = size;
			  data.h = output.rows;
			  data.w = output.cols;
			  data.data_Output = (uchar *)calloc(size, sizeof(uchar));
			  std::memcpy(data.data_Output, output.data, size * sizeof(BYTE));

			  //仅在通信触发时保存图片
			  if (DJimagesaveflag == 1)
			  {
				  string _ImageCurrentCount = to_string(ImageCurrentCount);
				  string imagesavepath = "DJimagesave\\" + _ImageCurrentCount + ".jpg";
				  imwrite(imagesavepath, dst);
			  }

#pragma endregion

	}

	catch (const std::exception&)//exception&可能存在错误代码，C++里面的固定语法
	{
		output_Parameter_Float[0] = false;
		ErrOutput(data, input_Parameter, output_Parameter_Float);
		return 1;
	}
}


//进入算法选择
bool MV_EntryPoint(int type, BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	try
	{
		switch (type)
		{
		case 0:locationJT(data, input_Parameter, output_Parameter_Float); break;
		case 1:locationJP(data, input_Parameter, output_Parameter_Float); break;
		case 2:locationSM(data, input_Parameter, output_Parameter_Float); break;
		case 3:locationPJ(data, input_Parameter, output_Parameter_Float); break;
		case 4:locationDJ(data, input_Parameter, output_Parameter_Float); break;

		default:
			break;
		}		
	}
	catch (const std::exception&)
	{
		ErrOutput(data, input_Parameter, output_Parameter_Float);
	}

	return false;
}

bool MV_Release(BmpBuf &data)
{
	delete data.data_Output;
	data.data_Output = NULL;

	data.size = 0;
	return 0;
}


