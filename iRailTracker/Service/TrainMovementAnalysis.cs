using System.Globalization;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    public static class TrainMovementAnalysis
    {
        private const string TimeFormat = "HH:mm:ss";
        private static readonly TimeSpan ZeroSentinel = TimeSpan.Zero;

        /// <summary>
        /// Returns the last stop in the route whose effective time has already passed (i.e. where the
        /// train currently is, or most recently was), falling back to the first named stop if none have
        /// passed yet. Blank-name timing points are skipped.
        /// </summary>
        public static TrainMovement? DetermineCurrentStop(IEnumerable<TrainMovement> movements, DateTime now)
        {
            var nowTime = now.TimeOfDay;
            TrainMovement? current = null;
            TrainMovement? firstNamed = null;

            foreach (var movement in movements.OrderBy(m => m.LocationOrder))
            {
                if (string.IsNullOrWhiteSpace(movement.LocationFullName))
                    continue;

                firstNamed ??= movement;

                var effectiveTime = GetEffectiveTime(movement);
                if (effectiveTime is { } effective && effective <= nowTime)
                    current = movement;
            }

            return current ?? firstNamed;
        }

        public static string? DetermineCurrentLocation(IEnumerable<TrainMovement> movements, DateTime now) =>
            DetermineCurrentStop(movements, now)?.LocationFullName;

        /// <summary>
        /// Returns the next stop (by route order) the train hasn't reached yet - i.e. one whose
        /// departure board would still list this train as due. Used to look up the train's official,
        /// live "Late" value from that station's board rather than approximating it from movement
        /// timestamps (which can disagree with Irish Rail's own figure).
        /// </summary>
        public static TrainMovement? DetermineNextStop(IEnumerable<TrainMovement> movements, DateTime now)
        {
            var nowTime = now.TimeOfDay;

            return movements
                .Where(m => !string.IsNullOrWhiteSpace(m.LocationFullName) && !string.IsNullOrWhiteSpace(m.LocationCode))
                .OrderBy(m => m.LocationOrder)
                .FirstOrDefault(m => GetEffectiveTime(m) is { } effective && effective > nowTime);
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
