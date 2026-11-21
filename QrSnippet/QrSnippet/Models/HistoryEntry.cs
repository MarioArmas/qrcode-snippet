using System.Text.Json.Serialization;
using QrSnippet.Services;

namespace QrSnippet.Models
{
    /// <summary>
    /// A successfully decoded QR code kept in the recent scans history.
    /// </summary>
    public record HistoryEntry(string Text, DateTimeOffset ScannedAt)
    {
        [JsonIgnore]
        public bool IsLink => LinkService.TryGetWebUrl(Text, out _);
    }
}
