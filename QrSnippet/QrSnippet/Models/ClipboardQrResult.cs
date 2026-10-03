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
    /// Outcome of reading a QR code from the clipboard. Text is set only when Status is Decoded.
    /// </summary>
    public record ClipboardQrResult(ClipboardQrStatus Status, string? Text = null);
}
