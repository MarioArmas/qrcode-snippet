using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace QrSnippet.Services
{
    /// <summary>
    /// Lets the user pick an image file and loads it as a WPF image.
    /// </summary>
    public class ImageFileService
    {
        private const string ImageFilter =
            "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp|All files|*.*";

        /// <summary>
        /// Shows the Open File dialog. Returns the selected path, or null if the user cancels.
        /// </summary>
        public string? PickImagePath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open an image containing a QR code",
                Filter = ImageFilter
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>
        /// Loads an image file. Returns null if the file is not a readable image.
        /// </summary>
        public BitmapSource? LoadImage(string path)
        {
            try
            {
                // OnLoad reads the whole file now, so it is not kept locked afterwards.
                using var stream = File.OpenRead(path);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                BitmapSource frame = decoder.Frames[0];
                frame.Freeze();
                return frame;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // FileFormatException derives from IOException.
                return null;
            }
        }
    }
}
