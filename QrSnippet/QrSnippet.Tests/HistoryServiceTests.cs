using System.IO;
using QrSnippet.Models;
using QrSnippet.Services;

namespace QrSnippet.Tests
{
    public sealed class HistoryServiceTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "QrSnippetTests", Guid.NewGuid().ToString("N"));
        private readonly string _filePath;
        private readonly HistoryService _service;

        public HistoryServiceTests()
        {
            _filePath = Path.Combine(_directory, "history.json");
            _service = new HistoryService(_filePath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [Fact]
        public void DefaultFilePath_IsInLocalAppData()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            Assert.StartsWith(localAppData, HistoryService.DefaultFilePath);
            Assert.EndsWith(".json", HistoryService.DefaultFilePath);
        }

        [Fact]
        public void Load_ReturnsEmptyWhenFileIsMissing()
        {
            Assert.Empty(_service.Load());
        }

        [Fact]
        public void Load_ReturnsEmptyWhenFileIsCorrupt()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_filePath, "{ not json");

            Assert.Empty(_service.Load());
        }

        [Fact]
        public void Save_CreatesFolderAndRoundTripsEntries()
        {
            var entries = new List<HistoryEntry>
            {
                new("https://example.com/", new DateTimeOffset(2026, 11, 20, 10, 30, 0, TimeSpan.FromHours(-3))),
                new("Hola, ¿qué tal? Ñandú 🚀", new DateTimeOffset(2026, 11, 19, 8, 0, 0, TimeSpan.Zero))
            };

            Assert.True(_service.Save(entries));
            Assert.Equal(entries, _service.Load());
        }

        [Fact]
        public void AddEntry_PutsNewestFirstAndRemovesDuplicates()
        {
            var entries = new List<HistoryEntry>();
            var now = DateTimeOffset.Now;

            HistoryService.AddEntry(entries, new HistoryEntry("a", now));
            HistoryService.AddEntry(entries, new HistoryEntry("b", now.AddMinutes(1)));
            HistoryService.AddEntry(entries, new HistoryEntry("a", now.AddMinutes(2)));

            Assert.Equal(["a", "b"], entries.Select(e => e.Text));
            Assert.Equal(now.AddMinutes(2), entries[0].ScannedAt);
        }

        [Fact]
        public void AddEntry_KeepsAtMostMaxEntries()
        {
            var entries = new List<HistoryEntry>();

            for (int i = 0; i < HistoryService.MaxEntries + 5; i++)
            {
                HistoryService.AddEntry(entries, new HistoryEntry($"item {i}", DateTimeOffset.Now));
            }

            Assert.Equal(HistoryService.MaxEntries, entries.Count);
            Assert.Equal($"item {HistoryService.MaxEntries + 4}", entries[0].Text);
        }

        [Fact]
        public void IsLink_DetectsWebUrls()
        {
            Assert.True(new HistoryEntry("https://example.com", DateTimeOffset.Now).IsLink);
            Assert.False(new HistoryEntry("plain text", DateTimeOffset.Now).IsLink);
        }
    }
}
