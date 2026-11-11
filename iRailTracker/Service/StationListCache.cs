using System.Globalization;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    /// <summary>
    /// Keeps a copy of the station list on the device, so the app starts instantly and still works
    /// when it's opened without a network connection. Stations rarely change; the copy is refreshed
    /// in the background on each start.
    /// </summary>
    public static class StationListCache
    {
        private const string FileName = "stations.tsv";

        private static string FilePath => Path.Combine(FileSystem.Current.AppDataDirectory, FileName);

        public static List<Station> Load()
        {
            try
            {
                // Repair here too: caches saved by older versions still hold the API's bad coordinates
                return File.Exists(FilePath) ? StationCoordinates.Repair(Parse(File.ReadAllText(FilePath))) : [];
            }
            catch (IOException)
            {
                return [];
            }
        }

        public static void Save(IReadOnlyList<Station> stations)
        {
            if (stations.Count == 0)
                return;

            try
            {
                // Write then rename, so a crash mid-write can't leave a truncated cache
                var tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, Serialize(stations));
                File.Move(tempPath, FilePath, overwrite: true);
            }
            catch (IOException)
            {
                // Caching is best-effort
            }
        }

        public static string Serialize(IEnumerable<Station> stations) =>
            string.Join('\n', stations.Select(s => string.Join('\t',
                Clean(s.StationDesc), Clean(s.StationAlias), Clean(s.StationCode),
                s.StationLatitude.ToString("R", CultureInfo.InvariantCulture),
                s.StationLongitude.ToString("R", CultureInfo.InvariantCulture),
                s.StationId.ToString(CultureInfo.InvariantCulture))));

        public static List<Station> Parse(string text)
        {
            var stations = new List<Station>();

            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var f = line.Split('\t');
                if (f.Length != 6 ||
                    !double.TryParse(f[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(f[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) ||
                    !int.TryParse(f[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    continue;
                }

                stations.Add(new Station
                {
                    StationDesc = f[0],
                    StationAlias = f[1],
                    StationCode = f[2],
                    StationLatitude = lat,
                    StationLongitude = lon,
                    StationId = id
                });
            }

            return stations;
        }

        private static string Clean(string value) => value.Replace('\t', ' ').Replace('\n', ' ').Replace("\r", string.Empty);
    }
}
