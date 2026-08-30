using CommunityToolkit.Mvvm.Messaging;
using iRailTracker.Service;
using iRailTracker.View;
using Microsoft.Extensions.Configuration;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

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

            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
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

            var launchDetails = LocalNotificationCenter.LaunchNotificationDetails;
            if (launchDetails is { DidNotificationLaunchApp: true })
                HandleNotificationTap(launchDetails.Request?.ReturningData);
        }

        private void OnNotificationActionTapped(NotificationActionEventArgs e)
        {
            if (e.IsTapped)
                HandleNotificationTap(e.Request?.ReturningData);
        }

        private void HandleNotificationTap(string? trainCode)
        {
            if (string.IsNullOrEmpty(trainCode))
                return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var nav = Windows.Count > 0 ? Windows[0].Page?.Navigation : null;
                if (nav is null)
                    return;

                while (nav.ModalStack.Count > 0)
                    await nav.PopModalAsync(false);

                while (nav.NavigationStack.Count > 1)
                    await nav.PopAsync(false);

                WeakReferenceMessenger.Default.Send(new NavigateToTrackedJourneyMessage(trainCode));
            });
        }
    }
}