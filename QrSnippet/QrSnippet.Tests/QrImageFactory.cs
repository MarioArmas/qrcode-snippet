using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.QrCode;

namespace QrSnippet.Tests
{
    /// <summary>
    /// Builds QR code images in memory so the tests don't depend on image files.
    /// </summary>
    internal static class QrImageFactory
    {
        private const double Dpi = 96;

        /// <summary>
        /// A frozen BGRA32 image of a QR code encoding the text.
        /// </summary>
        public static BitmapSource Create(string text, int size = 300)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = size,
                    Height = size,
                    Margin = 2,
                    CharacterSet = "UTF-8"
                }
            };

            var data = writer.Write(text);
            return FromPixels(data.Pixels, data.Width, data.Height);
        }

        /// <summary>
        /// The image converted to another pixel format, e.g. Bgr24 for JPEG-like images.
        /// </summary>
        public static BitmapSource Convert(BitmapSource image, PixelFormat format)
        {
            var converted = new FormatConvertedBitmap(image, format, null, 0);
            converted.Freeze();
            return converted;
        }

        /// <summary>
        /// Light modules on a dark background, as in dark-mode screenshots.
        /// </summary>
        public static BitmapSource Invert(BitmapSource image)
        {
            byte[] pixels = GetPixels(image);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)(255 - pixels[i]);
                pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                pixels[i + 2] = (byte)(255 - pixels[i + 2]);
            }

            return FromPixels(pixels, image.PixelWidth, image.PixelHeight);
        }

        /// <summary>
        /// The image placed on a large white canvas, like a QR code in a full-screen capture.
        /// </summary>
        public static BitmapSource PlaceOnCanvas(BitmapSource image, int width, int height, int x, int y)
        {
            int stride = width * 4;
            var canvas = new byte[stride * height];
            Array.Fill(canvas, (byte)255);

            byte[] pixels = GetPixels(image);
            int rowBytes = image.PixelWidth * 4;
            for (int row = 0; row < image.PixelHeight; row++)
            {
                Buffer.BlockCopy(pixels, row * rowBytes, canvas, (y + row) * stride + x * 4, rowBytes);
            }

            return FromPixels(canvas, width, height);
        }

        /// <summary>
        /// A frozen image filled with one color and no QR code.
        /// </summary>
        public static BitmapSource Blank(int width, int height)
        {
            var pixels = new byte[width * height * 4];
            Array.Fill(pixels, (byte)255);
            return FromPixels(pixels, width, height);
        }

        private static byte[] GetPixels(BitmapSource image)
        {
            BitmapSource bgra = image.Format == PixelFormats.Bgra32 ? image : Convert(image, PixelFormats.Bgra32);
            int stride = bgra.PixelWidth * 4;
            var pixels = new byte[stride * bgra.PixelHeight];
            bgra.CopyPixels(pixels, stride, 0);
            return pixels;
        }

        private static BitmapSource FromPixels(byte[] pixels, int width, int height)
        {
            var image = BitmapSource.Create(width, height, Dpi, Dpi, PixelFormats.Bgra32, null, pixels, width * 4);
            image.Freeze();
            return image;
        }
    }
}
