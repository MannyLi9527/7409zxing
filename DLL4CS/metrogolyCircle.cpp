//#include "stdafx.h"
#include "pch.h"
#include "metrogolyCircle.h"

/**
*@brief    根据输入点、圆心 、半径；求解点与圆的距离
*@pInput   输入点
*@pRadius  输入圆心
*@fRadius  输入半径
*@return   返回距离
*/
static float dDifCircle(cv::Point pInput, cv::Point pRadius, float fRadius) {
	//the value is between two points the distance  
	float fDistance = powf((pInput.x - pRadius.x), 2) + powf((pInput.y - pRadius.y), 2);
	fDistance = sqrtf(fDistance);

	return fabs(fDistance - fRadius);
}

/**
*@brief         ：获取ROI点
*@input         ：输入图像
*@inputRet      ：输入旋转矩形
*@nThresholdImg : 图像梯度阈值
*@return        ：返回符合点
*/
static cv::Point2f fromRotatedGetPoint(cv::Mat &input, cv::RotatedRect &inPutRet, int nThresholdImg,bool ore)
{
	cv::Point2f pRet;

	//The order is bottomLeft, topLeft, topRight, bottomRight --> 顺序: 左下，左上，右上，右下
	cv::Point2f p4Corner[4];
	inPutRet.points(p4Corner);
	cv::Point P0, P1, P2, P3;
	for (int i = 0; i < 4; i++) {
		switch (i) {
		case 0: P0 = p4Corner[i]; break;
		case 1: P1 = p4Corner[i]; break;
		case 2: P2 = p4Corner[i]; break;
		case 3: P3 = p4Corner[i]; break;
		}
	}
	/**
	1.  Opencv图像坐标系，左上角为原点O(0, 0)，X轴向右递增，Y轴向下递增，单位为像素。

	2. 矩形4个顶点位置的确定，是理解其它各变量的基础，其中p[0]点是关键。
	顶点p[0]的位置可以这样理解：
	ⓐ 如果没有边与坐标轴平行，则Y坐标最大的点为p[0]点；
	ⓑ 如果有边与坐标轴平行，则有两个Y坐标最大的点。此时，左侧的点为p[0]点。
	即：Y坐标最大的点为p[0]。如果有两个最大的Y坐标，则左侧点（X坐标较小）为p[0]。

	3. p[0]~p[3]按顺时针方向依次排列。
	4. p[0]到p[3]之间的距离为宽width，其邻边为高height。
	**/

	cv::Point2f P1Center, P2Center;
	//左下大于右下
	if (inPutRet.size.width > inPutRet.size.height) {
		P1Center = cv::Point2f((P0.x + P1.x) / 2, (P0.y + P1.y) / 2);
		P2Center = cv::Point2f((P2.x + P3.x) / 2, (P2.y + P3.y) / 2);
	}
	else {
		P1Center = cv::Point2f((P0.x + P3.x) / 2, (P0.y + P3.y) / 2);
		P2Center = cv::Point2f((P1.x + P2.x) / 2, (P1.y + P2.y) / 2);
	}

	//
	cv::LineIterator lit(input, P1Center, P2Center);
	//std::cout << "P1: " << P1Center << ", P2: " << P2Center << std::endl;
	std::vector<cv::Point> line_poits_coord;
	cv::Mat line_grayval = cv::Mat::zeros(1, lit.count + 1, CV_8U);
	for (int i = 0; i < lit.count; i++, ++lit) {

		cv::Point pt(lit.pos());
		line_poits_coord.push_back(pt);
		line_grayval.ptr(0)[i] = input.ptr(pt.y)[pt.x]; //第y行x列
	}

	//进行滤波
	cv::Mat kernel = (cv::Mat_<int>(1, 3) << -1, 0, 1);
	cv::Mat dst;
	dst.convertTo(dst, CV_32FC1);
	cv::filter2D(line_grayval, dst, -1, kernel);

	int max = INT_MIN, pos = -1;

	if (ore)
	{
		for (int i = 1; i < lit.count - 3; i++) {
			if (dst.ptr(0)[i] > max && dst.ptr(0)[i] >= nThresholdImg) {
				max = dst.ptr(0)[i];
				pos = i;
			}
		}
	}
	else
	{
		for (int i = lit.count - 4; i > 0 ; i--) {
			if (dst.ptr(0)[i] > max && dst.ptr(0)[i] >= nThresholdImg) {
				max = dst.ptr(0)[i];
				pos = i;
			}
		}
	}

	


	

	if (pos < 0)
	{
		pRet = cv::Point(0, 0);
	}
	else
	{
		pRet = line_poits_coord[pos];
	}


	return pRet;
}

