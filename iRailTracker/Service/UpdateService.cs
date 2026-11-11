using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace iRailTracker.Service
{
    /// <summary>A GitHub release that carries an installable APK.</summary>
    public sealed record ReleaseInfo(Version Version, string Tag, string Notes, string PageUrl, string ApkUrl, long ApkSize);

    /// <summary>
    /// Checks GitHub Releases for a newer version of the app. Only the sideloaded APK updates itself
    /// (SELF_UPDATE, see the csproj): Play Store policy forbids apps installed from Play from
    /// updating themselves, so the Play bundle leaves this switched off.
    /// </summary>
    public static partial class UpdateService
    {
        public const string LatestReleaseApiUrl = "https://api.github.com/repos/sibinsilva/iRailTracker/releases/latest";

        /// <summary>ReturningData of the "update available" notification, so a tap opens the update flow.</summary>
        public const string NotificationTag = "app_update";
        public const int NotificationId = 8000;

        /// <summary>How often the background check runs (the app also checks every time it starts).</summary>
        public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

        private const string LastNotifiedKey = "update_last_notified_version";
        public const string NotificationsEnabledKey = "update_notifications_enabled";

        public static bool IsSupported =>
#if SELF_UPDATE
            true;
#else
            false;
#endif

        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            // GitHub's API rejects requests without a User-Agent
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("iRailTracker", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        public static bool NotificationsEnabled
        {
            get => Preferences.Get(NotificationsEnabledKey, true);
            set => Preferences.Set(NotificationsEnabledKey, value);
        }

        public static Version CurrentVersion => Normalize(ParseVersion(AppInfo.Current.VersionString) ?? new Version(0, 0, 0));

        /// <summary>
        /// Returns the latest release if it's newer than this app, otherwise null. Throws on network
        /// or API errors so a manual check can report them; automatic checks catch them.
        /// </summary>
        public static async Task<ReleaseInfo?> GetAvailableUpdateAsync(CancellationToken ct = default)
        {
            using var response = await Http.GetAsync(LatestReleaseApiUrl, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);

            var release = ParseRelease(json);
            return release is not null && IsNewer(release, CurrentVersion) ? release : null;
        }

        /// <summary>The background check notifies once per version.</summary>
        public static bool WasNotified(ReleaseInfo release) => Preferences.Get(LastNotifiedKey, string.Empty) == release.Tag;
        public static void MarkNotified(ReleaseInfo release) => Preferences.Set(LastNotifiedKey, release.Tag);

        public static bool IsNewer(ReleaseInfo release, Version current) => Normalize(release.Version) > Normalize(current);

        /// <summary>"v2.62.1", "2.62", "V3.0.0-beta" → a version; null if the tag has no version number.</summary>
        public static Version? ParseVersion(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = VersionPattern().Match(text);
            if (!match.Success)
                return null;

            var minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            var patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            return new Version(int.Parse(match.Groups[1].Value), minor, patch);
        }

        /// <summary>
        /// Reads a GitHub "latest release" response. Returns null for drafts, pre-releases, tags
        /// without a version number, and releases without an APK.
        /// </summary>
        public static ReleaseInfo? ParseRelease(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (GetBool(root, "draft") || GetBool(root, "prerelease"))
                    return null;

                var tag = GetString(root, "tag_name");
                var version = ParseVersion(tag);
                if (tag is null || version is null)
                    return null;

                if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
                    return null;

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = GetString(asset, "name");
                    var url = GetString(asset, "browser_download_url");
                    if (name is null || url is null || !name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out var n) ? n : 0;
                    return new ReleaseInfo(version, tag, CleanNotes(GetString(root, "body")),
                        GetString(root, "html_url") ?? string.Empty, url, size);
                }

                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Turns GitHub's generated release notes (markdown headings, "by @user in #12" credits,
        /// a "Full Changelog" link) into a short plain list for a dialog.
        /// </summary>
        public static string CleanNotes(string? body, int maxLength = 600)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var lines = body.Replace("\r", string.Empty).Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith('#') && !l.StartsWith("**Full Changelog**", StringComparison.OrdinalIgnoreCase))
                .Select(l => CreditPattern().Replace(l, string.Empty))
                .Select(l => l.StartsWith("* ") || l.StartsWith("- ") ? "• " + l[2..] : l)
                .Select(l => l.Replace("**", string.Empty));

            var text = string.Join('\n', lines).Trim();
            return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
        }

        private static Version Normalize(Version v) => new(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0));

        private static string? GetString(JsonElement e, string name) =>
            e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

        private static bool GetBool(JsonElement e, string name) =>
            e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.True;

        [GeneratedRegex(@"(\d+)(?:\.(\d+))?(?:\.(\d+))?")]
        private static partial Regex VersionPattern();

        [GeneratedRegex(@"\s+by @\S+ in \S+$")]
        private static partial Regex CreditPattern();
    }
}
