using System.IO;
using System.Text.Json;
using QrSnippet.Models;

namespace QrSnippet.Services
{
    /// <summary>
    /// Persists the recent scans history as JSON, by default in %LocalAppData%\QrSnippet\history.json.
    /// </summary>
    public class HistoryService
    {
        public const int MaxEntries = 50;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly string _filePath;

        public HistoryService()
            : this(DefaultFilePath)
        {
        }

        public HistoryService(string filePath)
        {
            _filePath = filePath;
        }

        public static string DefaultFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QrSnippet",
            "history.json");

        /// <summary>
        /// Reads the saved history, newest first. Returns an empty list if the file is missing or unreadable.
        /// </summary>
        public List<HistoryEntry> Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return [];
                }

                using FileStream stream = File.OpenRead(_filePath);
                var entries = JsonSerializer.Deserialize<List<HistoryEntry>>(stream, JsonOptions) ?? [];

                // Ignore entries a hand-edited or partially written file may contain without text.
                return entries
                    .Where(e => !string.IsNullOrEmpty(e?.Text))
                    .Take(MaxEntries)
                    .ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                return [];
            }
        }

        /// <summary>
        /// Writes the history to disk. Returns false if the file could not be written.
        /// </summary>
        public bool Save(IEnumerable<HistoryEntry> entries)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                // Write to a temporary file first so a crash mid-write cannot corrupt the existing history.
                string tempPath = _filePath + ".tmp";
                using (FileStream stream = File.Create(tempPath))
                {
                    JsonSerializer.Serialize(stream, entries.Take(MaxEntries).ToList(), JsonOptions);
                }

                File.Move(tempPath, _filePath, overwrite: true);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Puts the entry at the top of the list, removing an older entry with the same text
        /// and dropping the oldest entries beyond <see cref="MaxEntries"/>.
        /// </summary>
        public static void AddEntry(IList<HistoryEntry> entries, HistoryEntry entry)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Text == entry.Text)
                {
                    entries.RemoveAt(i);
                }
            }

            entries.Insert(0, entry);

            while (entries.Count > MaxEntries)
            {
                entries.RemoveAt(entries.Count - 1);
            }
        }
    }
}
