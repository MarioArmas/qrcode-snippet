using System.Collections;
using System.Globalization;
using System.Resources;
using QrSnippet.Properties;

namespace QrSnippet.Tests
{
    public class LocalizationTests
    {
        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es");

        [Fact]
        public void EveryStringHasASpanishTranslation()
        {
            Dictionary<string, string> english = ReadStrings(CultureInfo.InvariantCulture);
            Dictionary<string, string> spanish = ReadStrings(Spanish);

            Assert.Empty(english.Keys.Except(spanish.Keys));
            Assert.Empty(spanish.Keys.Except(english.Keys));
            Assert.All(spanish, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Value), entry.Key));
        }

        [Fact]
        public void OpenDialogFilter_KeepsTheSamePatternsInEveryLanguage()
        {
            // Only the labels may be translated; the "|"-separated patterns must stay identical.
            string[] english = Strings.ResourceManager.GetString(nameof(Strings.OpenDialogFilter), CultureInfo.InvariantCulture)!.Split('|');
            string[] spanish = Strings.ResourceManager.GetString(nameof(Strings.OpenDialogFilter), Spanish)!.Split('|');

            Assert.Equal(english.Length, spanish.Length);
            for (int i = 1; i < english.Length; i += 2)
            {
                Assert.Equal(english[i], spanish[i]);
            }
        }

        [Fact]
        public void ResourceManager_ReturnsSpanishForSpanishCulture()
        {
            Assert.Equal("Pegar", Strings.ResourceManager.GetString(nameof(Strings.Paste), Spanish));
            Assert.Equal("Paste", Strings.ResourceManager.GetString(nameof(Strings.Paste), CultureInfo.GetCultureInfo("fr")));
        }

        // Only the entries defined for that exact culture, without falling back to the neutral ones.
        private static Dictionary<string, string> ReadStrings(CultureInfo culture)
        {
            ResourceSet set = Strings.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
            return set.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
        }
    }
}
