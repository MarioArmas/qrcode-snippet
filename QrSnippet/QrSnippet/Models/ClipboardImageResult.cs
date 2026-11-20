using System.Windows.Media.Imaging;

namespace QrSnippet.Models
{
    public enum ClipboardImageStatus
    {
        HasImage,
        Empty,
        ContainsText,
        ClipboardBusy
    }

    /// <summary>
    /// Outcome of reading an image from the clipboard. Image is set (and frozen) only when Status is HasImage.
    /// </summary>
    public record ClipboardImageResult(ClipboardImageStatus Status, BitmapSource? Image = null);
}
