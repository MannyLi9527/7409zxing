using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;

namespace Vision.通用
{
    public class QrCodeReader
    {
        public static string ReadDataMatrix(WriteableBitmap writeableBitmap)
        {
            if (writeableBitmap == null)
                throw new ArgumentNullException(nameof(writeableBitmap));

            try
            {
                // 确保图像可跨线程访问
                if (!writeableBitmap.IsFrozen && writeableBitmap.CanFreeze)
                    writeableBitmap.Freeze();

                // 获取图像数据
                int width = writeableBitmap.PixelWidth;
                int height = writeableBitmap.PixelHeight;
                int stride = width * ((writeableBitmap.Format.BitsPerPixel + 7) / 8);
                byte[] pixels = new byte[height * stride];
                writeableBitmap.CopyPixels(pixels, stride, 0);

                // 配置 DataMatrix 专用解码器
                var reader = new BarcodeReader
                {
                    AutoRotate = true,
                    Options = new DecodingOptions
                    {
                        PossibleFormats = new[] { BarcodeFormat.DATA_MATRIX }, // 仅识别 DataMatrix
                        TryHarder = true,      // 启用更复杂的识别算法
                        PureBarcode = true,     // 假设图像中只有条码（无背景干扰）
                        UseCode39ExtendedMode = false,
                        UseCode39RelaxedExtendedMode = false
                    },
                    // 使用 DataMatrix 专用解码器（ZXing 内部实现）
                    //Reader = new DataMatrixReader()
                };

                // 根据图像格式选择正确的 BitmapFormat
                var bitmapFormat = writeableBitmap.Format == PixelFormats.Gray8
                    ? RGBLuminanceSource.BitmapFormat.Gray8
                    : RGBLuminanceSource.BitmapFormat.BGRA32;

                var luminanceSource = new RGBLuminanceSource(pixels, width, height, bitmapFormat);
                var result = reader.Decode(luminanceSource);

                return result?.Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DataMatrix 识别失败: {ex.Message}");
                return null;
            }
        }
    }
}