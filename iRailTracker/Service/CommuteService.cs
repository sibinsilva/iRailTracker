using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    /// <summary>
    /// A saved commute: when a train from <see cref="From"/> that stops at <see cref="To"/> is due to
    /// leave within the time window on one of the chosen days and is late by at least
    /// <see cref="ThresholdMinutes"/>, the user gets a notification without tracking it by hand.
    /// </summary>
    public sealed record CommutePreset(
        bool Enabled,
        string From,
        string To,
        TimeSpan WindowStart,
        TimeSpan WindowEnd,
        DayOfWeek[] Days,
        int ThresholdMinutes)
    {
        public static CommutePreset Default { get; } = new(
            false, string.Empty, string.Empty,
            new TimeSpan(7, 30, 0), new TimeSpan(9, 0, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
            5);

        public bool IsComplete => !string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(To);

        public bool IsActiveAt(DateTime irishNow) =>
            Enabled && IsComplete && Days.Contains(irishNow.DayOfWeek) &&
            irishNow.TimeOfDay >= WindowStart - TimeSpan.FromMinutes(30) && // warn a little ahead
            irishNow.TimeOfDay <= WindowEnd;
    }

    /// <summary>A late train worth telling the user about.</summary>
    public sealed record CommuteAlert(string Key, string Title, string Message);

    public static class CommuteService
    {
        private const string PresetKey = "commute_preset";
        private const string NotifiedKey = "commute_notified";

        public static CommutePreset Load()
        {
            var json = SafePreferences.Get(PresetKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return CommutePreset.Default;

            try
            {
                return JsonSerializer.Deserialize(json, CommuteJsonContext.Default.CommutePreset) ?? CommutePreset.Default;
            }
            catch (JsonException)
            {
                return CommutePreset.Default;
            }
        }

        public static void Save(CommutePreset preset)
        {
            SafePreferences.Set(PresetKey, JsonSerializer.Serialize(preset, CommuteJsonContext.Default.CommutePreset));
#if ANDROID
            CommuteScheduler.Update(preset);
#endif
        }

        /// <summary>
        /// Picks the late trains to alert about from a board already filtered to trains that stop
        /// at the destination: scheduled to leave inside the window, late by at least the threshold,
        /// and not alerted before at this (or a higher) delay. Pure, for unit tests.
        /// </summary>
        public static List<CommuteAlert> FindAlerts(CommutePreset preset, IEnumerable<StationData> board,
            DateTime irishNow, IReadOnlySet<string> alreadyNotified)
        {
            var alerts = new List<CommuteAlert>();

            foreach (var train in board)
            {
                if (!TryParseTime(train.Schdepart, out var scheduled) &&
                    !TryParseTime(train.Scharrival, out scheduled))
                    continue;

                if (scheduled < preset.WindowStart || scheduled > preset.WindowEnd || train.Late < preset.ThresholdMinutes)
                    continue;

                // Alert again only if the delay grows by another threshold step
                var step = train.Late / Math.Max(preset.ThresholdMinutes, 1);
                var key = $"{irishNow:yyyyMMdd}|{train.Traincode.Trim()}|{step}";
                if (alreadyNotified.Contains(key))
                    continue;

                var time = DateTime.Today.Add(scheduled).ToString("HH:mm", CultureInfo.InvariantCulture);
                alerts.Add(new CommuteAlert(
                    key,
                    $"Your {time} to {preset.To} is {train.Late} min late",
                    $"{train.Origin.Trim()} → {train.Destination.Trim()} · expected {train.Expdepart.Trim()} from {preset.From}"));
            }

            return alerts;
        }

        /// <summary>Runs one background check: fetch, filter, notify. Called by the WorkManager job.</summary>
        public static async Task CheckAsync(Settings settings, Func<CommuteAlert, Task> notify)
        {
            var preset = Load();
            var now = IrishTime.Now;
            if (!preset.IsActiveAt(now))
                return;

            var stations = StationListCache.Load();
            var from = stations.FirstOrDefault(s => s.StationDesc.Trim().Equals(preset.From, StringComparison.OrdinalIgnoreCase));
            var to = stations.FirstOrDefault(s => s.StationDesc.Trim().Equals(preset.To, StringComparison.OrdinalIgnoreCase));
            if (from is null || to is null)
                return;

            var stationService = new StationService();
            string? error = null;
            var board = await stationService.GetTrainServicesAsync(settings, from.StationCode, e => error = e);
            if (error is not null || board.Count == 0)
                return;

            var calling = await RouteFilterService.Instance.FilterCallingAtAsync(settings, board, from, to, stationService);

            var notified = LoadNotified(now);
            foreach (var alert in FindAlerts(preset, calling, now, notified))
            {
                await notify(alert);
                notified.Add(alert.Key);
            }
            SaveNotified(notified);
        }

        private static HashSet<string> LoadNotified(DateTime now)
        {
            // Only today's keys are kept
            var today = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            return SafePreferences.Get(NotifiedKey, string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(k => k.StartsWith(today, StringComparison.Ordinal))
                .ToHashSet();
        }

        private static void SaveNotified(HashSet<string> keys) =>
            SafePreferences.Set(NotifiedKey, string.Join('\n', keys));

        private static bool TryParseTime(string? value, out TimeSpan time)
        {
            time = default;
            if (!DateTime.TryParseExact(value?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ||
                t.TimeOfDay == TimeSpan.Zero)
                return false;
            time = t.TimeOfDay;
            return true;
        }
    }

    [JsonSerializable(typeof(CommutePreset))]
    internal partial class CommuteJsonContext : JsonSerializerContext
    {
    }
}
