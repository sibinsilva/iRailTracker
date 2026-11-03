using BruTile;
using BruTile.Cache;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;

namespace iRailTracker.Service
{
    /// <summary>
    /// Creates the Live Status map's base layer: OpenStreetMap tiles in light mode, CARTO's dark
    /// tiles in dark mode. Tiles are cached on the device for 30 days. OpenStreetMap's tile usage
    /// policy expects apps to cache, which also makes reopening the map fast and partly offline.
    /// </summary>
    public static class MapTiles
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(30);

        public static string UserAgent { get; } =
            $"iRailTracker/{AppInfo.Current.VersionString} ({AppInfo.Current.PackageName}; contact: sibinsilva@gmail.com)";

        public static TileLayer CreateLayer(bool dark)
        {
            var source = dark ? CreateDarkSource() : CreateOsmSource();
            return new TileLayer(source) { Name = dark ? "CartoDark" : "OpenStreetMap" };
        }

        private static HttpTileSource CreateOsmSource() => new(
            new GlobalSphericalMercator(),
            "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
            name: "OpenStreetMap",
            persistentCache: CreateCache("osm"),
            attribution: new Attribution("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright"),
            configureHttpRequestMessage: AddUserAgent);

        private static HttpTileSource CreateDarkSource() => new(
            new GlobalSphericalMercator(),
            "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}.png",
            serverNodes: ["a", "b", "c", "d"],
            name: "CartoDark",
            persistentCache: CreateCache("carto-dark"),
            attribution: new Attribution("© OpenStreetMap contributors © CARTO", "https://carto.com/attributions"),
            configureHttpRequestMessage: AddUserAgent);

        private static void AddUserAgent(HttpRequestMessage request) =>
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        private static FileCache CreateCache(string name) =>
            new(Path.Combine(FileSystem.Current.CacheDirectory, "tiles", name), "png", CacheDuration);
    }
}
