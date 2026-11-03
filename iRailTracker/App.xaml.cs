using iRailTracker.Resources.Strings;
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

        private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Create the window first and hand it to InitializeAsync. Previously InitializeAsync
            // looked the window up in Windows[0], but when startup finished synchronously (station
            // list from the on-device cache) the window didn't exist yet, so Home was never shown
            // and the app stayed on "Loading" forever.
            var window = new Window(new StartPage());
            _ = InitializeAsync(window);
            return window;
        }

        private async Task InitializeAsync(Window window)
        {
            string? loadError = null;
            try
            {
                await _configLoader.LoadSettingsAsync(_config, e => loadError = e).WaitAsync(StartupTimeout);
            }
            catch (TimeoutException)
            {
                loadError ??= AppMessages.StationListTooSlow;
            }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                window.Page = new NavigationPage(new AppHome());

                if (loadError is not null)
                    await window.Page.DisplayAlertAsync("Can't load stations", loadError, "OK");
            });

            // Resume journeys tracked before the app was closed, in the background so it can
            // never hold up startup. Home re-syncs its bell icons when this finishes.
            if (_settings.Data is not null)
                _ = RestoreTrackingAsync(_settings.Data);

            var launchDetails = LocalNotificationCenter.LaunchNotificationDetails;
            if (launchDetails is { DidNotificationLaunchApp: true })
                HandleNotificationTap(launchDetails.Request?.ReturningData);
        }

        private static async Task RestoreTrackingAsync(Settings settings)
        {
            try
            {
                await JourneyTrackingService.Instance.RestoreAsync(settings).WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Restoring tracked journeys failed: {ex}");
            }
            finally
            {
                WeakReferenceMessenger.Default.Send(new TrackingRestoredMessage());
            }
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