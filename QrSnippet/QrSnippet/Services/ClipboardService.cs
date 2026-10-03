using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using QrSnippet.Models;

namespace QrSnippet.Services
{
    /// <summary>
    /// Reads an image from the clipboard and decodes the QR code it contains.
    /// Must be called from the UI (STA) thread.
    /// </summary>
    public class ClipboardService
    {
        private const int MaxAttempts = 5;
        private const int RetryDelayMs = 50;

        private readonly QrDecoderService _decoder;

        public ClipboardService(QrDecoderService decoder)
        {
            _decoder = decoder;
        }

        public ClipboardQrResult ReadQr()
        {
            BitmapSource? image;
            bool containsText;

            try
            {
                (image, containsText) = WithRetry(() => (GetImage(), Clipboard.ContainsText()));
            }
            catch (COMException)
            {
                return new ClipboardQrResult(ClipboardQrStatus.ClipboardBusy);
            }

            // An image takes priority: browsers and Office often put text alongside a copied image.
            if (image is null)
            {
                return new ClipboardQrResult(containsText ? ClipboardQrStatus.ContainsText : ClipboardQrStatus.Empty);
            }

            string? text = _decoder.Decode(image);
            return text is null
                ? new ClipboardQrResult(ClipboardQrStatus.NoQrFound)
                : new ClipboardQrResult(ClipboardQrStatus.Decoded, text);
        }

        /// <summary>
        /// Copies text to the clipboard. Returns false if the clipboard stayed busy.
        /// </summary>
        public bool SetText(string text)
        {
            try
            {
                WithRetry(() =>
                {
                    Clipboard.SetText(text);
                    return true;
                });
                return true;
            }
            catch (COMException)
            {
                return false;
            }
        }

        private static BitmapSource? GetImage()
        {
            // Prefer the PNG format when present (Snipping Tool, browsers): WPF's own
            // Clipboard.GetImage() reads the DIB format and can return a corrupted bitmap.
            if (Clipboard.ContainsData("PNG") && Clipboard.GetData("PNG") is MemoryStream png)
            {
                try
                {
                    var decoder = BitmapDecoder.Create(png, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    BitmapSource frame = decoder.Frames[0];
                    frame.Freeze();
                    return frame;
                }
                catch (Exception ex) when (ex is NotSupportedException or FileFormatException)
                {
                    // Fall back to the standard bitmap format below.
                }
            }

            return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
        }

        // The clipboard is a shared resource; another process may hold it open briefly.
        private static T WithRetry<T>(Func<T> action)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return action();
                }
                catch (COMException) when (attempt < MaxAttempts)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
        }
    }
}
