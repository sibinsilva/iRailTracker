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
        private const int MaxParallelRequests = 4;

        private readonly ConcurrentDictionary<(string TrainCode, DateTime Date), List<TrainMovement>> _routes = new();

        public static RouteFilterService Instance { get; } = new();

        public async Task<List<StationData>> FilterCallingAtAsync(
            Settings settings, IReadOnlyList<StationData> board, string fromCode, string toCode,
            StationService? stationService = null)
        {
            stationService ??= new StationService();
            PruneOldRoutes();
            using var throttle = new SemaphoreSlim(MaxParallelRequests);

            var checks = board.Select(async train =>
            {
                await throttle.WaitAsync();
                try
                {
                    var route = await GetRouteAsync(settings, train, stationService);
                    // If a route couldn't be fetched, keep the train rather than hide one that may be right
                    return (train, keep: route.Count == 0 || CallsAtAfter(route, fromCode, toCode));
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
        public static bool CallsAtAfter(IEnumerable<TrainMovement> route, string fromCode, string toCode)
        {
            var seenFrom = false;

            foreach (var stop in route.OrderBy(m => m.LocationOrder))
            {
                if (stop.LocationType == "T")
                    continue;

                var code = stop.LocationCode.Trim();

                if (!seenFrom)
                {
                    seenFrom = code.Equals(fromCode.Trim(), StringComparison.OrdinalIgnoreCase);
                }
                else if (code.Equals(toCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

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
