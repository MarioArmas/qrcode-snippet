using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;

namespace QrSnippet.Services
{
    /// <summary>
    /// Decodes QR codes from WPF images using ZXing.Net.
    /// </summary>
    public class QrDecoderService
    {
        private readonly BarcodeReaderGeneric _reader = new()
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                TryHarder = true,
                TryInverted = true
            }
        };

        /// <summary>
        /// Returns the text encoded in the QR code, or null if none is found.
        /// </summary>
        public string? Decode(BitmapSource image)
        {
            ArgumentNullException.ThrowIfNull(image);

            // Normalize to BGRA32 so the raw pixels map directly to a ZXing luminance source.
            BitmapSource bgra = image.Format == PixelFormats.Bgra32
                ? image
                : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);

            int width = bgra.PixelWidth;
            int height = bgra.PixelHeight;
            int stride = width * 4;
            var pixels = new byte[stride * height];
            bgra.CopyPixels(pixels, stride, 0);

            var source = new RGBLuminanceSource(pixels, width, height, RGBLuminanceSource.BitmapFormat.BGRA32);
            return _reader.Decode(source)?.Text;
        }
    }
}
