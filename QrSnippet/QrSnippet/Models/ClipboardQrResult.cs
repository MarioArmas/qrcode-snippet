using System.Windows.Media.Imaging;

namespace QrSnippet.Models
{
    public enum ClipboardQrStatus
    {
        Decoded,
        Empty,
        ContainsText,
        NoQrFound,
        ClipboardBusy
    }

    /// <summary>
    /// Outcome of reading a QR code from the clipboard. Text is set only when Status is Decoded;
    /// Image is set when the clipboard held an image (Decoded or NoQrFound).
    /// </summary>
    public record ClipboardQrResult(ClipboardQrStatus Status, string? Text = null, BitmapSource? Image = null);
}
