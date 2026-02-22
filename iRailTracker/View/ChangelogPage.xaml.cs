namespace iRailTracker.View;

public partial class ChangelogPage : ContentPage
{
    private static readonly List<ChangelogEntry> Entries =
    [
        new("2.0", [
            "🚂 Live train tracking with interactive map",
            "⭐ Favourite station quick access",
            "🔄 Auto-refresh for real-time updates",
            "⚙️ Configurable refresh intervals",
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

    private async void OnDismissClicked(object? sender, EventArgs e)
    {
        Preferences.Set(AppPreferences.LastSeenChangelogVersion, AppInfo.VersionString);
        await Navigation.PopModalAsync();
    }

    private record ChangelogEntry(string Version, string[] Items);
}
