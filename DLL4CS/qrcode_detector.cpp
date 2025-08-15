#include "pch.h"
#include "qrcode_detector.h"
#include <opencv2/opencv.hpp>
#include <ReadBarcode.h>
#include <TextUtfEncoding.h>
#include <BarcodeFormat.h>

using namespace ZXing;

std::string detectQRCodeTest(const cv::Mat& mat) {
	try {
		// 将 OpenCV Mat 转换为 ZXing 可识别的格式
		auto binarizer = [](const cv::Mat& mat) {
			cv::Mat gray, binary;
			if (mat.channels() == 3)
				cv::cvtColor(mat, gray, cv::COLOR_BGR2GRAY);
			else if (mat.channels() == 4)
				cv::cvtColor(mat, gray, cv::COLOR_BGRA2GRAY);
			else
				gray = mat;

			// 自适应阈值处理可以提高识别率
			cv::adaptiveThreshold(gray, binary, 255, cv::ADAPTIVE_THRESH_GAUSSIAN_C,
				cv::THRESH_BINARY, 51, 10);
			return binary;
		};

		cv::Mat binary = binarizer(mat);

		// 创建 ZXing 图像对象
		ImageView image{
			binary.data,
			binary.cols,
			binary.rows,
			ImageFormat::Lum,
			static_cast<int>(binary.step)
		};

		// 使用 ReaderOptions 替代 DecodeHints
		ReaderOptions options;
		options.setTryHarder(true);          // 更努力地解码
		options.setFormats(BarcodeFormat::QRCode);  // 仅识别二维码

		// 解码
		Result result = ReadBarcode(
			ImageView{ binary.data, binary.cols, binary.rows, ImageFormat::Lum, static_cast<int>(binary.step) },
			options
		);

		if (result.isValid()) {
			return result.text();  // 直接返回解码文本
		}
		return "";
	}
	catch (const std::exception& e) {
		std::cerr << "ZXing Error: " << e.what() << std::endl;
		return "";
	}
}