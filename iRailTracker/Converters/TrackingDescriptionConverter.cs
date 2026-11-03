using System.Globalization;

namespace iRailTracker.Converters
{
    /// <summary>Screen-reader text for the tracking bell, which is otherwise just an icon.</summary>
    public class TrackingDescriptionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? "Tracking this journey. Double-tap to stop tracking" : "Track this journey";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
