using System.Globalization;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    public static class TrainMovementAnalysis
    {
        private const string TimeFormat = "HH:mm:ss";
        private static readonly TimeSpan ZeroSentinel = TimeSpan.Zero;

        public static bool TryComputeDelayMinutes(TrainMovement destinationStop, out int minutes)
        {
            minutes = 0;

            if (!TryParseTime(destinationStop.ScheduledArrival, out var scheduled) ||
                !TryParseTime(destinationStop.ExpectedArrival, out var expected))
            {
                return false;
            }

            var diff = (expected - scheduled).TotalMinutes;

            if (diff > 720)
                diff -= 1440;
            else if (diff < -720)
                diff += 1440;

            minutes = (int)Math.Round(diff);
            return true;
        }

        public static string? DetermineCurrentLocation(IEnumerable<TrainMovement> movements, DateTime now)
        {
            var nowTime = now.TimeOfDay;
            string? current = null;
            string? firstNamed = null;

            foreach (var movement in movements.OrderBy(m => m.LocationOrder))
            {
                if (string.IsNullOrWhiteSpace(movement.LocationFullName))
                    continue;

                firstNamed ??= movement.LocationFullName;

                var effectiveTime = GetEffectiveTime(movement);
                if (effectiveTime is { } effective && effective <= nowTime)
                    current = movement.LocationFullName;
            }

            return current ?? firstNamed;
        }

        private static TimeSpan? GetEffectiveTime(TrainMovement movement)
        {
            if (TryParseTime(movement.ExpectedArrival, out var arrival) && arrival.TimeOfDay != ZeroSentinel)
                return arrival.TimeOfDay;

            if (TryParseTime(movement.ExpectedDeparture, out var departure) && departure.TimeOfDay != ZeroSentinel)
                return departure.TimeOfDay;

            return null;
        }

        private static bool TryParseTime(string value, out DateTime time) =>
            DateTime.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }
}
