using QrSnippet.Models;
using QrSnippet.Services;

namespace QrSnippet.Tests
{
    public class LinkServiceTests
    {
        [Theory]
        [InlineData("https://example.com", "https://", "example.com", "/")]
        [InlineData("https://www.example.com/login?next=/home#top", "https://", "www.example.com", "/login?next=/home#top")]
        [InlineData("http://example.com:8080/a", "http://", "example.com", ":8080/a")]
        [InlineData("https://example.com:443/a", "https://", "example.com", "/a")]
        public void GetDisplayParts_SplitsAroundTheHost(string text, string prefix, string host, string rest)
        {
            Assert.True(LinkService.TryGetWebUrl(text, out Uri? url));

            LinkDisplay parts = LinkService.GetDisplayParts(url);

            Assert.Equal(new LinkDisplay(prefix, host, rest), parts);
        }

        [Fact]
        public void GetDisplayParts_ShowsLookAlikeUnicodeDomainsAsPunycode()
        {
            // The first letter is a Cyrillic "а", not a Latin "a".
            Assert.True(LinkService.TryGetWebUrl("https://аpple.com/", out Uri? url));

            LinkDisplay parts = LinkService.GetDisplayParts(url);

            Assert.StartsWith("xn--", parts.Host);
            Assert.DoesNotContain("apple.com", parts.Host);
        }

        [Theory]
        [InlineData("https://example.com", "https://example.com/")]
        [InlineData("http://example.com/path?q=1", "http://example.com/path?q=1")]
        [InlineData("https://example.com:8080/a#section", "https://example.com:8080/a#section")]
        [InlineData("HTTPS://EXAMPLE.COM/Path", "https://example.com/Path")]
        [InlineData("  https://example.com/a \r\n", "https://example.com/a")]
        public void TryGetWebUrl_AcceptsHttpAndHttpsUrls(string text, string expected)
        {
            bool result = LinkService.TryGetWebUrl(text, out Uri? url);

            Assert.True(result);
            Assert.Equal(expected, url!.AbsoluteUri);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("hello world")]
        [InlineData("example.com")]
        [InlineData("www.example.com/path")]
        [InlineData("https://")]
        [InlineData("https://exa mple.com")]
        [InlineData("WIFI:T:WPA;S:MyNetwork;P:secret;;")]
        public void TryGetWebUrl_RejectsTextThatIsNotAnAbsoluteUrl(string text)
        {
            Assert.False(LinkService.TryGetWebUrl(text, out Uri? url));
            Assert.Null(url);
        }

        [Theory]
        [InlineData("ftp://example.com/file.txt")]
        [InlineData("file:///C:/Windows/System32/cmd.exe")]
        [InlineData("javascript:alert(1)")]
        [InlineData("mailto:someone@example.com")]
        [InlineData("tel:+123456789")]
        public void TryGetWebUrl_RejectsNonWebSchemes(string text)
        {
            Assert.False(LinkService.TryGetWebUrl(text, out _));
        }

        [Theory]
        [InlineData("https://bank.com@evil.com")]
        [InlineData("https://user:password@example.com/")]
        public void TryGetWebUrl_RejectsUrlsWithUserInfo(string text)
        {
            Assert.False(LinkService.TryGetWebUrl(text, out _));
        }
    }
}
