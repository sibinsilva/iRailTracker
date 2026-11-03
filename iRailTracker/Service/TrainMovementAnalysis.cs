using System.Globalization;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    public static class TrainMovementAnalysis
    {
        private const string TimeFormat = "HH:mm:ss";
        private static readonly TimeSpan ZeroSentinel = TimeSpan.Zero;

        /// <summary>
        /// A later stop whose clock time is more than this far before the previous stop's is taken
        /// to be on the next day (the train ran past midnight).
        /// </summary>
        private static readonly TimeSpan MidnightRolloverThreshold = TimeSpan.FromHours(6);

        /// <summary>
        /// Returns the last stop in the route whose effective time has already passed (i.e. where the
        /// train currently is, or most recently was), falling back to the first named stop if none have
        /// passed yet. Blank-name timing points are skipped.
        /// </summary>
        /// <param name="serviceDate">The date the train's service started (the API's TrainDate).
        /// Defaults to <paramref name="now"/>'s date.</param>
        public static TrainMovement? DetermineCurrentStop(IEnumerable<TrainMovement> movements, DateTime now, DateTime? serviceDate = null)
        {
            TrainMovement? current = null;
            TrainMovement? firstNamed = null;

            foreach (var (movement, effective) in ResolveTimes(movements, serviceDate ?? now.Date))
            {
                if (string.IsNullOrWhiteSpace(movement.LocationFullName))
                    continue;

                firstNamed ??= movement;

                if (effective is { } at && at <= now)
                    current = movement;
            }

            return current ?? firstNamed;
        }

        public static string? DetermineCurrentLocation(IEnumerable<TrainMovement> movements, DateTime now, DateTime? serviceDate = null) =>
            DetermineCurrentStop(movements, now, serviceDate)?.LocationFullName;

        /// <summary>
        /// Returns the next stop (by route order) the train hasn't reached yet - i.e. one whose
        /// departure board would still list this train as due. Used to look up the train's official,
        /// live "Late" value from that station's board rather than approximating it from movement
        /// timestamps (which can disagree with Irish Rail's own figure).
        /// </summary>
        public static TrainMovement? DetermineNextStop(IEnumerable<TrainMovement> movements, DateTime now, DateTime? serviceDate = null) =>
            ResolveTimes(movements, serviceDate ?? now.Date)
                .Where(x => !string.IsNullOrWhiteSpace(x.Movement.LocationFullName) &&
                            !string.IsNullOrWhiteSpace(x.Movement.LocationCode))
                .FirstOrDefault(x => x.EffectiveTime is { } at && at > now)
                .Movement;

        /// <summary>
        /// True once the train's expected arrival at its destination is more than
        /// <paramref name="grace"/> in the past. Unknown arrival times never count as ended.
        /// </summary>
        public static bool HasJourneyEnded(IEnumerable<TrainMovement> movements, DateTime now, DateTime serviceDate, TimeSpan grace)
        {
            var destination = ResolveTimes(movements, serviceDate)
                .FirstOrDefault(x => x.Movement.LocationType == "D");

            // Irish Rail records the actual arrival once the train gets in: stop right away
            if (destination.Movement is not null && TryParseTime(destination.Movement.Arrival?.Trim() ?? "", out var actual) &&
                actual.TimeOfDay != ZeroSentinel)
            {
                return true;
            }

            return destination.EffectiveTime is { } arrival && now - arrival > grace;
        }

        /// <summary>
        /// Orders the stops by route order and turns each stop's clock time into a full date and time,
        /// starting on <paramref name="serviceDate"/> and moving to the next day whenever the clock
        /// time jumps backwards past midnight.
        /// </summary>
        public static List<(TrainMovement Movement, DateTime? EffectiveTime)> ResolveTimes(
            IEnumerable<TrainMovement> movements, DateTime serviceDate)
        {
            var result = new List<(TrainMovement, DateTime?)>();
            var dayOffset = 0;
            DateTime? previous = null;

            foreach (var movement in movements.OrderBy(m => m.LocationOrder))
            {
                if (GetEffectiveTime(movement) is not { } timeOfDay)
                {
                    result.Add((movement, null));
                    continue;
                }

                var at = serviceDate.Date.AddDays(dayOffset) + timeOfDay;
                if (previous is { } prev && at < prev - MidnightRolloverThreshold)
                {
                    dayOffset++;
                    at = at.AddDays(1);
                }

                previous = at;
                result.Add((movement, at));
            }

            return result;
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
