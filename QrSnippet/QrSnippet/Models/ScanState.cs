namespace QrSnippet.Models
{
    /// <summary>
    /// Outcome of the last scan, used to pick the result card's icon and color.
    /// </summary>
    public enum ScanState
    {
        Idle,
        Success,
        Warning,
        Error
    }
}
