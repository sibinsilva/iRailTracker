using System;
using System.Globalization;

namespace iRailTracker.Converters
{
    public class BellGlyphConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            // Same bell both ways: a faded bell reads as "off", the crossed-out 🔕 looked like an error
            => "🔔";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
