using iRailTracker.Service;
using iRailTracker.View;
using Microsoft.Extensions.Configuration;

namespace iRailTracker
{
    public partial class App : Application
    {
        public App(IConfiguration config, ConfigLoader configLoader)
        {
            InitializeComponent();
            MainPage = new ContentPage
            {
                Content = new StartPage()
            };
            InitializeAsync(config, configLoader);
        }
        private async void InitializeAsync(IConfiguration config, ConfigLoader configLoader)
        {
            // Load settings asynchronously
            await configLoader.LoadSettingsAsync(config);

            // After settings are loaded, display the MainPage
            MainPage = new NavigationPage(new AppHome());
        }
    }
}
