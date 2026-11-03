using System.Collections.Concurrent;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    /// <summary>
    /// Filters a station's departure board to trains that also stop at a chosen destination later on
    /// their route. A train's stop list doesn't change during the day, so each train's movements are
    /// fetched once per service date and reused by later refreshes.
    /// </summary>
    public class RouteFilterService
    {
        private const int MaxParallelRequests = 8;

        private readonly ConcurrentDictionary<(string TrainCode, DateTime Date), List<TrainMovement>> _routes = new();

        public static RouteFilterService Instance { get; } = new();

        /// <param name="from">The selected station; <paramref name="to"/> the destination. Stops are
        /// matched by code or name because Irish Rail's endpoints use different codes for some
        /// stations (Adamstown is ADAMS in the station list but ADMTN in train movements).</param>
        public async Task<List<StationData>> FilterCallingAtAsync(
            Settings settings, IReadOnlyList<StationData> board, Station from, Station to,
            StationService? stationService = null)
        {
            stationService ??= new StationService();
            PruneOldRoutes();
            using var throttle = new SemaphoreSlim(MaxParallelRequests);

            var checks = board.Select(async train =>
            {
                // Trains ending at the destination obviously go there: no route lookup needed
                if (SameName(train.Destination, to.StationDesc))
                    return (train, keep: true);

                await throttle.WaitAsync();
                try
                {
                    var route = await GetRouteAsync(settings, train, stationService);
                    // If a route couldn't be fetched, keep the train rather than hide one that may be right
                    return (train, keep: route.Count == 0 ||
                        CallsAtAfter(route, from.StationCode, to.StationCode, from.StationDesc, to.StationDesc));
                }
                finally
                {
                    throttle.Release();
                }
            });

            var results = await Task.WhenAll(checks);
            return results.Where(r => r.keep).Select(r => r.train).ToList();
        }

        /// <summary>
        /// True if the route stops at <paramref name="fromCode"/> and then, later on, stops at
        /// <paramref name="toCode"/>. Timing points the train passes without stopping (type "T") don't count.
        /// </summary>
        public static bool CallsAtAfter(IEnumerable<TrainMovement> route, string fromCode, string toCode,
            string? fromName = null, string? toName = null)
        {
            var seenFrom = false;

            foreach (var stop in route.OrderBy(m => m.LocationOrder))
            {
                if (stop.LocationType == "T")
                    continue;

                if (!seenFrom)
                {
                    seenFrom = IsStop(stop, fromCode, fromName);
                }
                else if (IsStop(stop, toCode, toName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStop(TrainMovement stop, string code, string? name) =>
            stop.LocationCode.Trim().Equals(code.Trim(), StringComparison.OrdinalIgnoreCase) ||
            (name is not null && SameName(stop.LocationFullName, name));

        private static bool SameName(string a, string b) =>
            a.Trim().Length > 0 && a.Trim().Equals(b.Trim(), StringComparison.OrdinalIgnoreCase);

        private void PruneOldRoutes()
        {
            var cutoff = IrishTime.Today.AddDays(-1);
            foreach (var key in _routes.Keys.Where(k => k.Date < cutoff))
                _routes.TryRemove(key, out _);
        }

        private async Task<List<TrainMovement>> GetRouteAsync(Settings settings, StationData train, StationService stationService)
        {
            var key = (train.Traincode.Trim(), IrishTime.ParseTrainDate(train.Traindate));

            if (_routes.TryGetValue(key, out var cached))
                return cached;

            var route = await stationService.GetTrainMovementsAsync(settings, key.Item1, key.Item2, null);

            // Don't cache failures, so the next refresh retries them
            if (route.Count > 0)
                _routes[key] = route;

            return route;
        }
    }
}
