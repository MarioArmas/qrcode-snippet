using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QrSnippet.Converters
{
    /// <summary>
    /// Collapses an element when the bound value is null. Set Invert to show it only when null.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool visible = (value is not null) ^ Invert;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
