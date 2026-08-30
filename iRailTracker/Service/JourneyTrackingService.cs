using iRailTracker.Model;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace iRailTracker.Service
{
    public class JourneyTrackingService
    {
        private static JourneyTrackingService? _instance;
        public static JourneyTrackingService Instance =>
            _instance ??= new JourneyTrackingService();

        private class TrackedJourney
        {
            public required string TrainCode { get; init; }
            public required string Origin { get; init; }
            public required string Destination { get; init; }
            public required int NotificationId { get; init; }
            public CancellationTokenSource Cts { get; } = new();
        }

        private readonly Dictionary<string, TrackedJourney> _tracked = new();
        private int _nextNotificationId = 5000;

        public JourneyTrackingService() { }

        public bool IsTracking(string trainCode) => _tracked.ContainsKey(trainCode);

        public async Task StartTracking(TrainJourney journey, Settings settings)
        {
            if (_tracked.ContainsKey(journey.TrainCode))
                return;

            var tracked = new TrackedJourney
            {
                TrainCode = journey.TrainCode,
                Origin = journey.Origin,
                Destination = journey.Destination,
                NotificationId = _nextNotificationId++
            };
            _tracked[journey.TrainCode] = tracked;

            if (_tracked.Count == 1)
                await StartForegroundServiceAsync(tracked);

            _ = PollLoop(tracked, settings);
        }

        public async Task StopTracking(string trainCode)
        {
            if (!_tracked.TryGetValue(trainCode, out var tracked))
                return;

            tracked.Cts.Cancel();
            _tracked.Remove(trainCode);

            LocalNotificationCenter.Current.Cancel(tracked.NotificationId);

            if (_tracked.Count == 0)
                await StopForegroundServiceAsync();
        }

        private async Task PollLoop(TrackedJourney tracked, Settings settings)
        {
            await PollOnce(tracked, settings);

            var intervalSeconds = Math.Max(Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30), 15);

            try
            {
                while (!tracked.Cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), tracked.Cts.Token);
                    await PollOnce(tracked, settings);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private async Task PollOnce(TrackedJourney tracked, Settings settings)
        {
            if (tracked.Cts.IsCancellationRequested)
                return;

            var stationService = new StationService();
            var movements = await stationService.GetTrainMovementsAsync(settings, tracked.TrainCode, _ => { });

            if (movements.Count == 0 || tracked.Cts.IsCancellationRequested)
                return;

            var destinationStop = movements.FirstOrDefault(m => m.LocationType == "D");
            var lateMinutes = destinationStop is not null &&
                TrainMovementAnalysis.TryComputeDelayMinutes(destinationStop, out var late)
                    ? late
                    : 0;

            if (destinationStop is not null && HasJourneyEnded(destinationStop))
            {
                await StopTracking(tracked.TrainCode);
                return;
            }

            var currentLocation = TrainMovementAnalysis.DetermineCurrentLocation(movements, DateTime.Now) ?? tracked.Origin;

            await ShowStatusNotification(tracked, currentLocation, lateMinutes);

            if (Preferences.Get(AppPreferences.DelayNotificationsEnabled, false))
            {
                var threshold = Preferences.Get(AppPreferences.DelayNotificationThresholdMinutes, 5);
                var delay = new TrainDelayInfo(tracked.TrainCode, tracked.Origin, tracked.Destination, lateMinutes);
                await DelayNotificationService.Instance.CheckAndNotify(delay, threshold);
            }
        }

        private static bool HasJourneyEnded(TrainMovement destinationStop)
        {
            if (!DateTime.TryParseExact(destinationStop.ExpectedArrival, "HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var expectedArrival))
            {
                return false;
            }

            var now = DateTime.Now;
            var arrivalToday = now.Date + expectedArrival.TimeOfDay;

            return now - arrivalToday > TimeSpan.FromMinutes(5);
        }

        private static async Task ShowStatusNotification(TrackedJourney tracked, string currentLocation, int lateMinutes)
        {
            var statusText = lateMinutes > 0
                ? $"Near {currentLocation} · Delayed {lateMinutes} min"
                : $"Near {currentLocation} · On time";

            var notification = new NotificationRequest
            {
                NotificationId = tracked.NotificationId,
                Title = $"{tracked.Origin} → {tracked.Destination}",
                Description = statusText,
                Android =
                {
                    ChannelId = "journey_tracking",
                    Ongoing = true,
                    AutoCancel = false
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
        }

        private static async Task StartForegroundServiceAsync(TrackedJourney tracked)
        {
#if ANDROID
            if (LocalNotificationCenter.Current is not IAndroidNotificationService androidService)
                return;

            await androidService.StartForegroundServiceAsync(new AndroidForegroundServiceRequest
            {
                ForegroundServiceType = AndroidForegroundServiceType.DataSync,
                Notification = new NotificationRequest
                {
                    NotificationId = tracked.NotificationId,
                    Title = $"{tracked.Origin} → {tracked.Destination}",
                    Description = "Tracking journey…",
                    Android =
                    {
                        ChannelId = "journey_tracking",
                        Ongoing = true,
                        AutoCancel = false
                    }
                }
            });
#else
            await Task.CompletedTask;
#endif
        }

        private static async Task StopForegroundServiceAsync()
        {
#if ANDROID
            if (LocalNotificationCenter.Current is IAndroidNotificationService androidService)
                await androidService.StopForegroundServiceAsync();
#else
            await Task.CompletedTask;
#endif
        }
    }
}
