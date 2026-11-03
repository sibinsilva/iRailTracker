namespace iRailTracker.View;

/// <summary>
/// Full-screen, searchable station list, replacing the long radio-button Picker dialog.
/// Use <see cref="PickAsync"/>; it returns the chosen name, or null if cancelled.
/// </summary>
public partial class StationSearchPage : ContentPage
{
    private readonly IReadOnlyList<string> _allStations;
    private readonly TaskCompletionSource<string?> _result = new();

    private StationSearchPage(string title, IEnumerable<string> stations)
    {
        InitializeComponent();
        Title = title;
        _allStations = stations.ToList();
        StationList.ItemsSource = _allStations;
    }

    public static async Task<string?> PickAsync(INavigation navigation, string title, IEnumerable<string> stations)
    {
        var page = new StationSearchPage(title, stations);
        await navigation.PushModalAsync(new NavigationPage(page));
        return await page._result.Task;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StationSearchBar.Focus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Back button or swipe-down: treat as cancel
        _result.TrySetResult(null);
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim() ?? string.Empty;
        StationList.ItemsSource = query.Length == 0
            ? _allStations
            : _allStations.Where(s => s.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async void OnStationSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not string station)
            return;

        _result.TrySetResult(station);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _result.TrySetResult(null);
        await Navigation.PopModalAsync();
    }
}
