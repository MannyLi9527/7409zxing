#pragma once
#include <opencv2/opencv.hpp>
#include <string>

using namespace std;

/**
 * @brief 识别Mat图像中的二维码内容
 * @param image 输入的OpenCV Mat图像（支持BGR或灰度图）
 * @return 识别到的二维码字符串，若未识别到则返回空字符串
 */
string detectQRCodeTest(const cv::Mat& image);