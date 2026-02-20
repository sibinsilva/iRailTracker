using iRailTracker.Service;
using iRailTracker.View;
using Microsoft.Extensions.Configuration;

namespace iRailTracker
{
    public partial class App : Application
    {
        private readonly IConfiguration _config;
        private readonly ConfigLoader _configLoader;

        public App(IConfiguration config, ConfigLoader configLoader)
        {
            InitializeComponent();
            _config = config;
            _configLoader = configLoader;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var startPage = new StartPage();
            _ = InitializeAsync();
            return new Window(startPage);
        }

        private async Task InitializeAsync()
        {
            await _configLoader.LoadSettingsAsync(_config);

            if (Windows.Count > 0)
            {
                Windows[0].Page = new NavigationPage(new AppHome());
            }
        }
    }
}