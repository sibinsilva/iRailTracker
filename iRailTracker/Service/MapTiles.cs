using BruTile;
using BruTile.Cache;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;

namespace iRailTracker.Service
{
    /// <summary>
    /// Creates the Live Status map's base layer: OpenStreetMap tiles, cached on the device for 30
    /// days. OpenStreetMap's tile usage policy expects apps to cache, which also makes reopening the
    /// map fast and partly offline.
    /// CARTO's dark tiles were used in dark mode, but CARTO now requires an API key and serves an
    /// "API KEY REQUIRED" placeholder without one; every keyless dark style has the same problem.
    /// </summary>
    public static class MapTiles
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(30);

        public static string UserAgent { get; } =
            $"iRailTracker/{AppInfo.Current.VersionString} ({AppInfo.Current.PackageName}; contact: sibinsilva@gmail.com)";

        public static TileLayer CreateLayer()
        {
            RemoveOldCartoCache();
            return new TileLayer(CreateOsmSource()) { Name = "OpenStreetMap" };
        }

        /// <summary>Deletes the cached "API KEY REQUIRED" placeholders from the CARTO dark tiles.</summary>
        private static void RemoveOldCartoCache()
        {
            try
            {
                var dir = Path.Combine(FileSystem.Current.CacheDirectory, "tiles", "carto-dark");
                if (System.IO.Directory.Exists(dir))
                    System.IO.Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private static HttpTileSource CreateOsmSource() => new(
            new GlobalSphericalMercator(),
            "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
            name: "OpenStreetMap",
            persistentCache: CreateCache("osm"),
            attribution: new Attribution("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright"),
            configureHttpRequestMessage: AddUserAgent);

        private static void AddUserAgent(HttpRequestMessage request) =>
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        private static FileCache CreateCache(string name) =>
            new(Path.Combine(FileSystem.Current.CacheDirectory, "tiles", name), "png", CacheDuration);
    }
}
