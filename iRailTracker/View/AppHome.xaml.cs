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
    }
}