using iRailTracker.Service;
using iRailTracker.ViewModel;

namespace iRailTracker.View;

public partial class AppHome : ContentPage
{
    private readonly AppHomeViewModel _viewModel;
    public AppHome()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Current.GetService<AppHomeViewModel>();
        BindingContext = _viewModel;
        _viewModel.ErrorOccurred += OnErrorOccurred;
    }

    private async void OnErrorOccurred(object sender, string errorMessage)
    {
        await DisplayAlert("Error", errorMessage, "OK");
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