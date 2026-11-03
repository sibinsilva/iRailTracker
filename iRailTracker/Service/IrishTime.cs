using System.Globalization;

namespace iRailTracker.Service
{
    /// <summary>
    /// Irish Rail publishes all times in Irish local time. Comparing them with the device clock
    /// (DateTime.Now) breaks when the phone is set to another time zone, so all "now" comparisons
    /// against API times go through here.
    /// </summary>
    public static class IrishTime
    {
        private const string TrainDateFormat = "dd MMM yyyy";

        public static TimeZoneInfo Zone { get; } = ResolveZone();

        /// <summary>Overridable clock for tests.</summary>
        public static Func<DateTime> UtcNowProvider { get; set; } = () => DateTime.UtcNow;

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNowProvider(), Zone);

        public static DateTime Today => Now.Date;

        /// <summary>Formats a service date the way the Irish Rail API expects it, e.g. "03 Nov 2026".</summary>
        public static string FormatTrainDate(DateTime date) =>
            date.ToString(TrainDateFormat, CultureInfo.InvariantCulture);

        /// <summary>Parses the API's Traindate field (e.g. "03 Nov 2026"); falls back to today in Ireland.</summary>
        public static DateTime ParseTrainDate(string? value) =>
            DateTime.TryParseExact(value?.Trim(), TrainDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date)
                ? date.Date
                : Today;

        private static TimeZoneInfo ResolveZone()
        {
            foreach (var id in new[] { "Europe/Dublin", "GMT Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Local;
        }
    }
}
