using iRailTracker.ViewModel;

namespace iRailTracker.View;

public partial class AppSettings : ContentPage
{
	public AppSettings(AppSettingsViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}