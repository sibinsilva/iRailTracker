using iRailTracker.Service;
using Microsoft.Extensions.Configuration;

namespace iRailTracker
{
    public partial class App : Application
    {
        public App(IConfiguration config, ConfigLoader configLoader)
        {
            InitializeComponent();
            MainPage = new AppShell();
            _ = configLoader.LoadSettingsAsync(config);
        }
    }
}
