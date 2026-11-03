using System.Xml.Serialization;
using iRailTracker.Model;

namespace iRailTracker.Service
{
    /// <summary>
    /// Saves the last departure board per station on the device, so opening the app offline still
    /// shows something (marked "Offline · showing times from …") instead of an empty screen.
    /// </summary>
    public static class BoardCache
    {
        private static readonly TimeSpan MaxAge = TimeSpan.FromHours(12);

        private static string? Directory
        {
            get
            {
                try
                {
                    return Path.Combine(FileSystem.Current.AppDataDirectory, "boards");
                }
                catch (NotImplementedException)
                {
                    return null; // unit tests
                }
            }
        }

        public static void Save(string stationCode, List<StationData> board)
        {
            if (Directory is not { } dir || string.IsNullOrEmpty(stationCode))
                return;

            try
            {
                System.IO.Directory.CreateDirectory(dir);
                using var stream = File.Create(Path.Combine(dir, $"{stationCode}.xml"));
                new XmlSerializer(typeof(TrainService)).Serialize(stream, new TrainService { ObjStationData = board });
            }
            catch (IOException)
            {
                // Best-effort cache
            }
        }

        public static bool TryLoad(string stationCode, out (DateTime fetchedAt, List<StationData> data) entry)
        {
            entry = default;
            if (Directory is not { } dir)
                return false;

            var path = Path.Combine(dir, $"{stationCode}.xml");
            try
            {
                if (!File.Exists(path))
                    return false;

                var savedAt = File.GetLastWriteTimeUtc(path);
                if (DateTime.UtcNow - savedAt > MaxAge)
                    return false;

                using var stream = File.OpenRead(path);
                if (new XmlSerializer(typeof(TrainService)).Deserialize(stream) is not TrainService board)
                    return false;

                entry = (savedAt, board.ObjStationData);
                return true;
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                return false;
            }
        }
    }
}
