using System;
using System.Globalization;

namespace iRailTracker.Converters
{
    public class BellGlyphConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            // Material Icons: filled "notifications_active" when tracking, outline "notifications_none"
            // when not (the old crossed-out 🔕 emoji looked like an error)
            => value is true ? "\ue7f7" : "\ue7f5";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