/**
*@brief   : 生成圆
*@vtPt    : 输入三个点
*@return  : 返回圆心、半径
*/
static circleInfo genCicle(std::vector<cv::Point> vtPt) {

	circleInfo circleInf;
	circleInf.Info = false;

	if (vtPt.size() != 3)
		return circleInf;

	cv::Point P1, P2, P3;
	for (size_t t = 0; t < vtPt.size(); t++) {
		switch (t) {
		case 0:  P1.x = vtPt[t].x, P1.y = vtPt[t].y; break;
		case 1:  P2.x = vtPt[t].x, P2.y = vtPt[t].y; break;
		case 2:  P3.x = vtPt[t].x, P3.y = vtPt[t].y; break;
		default: return circleInf;
		}
	}

	int a = P1.x - P2.x;
	int b = P1.y - P2.y;
	int c = P1.x - P3.x;
	int d = P1.y - P3.y;
	float e1 = pow(P1.x, 2) - pow(P2.x, 2), e2 = pow(P2.y, 2) - pow(P1.y, 2);
	float e = (e1 - e2) / 2;
	float f1 = pow(P1.x, 2) - pow(P3.x, 2), f2 = pow(P3.y, 2) - pow(P1.y, 2);
	float f = (f1 - f2) / 2;

	if (a == 0 || b == 0 || c == 0 || d == 0) return circleInf;
	if (a / b == c / d) return circleInf;


	float circleA = (d*e - b * f) / (a*d - c * b);
	float circleB = (a*f - c * e) / (a*d - c * b);
	float circleR = sqrt(pow((P1.x - circleA), 2) + pow((P1.y - circleB), 2));

	circleInf.A = circleA;
	circleInf.B = circleB;
	circleInf.C = circleR;
	circleInf.Info = true;
	return circleInf;
}


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
void
gen_Metrology_Model_circle(cv::Mat &inputPic, cv::Vec3i &mainCircle,
	int nPerpendicularTHreshold, int nTangetialThreshold,
	int nMeasureThreshold, std::vector<cv::Point> &stOutPoint, bool ore) {

	if (inputPic.empty())
		return;

	cv::Point pCenter(mainCircle[0], mainCircle[1]);
	int nRadius = mainCircle[2];

	//cv::circle(inputPic, pCenter, nRadius, cv::Scalar(128, 128, 128));

	//生成圆上的点，并储存
	std::vector<cv::Point> stTmpGenCircle;
	stTmpGenCircle.clear(), stOutPoint.clear();

	cv::Mat temp;
	cvtColor(inputPic, temp, 8);

	//每隔20度进行旋转
	for (int nRStart = 0; nRStart < 360; nRStart += 10) {
		//cout << nRStart << "d" << endl;

		//生成圆上的点
		int nTmpPx = pCenter.x + nRadius * cos(nRStart * CV_PI / 180);
		int nTmpPy = pCenter.y + nRadius * sin(nRStart * CV_PI / 180);
		cv::Point pTmp = cv::Point(nTmpPx, nTmpPy);
		/*cv::drawMarker(inputPic, pTmp, cv::Scalar(255), cv::MARKER_CROSS);*/
		stTmpGenCircle.emplace_back(pTmp);

		cv::RotatedRect rRet = cv::RotatedRect(pTmp, cv::Size(nPerpendicularTHreshold * 2, nTangetialThreshold * 2), nRStart);

		cv::Point2f vertices[4];
		rRet.points(vertices);
		for (int i = 0; i < 4; i++)
			line(inputPic, vertices[i], vertices[(i + 1) % 4], cv::Scalar(255), 1);

		for (int i = 0; i < 4; i++)
			line(temp, vertices[i], vertices[(i + 1) % 4], cv::Scalar(255, 0, 0), 1);


		cv::Point pCircle = fromRotatedGetPoint(inputPic, rRet, nMeasureThreshold,ore);
		if (pCircle.x != 0 || pCircle.y != 0)
			stOutPoint.emplace_back(static_cast<cv::Point>(pCircle));

		/*cv::drawMarker(inputPic, pCircle, cv::Scalar(255), cv::MARKER_CROSS);*/

	}

}


/**
*@brief       : 获得圆信息
*@allPoints   ：输入的所有点
*@maxIterCnt  ：迭代次数
*@disTH       ：离散距离阈值
*@minScore    ：最低分值
*@inPoints    ：使用点集bn
*@fitCircle   ：拟合的圆
*@return      ：无
*/
void
ransc_fit_circle(cv::Mat &input, std::vector<cv::Point> &allPoints,
	int maxIterCnt, float disTH, float minScor,
	std::vector<cv::Point> & inPoints, circleInfo& fitCircle) {
	using namespace cv;
	int max_inP_num = 0, it = 0;
	while (it < maxIterCnt)
	{
		std::vector<cv::Point> tmpInPoints;
		tmpInPoints.clear();
		//步骤1：从数据中随机选择3组数据
		while (1) {
			cv::Point pRand = allPoints[rand() % allPoints.size()];
			tmpInPoints.emplace_back(pRand);
			if (tmpInPoints.size() == 3) break;
		}

		// 步骤2：通过获得的数据获得模型参数  
		circleInfo genCircleInf = genCicle(tmpInPoints);


		// Step3: 进行模型评估: 点到圆的距离
		if (genCircleInf.Info) {
			cv::Point2f pCenter = cv::Point2f(genCircleInf.A, genCircleInf.B);
			float fTmpRadius = genCircleInf.C;
			//所有点到圆心的距离
			for (size_t t = 0; t < allPoints.size(); t++) {
				cv::Point p = allPoints[t];

				float d = dDifCircle(p, pCenter, fTmpRadius);
				//std::cout << t << " : " << d << std::endl;
				if (abs(d) <= disTH) {
					tmpInPoints.push_back(p);
					//std::cout << t << std::endl;
				}
			}
			//std::cout << "" << std::endl;

			float fMinScore = (float)tmpInPoints.size() / (float)allPoints.size() * 100.0001;
			// Step4: 判断一致性: 满足一致性集合的最小元素个数条件 + 至少比上一次的好
			if (tmpInPoints.size() > max_inP_num && fMinScore > minScor) {
				//std::cout << "1" << std::endl;
				max_inP_num = tmpInPoints.size();
				fitCircle.Info = true;
				fitCircle.A = genCircleInf.A;
				fitCircle.B = genCircleInf.B;
				fitCircle.C = genCircleInf.C;

				inPoints = tmpInPoints;
			}
		}

		it++;
	}

}

