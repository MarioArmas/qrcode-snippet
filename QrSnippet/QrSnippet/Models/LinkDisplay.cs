namespace QrSnippet.Models
{
    /// <summary>
    /// A URL split so the host can be highlighted: Prefix + Host + Rest is the full address.
    /// </summary>
    public record LinkDisplay(string Prefix, string Host, string Rest);
}
