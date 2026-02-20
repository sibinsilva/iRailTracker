using iRailTracker.ViewModel;

namespace iRailTracker.View;

public partial class AppHome : ContentPage
{
    private readonly AppHomeViewModel _viewModel;
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
        _viewModel.ErrorOccurred += OnErrorOccurred;
    }

    private async void OnErrorOccurred(object? sender, string errorMessage)
    {
        await DisplayAlertAsync("Error", errorMessage, "OK");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Unsubscribe from error events to prevent memory leaks
        if (_viewModel != null)
        {
            _viewModel.ErrorOccurred -= OnErrorOccurred;
        }
    }
}