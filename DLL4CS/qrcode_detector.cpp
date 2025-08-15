#include "pch.h"
#include "qrcode_detector.h"
#include <opencv2/opencv.hpp>
#include <ReadBarcode.h>
#include <TextUtfEncoding.h>
#include <BarcodeFormat.h>

using namespace ZXing;

std::string detectQRCodeTest(const cv::Mat& mat) {
	try {
		// �� OpenCV Mat ת��Ϊ ZXing ��ʶ��ĸ�ʽ
		auto binarizer = [](const cv::Mat& mat) {
			cv::Mat gray, binary;
			if (mat.channels() == 3)
				cv::cvtColor(mat, gray, cv::COLOR_BGR2GRAY);
			else if (mat.channels() == 4)
				cv::cvtColor(mat, gray, cv::COLOR_BGRA2GRAY);
			else
				gray = mat;

			// ����Ӧ��ֵ����������ʶ����
			cv::adaptiveThreshold(gray, binary, 255, cv::ADAPTIVE_THRESH_GAUSSIAN_C,
				cv::THRESH_BINARY, 51, 10);
			return binary;
		};

		cv::Mat binary = binarizer(mat);

		// ���� ZXing ͼ�����
		ImageView image{
			binary.data,
			binary.cols,
			binary.rows,
			ImageFormat::Lum,
			static_cast<int>(binary.step)
		};

		// ʹ�� ReaderOptions ��� DecodeHints
		ReaderOptions options;
		options.setTryHarder(true);          // ��Ŭ���ؽ���
		options.setFormats(BarcodeFormat::QRCode);  // ��ʶ���ά��

		// ����
		Result result = ReadBarcode(
			ImageView{ binary.data, binary.cols, binary.rows, ImageFormat::Lum, static_cast<int>(binary.step) },
			options
		);

		if (result.isValid()) {
			return result.text();  // ֱ�ӷ��ؽ����ı�
		}
		return "";
	}
	catch (const std::exception& e) {
		std::cerr << "ZXing Error: " << e.what() << std::endl;
		return "";
	}
}