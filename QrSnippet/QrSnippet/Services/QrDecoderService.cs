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
        /// <summary>
        /// Returns the text encoded in the QR code, or null if none is found.
        /// Runs on a thread-pool thread because TryHarder on a large image can take seconds.
        /// </summary>
        public Task<string?> DecodeAsync(BitmapSource image)
        {
            ArgumentNullException.ThrowIfNull(image);

            if (!image.IsFrozen)
            {
                throw new ArgumentException("The image must be frozen to be read from a background thread.", nameof(image));
            }

            return Task.Run(() => Decode(image));
        }

        private static string? Decode(BitmapSource image)
        {
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
            return CreateReader().Decode(source)?.Text;
        }

        // A new reader per scan: readers are not thread-safe and two scans may overlap.
        private static BarcodeReaderGeneric CreateReader() => new()
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                TryHarder = true,
                TryInverted = true
            }
        };
    }
}
