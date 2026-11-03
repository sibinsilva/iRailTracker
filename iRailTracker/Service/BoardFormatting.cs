using System.Globalization;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    /// <summary>Pure helpers for turning departure-board data into what the cards show.</summary>
    public static class BoardFormatting
    {
        /// <summary>
        /// Finds the code of the station the user picked. Exact name first: the old "name contains"
        /// match could pick a different station whose name merely contains the chosen one. Falls back
        /// to the old behaviour (including "A &amp; B" names) for names that don't match exactly.
        /// </summary>
        public static string? FindStationCode(IEnumerable<Station> stations, string selectedName)
        {
            var name = selectedName.Trim();
            if (name.Length == 0)
                return null;

            var list = stations as IReadOnlyList<Station> ?? stations.ToList();

            var exact = list.FirstOrDefault(s => s.StationDesc.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                return exact.StationCode;

            var parts = name.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return list.FirstOrDefault(s => parts.Any(p => s.StationDesc.Contains(p, StringComparison.OrdinalIgnoreCase)))
                ?.StationCode;
        }

        /// <summary>
        /// The time a passenger at this station cares about: when the train leaves, or when it
        /// arrives if it terminates here (LocationType "D"). Bad or missing times never throw.
        /// </summary>
        public static (string Label, string Time) DescribeTime(StationData train)
        {
            var terminates = train.Locationtype.Trim() == "D";

            if (!terminates && TryFormat(train.Expdepart, out var departs))
                return ("Departs", departs);

            if (TryFormat(train.Exparrival, out var arrives))
                return ("Arrives", arrives);

            return terminates ? ("Arrives", "--") : ("Departs", "--");
        }

        private static bool TryFormat(string value, out string formatted)
        {
            formatted = string.Empty;

            // The API uses 00:00 for "no time" (e.g. no departure at the origin's arrival field)
            if (!DateTime.TryParseExact(value?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ||
                time.TimeOfDay == TimeSpan.Zero)
            {
                return false;
            }

            formatted = time.ToString("h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant();
            return true;
        }
    }
}
