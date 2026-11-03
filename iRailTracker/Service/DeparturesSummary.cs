using System.Diagnostics.CodeAnalysis;
using iRailTracker.Model;
using Microsoft.Extensions.Configuration;

namespace iRailTracker.Service
{
    /// <summary>
    /// Builds the short "next departures" text used by the home-screen widget. Lives in shared code
    /// (no Android types) so it can be unit tested; the widget only renders the result.
    /// </summary>
    public static class DeparturesSummary
    {
        private const string StationCodesKey = "widget_station_codes";

        public sealed record Summary(string Title, IReadOnlyList<string> Rows, string Footer);

        /// <summary>Formats up to <paramref name="maxRows"/> departures, soonest first.</summary>
        public static IReadOnlyList<string> FormatRows(IEnumerable<StationData> board, int maxRows = 3) =>
            board.OrderBy(t => t.Duein)
                 .Take(maxRows)
                 .Select(FormatRow)
                 .ToList();

        public static string FormatRow(StationData train)
        {
            var due = train.Duein <= 0 ? "Due" : $"{train.Duein} min";
            var status = train.Late switch
            {
                > 0 => $" · +{train.Late}",
                < 0 => $" · {train.Late}",
                _ => string.Empty
            };
            return $"{due} → {train.Destination.Trim()}{status}";
        }

        /// <summary>
        /// Loads the next departures for the user's first favourite station. Network problems are
        /// described in the returned summary rather than thrown.
        /// </summary>
        /// <param name="station">The widget's chosen station; empty means the first favourite.</param>
        public static async Task<Summary> LoadAsync(string? station = null, int maxRows = 3)
        {
            if (string.IsNullOrWhiteSpace(station))
                station = FavouriteStationsStore.Load().FirstOrDefault();
            if (string.IsNullOrEmpty(station))
                return new Summary("iRailTracker", ["Long-press to choose a station"], string.Empty);

            var settings = LoadEmbeddedSettings();
            if (settings is null)
                return new Summary(station, ["Couldn't read app settings"], string.Empty);

            var stationService = new StationService();
            var code = await GetStationCodeAsync(stationService, settings, station);
            if (code is null)
                return new Summary(station, ["Station list unavailable (offline?)"], UpdatedText());

            string? error = null;
            var board = await stationService.GetTrainServicesAsync(settings, code, e => error = e);

            if (error is not null)
                return new Summary(station, ["Couldn't load departures (offline?)"], UpdatedText());

            var rows = FormatRows(board, maxRows);
            return new Summary(station, rows.Count > 0 ? rows : ["No departures in the next 90 min"], UpdatedText());
        }

        private static string UpdatedText() => $"Updated {DateTime.Now:HH:mm}";

        /// <summary>
        /// Maps a station name to its code. The mapping is cached in Preferences, so the full station
        /// list is downloaded only the first time.
        /// </summary>
        private static async Task<string?> GetStationCodeAsync(StationService stationService, Settings settings, string stationName)
        {
            var cached = ParseStationCodes(Preferences.Get(StationCodesKey, string.Empty));
            if (cached.TryGetValue(stationName, out var code))
                return code;

            var stations = await stationService.GetAllStationsAsync(settings, null);
            if (stations.Count == 0)
                return null;

            var map = stations
                .Where(s => !string.IsNullOrWhiteSpace(s.StationDesc))
                .GroupBy(s => s.StationDesc.Trim())
                .ToDictionary(g => g.Key, g => g.First().StationCode.Trim());
            Preferences.Set(StationCodesKey, string.Join('\n', map.Select(kv => $"{kv.Key}\t{kv.Value}")));

            return map.GetValueOrDefault(stationName.Trim());
        }

        public static Dictionary<string, string> ParseStationCodes(string stored) =>
            stored.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                  .Select(line => line.Split('\t'))
                  .Where(parts => parts.Length == 2)
                  .GroupBy(parts => parts[0])
                  .ToDictionary(g => g.Key, g => g.First()[1]);

        /// <summary>Reads the Settings section of the embedded appsettings.json, as MauiProgram does.</summary>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Settings))]
        public static Settings? LoadEmbeddedSettings()
        {
            using var stream = typeof(DeparturesSummary).Assembly.GetManifestResourceStream("iRailTracker.appsettings.json");
            if (stream is null)
                return null;

            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            return config.GetSection("Settings").Get<Settings>();
        }
    }
}
