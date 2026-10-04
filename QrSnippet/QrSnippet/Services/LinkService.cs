using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace QrSnippet.Services
{
    /// <summary>
    /// Detects web links in decoded QR text and opens them in the default browser.
    /// </summary>
    public class LinkService
    {
        /// <summary>
        /// True if the text is a well-formed absolute http or https URL.
        /// </summary>
        public static bool TryGetWebUrl(string text, [NotNullWhen(true)] out Uri? url)
        {
            url = null;
            string candidate = text.Trim();

            if (!Uri.IsWellFormedUriString(candidate, UriKind.Absolute)
                || !Uri.TryCreate(candidate, UriKind.Absolute, out Uri? parsed))
            {
                return false;
            }

            bool isWeb = parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps;

            // "https://bank.com@evil.com" actually goes to evil.com; reject URLs with user info to avoid that trick.
            if (!isWeb || string.IsNullOrEmpty(parsed.Host) || !string.IsNullOrEmpty(parsed.UserInfo))
            {
                return false;
            }

            url = parsed;
            return true;
        }

        /// <summary>
        /// Opens the URL in the default browser. Returns false if it could not be launched.
        /// </summary>
        public bool Open(Uri url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }
    }
}
