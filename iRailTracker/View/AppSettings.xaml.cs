using iRailTracker.ViewModel;

namespace iRailTracker.View;

public partial class AppSettings : ContentPage
{
    private readonly AppSettingsViewModel _viewModel;

	public AppSettings(AppSettingsViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    // Errors raised by the view model (e.g. "choose both stations first") were never shown here
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ErrorOccurred += OnErrorOccurred;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.ErrorOccurred -= OnErrorOccurred;
    }

    private async void OnErrorOccurred(object? sender, string message) =>
        await DisplayAlertAsync("Settings", message, "OK");
}