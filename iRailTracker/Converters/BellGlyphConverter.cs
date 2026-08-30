using System;
using System.Globalization;

namespace iRailTracker.Converters
{
    public class BellGlyphConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? "🔔" : "🔕";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
