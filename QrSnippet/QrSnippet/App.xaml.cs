using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace QrSnippet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Uncomment to force the app to use English for all UI text and formatting, regardless of the user's culture.
            // CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en");

            // WPF bindings format dates and numbers as en-US by default; use the user's culture instead.
            // UI text follows CultureInfo.CurrentUICulture through Properties/Strings*.resx.
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);
        }
    }

}
