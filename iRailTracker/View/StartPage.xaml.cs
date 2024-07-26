namespace iRailTracker.View;
public partial class StartPage : ContentPage
{
    public StartPage()
    {
		InitializeComponent();
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AppHome());
    }
}