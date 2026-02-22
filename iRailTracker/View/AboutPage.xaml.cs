namespace iRailTracker.View;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"Version {AppInfo.VersionString} (Build {AppInfo.BuildString})";
    }

    private async void OnGitHubLinkTapped(object? sender, EventArgs e)
    {
        await Launcher.OpenAsync(new Uri("https://github.com/sibinsilva/iRailTracker"));
    }
}
