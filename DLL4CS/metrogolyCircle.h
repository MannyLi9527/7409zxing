//#include "stdafx.h"
#include "pch.h"
/**************************************************************************
**File name		: metrogolyCircle.h
**Author		: Bigshow Zheng
**Date			: 2021-03-16
**Description   : find circle
**
**History       :
**
**
*******************************************************************************/
#pragma once
#include <vector>
#include <math.h>
#include "opencv2/opencv.hpp"


/**
*@brief   ：定义圆的基本参数
*@ A	  ：圆心X坐标
*@ B	  ：圆心Y坐标
*@ C	  ：圆的半斤
*@ Info	  ：判断构建圆是否成功
*/
typedef struct circleInfo
{
	float A;							                 //参数a
	float B;							                 //参数b
	float C;							                 //参数c
	bool Info;						                     //是否找到圆的标志位

	circleInfo() :A(), B(), C(), Info() {}               //无参数的构造函数数组初始化时调用
	circleInfo(float a, float b, float c, bool is_info)
		:A(a), B(b), C(c), Info(is_info) {}              //有参构造
};



/**
*@brief                   : 获取ROI圆点
*@inputPic                ：输入图片
*@mainCircle              : 输入初定位圆心、半径
*@nPerpendicularTHreshold : 输入圆垂直偏离查找距离
*@nTangetialThreshold     ：输入圆相切偏离查找距离
*@nMeasureThreshold       ：输入梯度查找阈值
*@stOutPoint              ：输出查找到的点
*@return                  ：无
*/
void gen_Metrology_Model_circle(cv::Mat &inputPic, cv::Vec3i &mainCircle,
	int nPerpendicularTHreshold, int nTangetialThreshold,
	int nMeasureThreshold, std::vector<cv::Point> &stOutPoint, bool ore);


/**
*@brief       : 获得圆信息
*@input       ：输入图片
*@allPoints   ：输入的所有点___gen_Metrology_Model_circle函数中的参数stOutPoint
*@maxIterCnt  ：迭代次数
*@disTH       ：离散距离阈值
*@minScore    ：最低分值
*@inPoints    ：使用点集
*@fitCircle   ：拟合的圆
*@return      ：无
*/
void ransc_fit_circle(cv::Mat &input, std::vector<cv::Point> &allPoints,
	int maxIterCnt, float disTH, float minScor, std::vector<cv::Point> & inPoints, circleInfo& fitCircle);


/**call function

	 cv::Mat img = cv::imread("./11.bmp"));//导入图片
	 if (img.channels() != 1) cv::cvtColor(img, img, cv::COLOR_BGR2GRAY);//确认格式转换成灰度
	 cv::Vec3i vecTmp = { 1965, 627, 50 };//预设参数
	 int nV = 30, nH = 5, nThreshold = 10;
	 std::vector<cv::Point> stOutPt;//输出队列
	 gen_Metrology_Model_circle(img, vecTmp, nV, nH, nThreshold, stOutPt);//调用函数查找边缘的点

	 std::vector < cv::Point> inPoints; inPoints.clear();定义一个数组
	 circleInfo fitCircle;找到的圆心和半径
	 ransc_fit_circle(img, stOutPt, 100, 60, 30, inPoints, fitCircle);拟合圆

	 cv::circle(img, cv::Point(fitCircle.A, fitCircle.B), fitCircle.C, cv::Scalar(255));把圆画到图片上

*/