using iRailTracker.Service;
using iRailTracker.ViewModel;
using Microsoft.Maui.ApplicationModel;

namespace iRailTracker.View;

public partial class AppHome : ContentPage
{
    private readonly AppHomeViewModel _viewModel;
    private bool _isErrorHandlerSubscribed;
    private bool _isExitPromptVisible;
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

    protected override bool OnBackButtonPressed()
    {
        if (_isExitPromptVisible)
            return true;

        _isExitPromptVisible = true;

        Dispatcher.Dispatch(async () =>
        {
            try
            {
                var exit = await DisplayAlertAsync("Exit", "Do you want to exit the app?", "Exit", "Cancel");
                if (exit)
                {
                    try
                    {
                        Application.Current?.Quit();
                    }
                    catch
                    {
#if ANDROID
                        Platform.CurrentActivity?.FinishAffinity();
#endif
                    }
                }
            }
            finally
            {
                _isExitPromptVisible = false;
            }
        });

        return true;
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }
}