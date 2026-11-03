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
        private readonly DataService<Settings> _settings;

        public App(IConfiguration config, ConfigLoader configLoader, DataService<Settings> settings)
        {
            InitializeComponent();
            _config = config;
            _configLoader = configLoader;
            _settings = settings;

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
            string? loadError = null;
            await _configLoader.LoadSettingsAsync(_config, e => loadError = e);

            // Resume journeys that were being tracked when the app was last closed or killed,
            // before Home builds its cards so their bell icons show the right state.
            if (_settings.Data is not null)
                await JourneyTrackingService.Instance.RestoreAsync(_settings.Data);

            if (Windows.Count > 0)
            {
                Windows[0].Page = new NavigationPage(new AppHome());

                // Previously a failed start just showed an empty Home with no explanation
                if (loadError is not null)
                    await Windows[0].Page!.DisplayAlertAsync("Can't load stations", loadError, "OK");
            }

            var launchDetails = LocalNotificationCenter.LaunchNotificationDetails;
            if (launchDetails is { DidNotificationLaunchApp: true })
                HandleNotificationTap(launchDetails.Request?.ReturningData);
        }

        private void OnNotificationActionTapped(NotificationActionEventArgs e)
        {
            if (e.ActionId == JourneyTrackingService.StopTrackingActionId &&
                e.Request?.ReturningData is { Length: > 0 } trainCode)
            {
                _ = JourneyTrackingService.Instance.StopTracking(trainCode);
                return;
            }

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