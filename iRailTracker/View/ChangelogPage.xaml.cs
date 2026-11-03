namespace iRailTracker.View;

public partial class ChangelogPage : ContentPage
{
    private static readonly List<ChangelogEntry> Entries =
    [
        new("2.62", [
            "🧳 Commute alerts: get notified when a train on your usual journey runs late (Settings)",
            "🧳 \"My commute\" shortcut on Home",
            "📱 Choose the station for each widget, plus a new one-row widget",
            "♿ Better screen-reader support",
        ]),
        new("2.61", [
            "🧭 Direction chips (Northbound, Southbound, …) and a DART badge on the board",
            "🗺️ The map loads faster (tiles are saved on your phone)",
            "🔁 Tracking resumes after your phone restarts, and uses less battery on long trips",
            "⏹️ Stop tracking straight from the notification",
            "🕐 Cards show when your train departs (or arrives, if it ends at your station)",
            "📡 Opens offline with your last departures",
            "📍 \"Near me\" opens the nearest station's departures",
            "🎨 Refreshed design throughout",
            "🐞 Fixes: app stuck on \"Loading\", Exit crash, missing train pin, \"Going to\" missing trains",
        ]),
        new("2.54", [
            "🗺️ Live map: the train moves as it travels, with your location, a legend and recenter buttons",
            "🎯 \"Going to\" filter shows only trains that stop at your destination",
            "🔍 Searchable station list",
            "📱 Home-screen widget with the next departures from your favourite station",
            "📡 Offline mode keeps the last departures on screen",
            "🎨 Clearer journey cards and route header",
        ]),
        new("2.0", [
            "🚂 Live train tracking with interactive map",
            "⭐ Favourite station quick access",
            "🔄 Auto-refresh for real-time updates",
            "⚙️ Configurable refresh intervals",
            "🗺️ Map tiles load again on the live status page",
            "🔔 Tracked journeys resume after the app is closed",
            "🌙 Correct status for trains running past midnight",
        ]),
        new("1.0", [
            "🎉 Initial Release",
        ]),
    ];

    public ChangelogPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"Version {AppInfo.VersionString}";
        BuildChangelogUI();
    }

    private void BuildChangelogUI()
    {
        foreach (var entry in Entries)
        {
            var header = new Label
            {
                Text = $"Version {entry.Version}",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
            };
            ChangelogEntries.Children.Add(header);

            foreach (var item in entry.Items)
            {
                var label = new Label
                {
                    Text = item,
                    FontSize = 14,
                    Margin = new Thickness(8, 0, 0, 0),
                };
                ChangelogEntries.Children.Add(label);
            }
        }
    }

    /// <summary>
    /// What's New appears when this changes, i.e. when a release adds an entry, not on every build.
    /// </summary>
    public static string LatestEntryVersion => Entries[0].Version;

    private async void OnDismissClicked(object? sender, EventArgs e)
    {
        Preferences.Set(AppPreferences.LastSeenChangelogVersion, LatestEntryVersion);
        await Navigation.PopModalAsync();
    }

    private record ChangelogEntry(string Version, string[] Items);
}
