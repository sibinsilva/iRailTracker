using iRailTracker.Service;
using iRailTracker.ViewModel;

namespace iRailTracker.View;

public partial class AppHome : ContentPage
{
    private readonly AppHomeViewModel _viewModel;
    private bool _isErrorHandlerSubscribed;
    public AppHome()
    {
        InitializeComponent();
        var serviceProvider = MauiProgram.Current;
        if (serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider is not initialized.");
        }
        var viewModel = serviceProvider.GetService<AppHomeViewModel>();
        if (viewModel == null)
        {
            throw new InvalidOperationException("AppHomeViewModel service is not registered.");
        }
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnErrorOccurred(object? sender, string errorMessage)
    {
        await DisplayAlertAsync("Error", errorMessage, "OK");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _viewModel.OnAppearing();

        if (!_isErrorHandlerSubscribed)
        {
            _viewModel.ErrorOccurred += OnErrorOccurred;
            _isErrorHandlerSubscribed = true;
        }

        var enabled = Preferences.Get(AppPreferences.AutoRefreshEnabled, false);
        var interval = Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30);

        AutoRefreshService.Instance.Start(enabled, interval);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        AutoRefreshService.Instance.Stop();

        _viewModel.OnDisappearing();

        if (_isErrorHandlerSubscribed)
        {
            _viewModel.ErrorOccurred -= OnErrorOccurred;
            _isErrorHandlerSubscribed = false;
        }
    }
}