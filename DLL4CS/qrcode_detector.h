#pragma once
#include <opencv2/opencv.hpp>
#include <string>

using namespace std;

/**
 * @brief ʶ��Matͼ���еĶ�ά������
 * @param image �����OpenCV Matͼ��֧��BGR��Ҷ�ͼ��
 * @return ʶ�𵽵Ķ�ά���ַ�������δʶ���򷵻ؿ��ַ���
 */
string detectQRCodeTest(const cv::Mat& image);