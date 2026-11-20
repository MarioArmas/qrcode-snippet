using System.Windows.Media;
using System.Windows.Media.Imaging;
using QrSnippet.Services;

namespace QrSnippet.Tests
{
    public class QrDecoderServiceTests
    {
        private readonly QrDecoderService _decoder = new();

        [Theory]
        [InlineData("https://example.com/path?q=1")]
        [InlineData("Plain text")]
        [InlineData("WIFI:T:WPA;S:MyNetwork;P:secret;;")]
        [InlineData("Hola, ¿qué tal? Ñandú 🚀")]
        public async Task DecodeAsync_ReturnsTheEncodedText(string text)
        {
            BitmapSource image = QrImageFactory.Create(text);

            Assert.Equal(text, await _decoder.DecodeAsync(image));
        }

        [Fact]
        public async Task DecodeAsync_ReadsLongContent()
        {
            string text = string.Concat(Enumerable.Repeat("0123456789abcdef", 50));
            BitmapSource image = QrImageFactory.Create(text, size: 600);

            Assert.Equal(text, await _decoder.DecodeAsync(image));
        }

        public static TheoryData<string> PixelFormatNames => ["Bgr24", "Bgr32", "Pbgra32", "Gray8"];

        [Theory]
        [MemberData(nameof(PixelFormatNames))]
        public async Task DecodeAsync_ConvertsOtherPixelFormats(string formatName)
        {
            var format = (PixelFormat)new PixelFormatConverter().ConvertFromString(formatName)!;
            BitmapSource image = QrImageFactory.Convert(QrImageFactory.Create("format test"), format);

            Assert.Equal("format test", await _decoder.DecodeAsync(image));
        }

        [Fact]
        public async Task DecodeAsync_ReadsInvertedQrCodes()
        {
            BitmapSource image = QrImageFactory.Invert(QrImageFactory.Create("inverted"));

            Assert.Equal("inverted", await _decoder.DecodeAsync(image));
        }

        [Fact]
        public async Task DecodeAsync_FindsSmallQrCodeInLargeScreenshot()
        {
            BitmapSource qr = QrImageFactory.Create("4k capture", size: 200);
            BitmapSource screenshot = QrImageFactory.PlaceOnCanvas(qr, 3840, 2160, x: 2900, y: 1500);

            Assert.Equal("4k capture", await _decoder.DecodeAsync(screenshot));
        }

        [Fact]
        public async Task DecodeAsync_ReturnsNullWhenThereIsNoQrCode()
        {
            Assert.Null(await _decoder.DecodeAsync(QrImageFactory.Blank(400, 300)));
        }

        [Fact]
        public async Task DecodeAsync_SupportsConcurrentScans()
        {
            string[] texts = Enumerable.Range(0, 8).Select(i => $"scan {i}").ToArray();
            BitmapSource[] images = texts.Select(text => QrImageFactory.Create(text)).ToArray();

            string?[] results = await Task.WhenAll(images.Select(_decoder.DecodeAsync));

            Assert.Equal(texts, results);
        }

        [Fact]
        public async Task DecodeAsync_RejectsUnfrozenImages()
        {
            var image = new WriteableBitmap(QrImageFactory.Create("unfrozen"));

            await Assert.ThrowsAsync<ArgumentException>(() => _decoder.DecodeAsync(image));
        }

        [Fact]
        public async Task DecodeAsync_RejectsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _decoder.DecodeAsync(null!));
        }
    }
}
