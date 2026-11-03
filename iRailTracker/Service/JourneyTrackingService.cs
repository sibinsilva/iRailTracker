using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.Messaging;
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

        private const string StorageKey = "tracked_journeys";

        /// <summary>Id of the "Stop tracking" button on the status notification (registered in MauiProgram).</summary>
        public const int StopTrackingActionId = 100;
        private const int FirstNotificationId = 5000;
        private static readonly TimeSpan JourneyEndGrace = TimeSpan.FromMinutes(5);

        /// <summary>What gets saved to Preferences so tracking survives the app being killed.</summary>
        public sealed record TrackedJourneyInfo(string TrainCode, string Origin, string Destination, DateTime TrainDate);

        private sealed class TrackedJourney
        {
            public required TrackedJourneyInfo Info { get; init; }
            public required int NotificationId { get; init; }
            public CancellationTokenSource Cts { get; } = new();
            public string TrainCode => Info.TrainCode;
        }

        private readonly ConcurrentDictionary<string, TrackedJourney> _tracked = new();
        private readonly SemaphoreSlim _serviceLock = new(1, 1);
        private int _nextNotificationId = FirstNotificationId;
        private bool _restored;

        public JourneyTrackingService() { }

        public bool IsTracking(string trainCode) => _tracked.ContainsKey(trainCode);

        public Task StartTracking(TrainJourney journey, Settings settings) =>
            StartTracking(new TrackedJourneyInfo(journey.TrainCode, journey.Origin, journey.Destination, journey.TrainDate.Date), settings);

        /// <summary>
        /// Resumes tracking of journeys saved before the app was last closed or killed. Journeys
        /// whose service date is more than a day old are dropped; finished ones stop on their first poll.
        /// </summary>
        public async Task RestoreAsync(Settings settings)
        {
            if (_restored)
                return;
            _restored = true;

            var cutoff = IrishTime.Today.AddDays(-1);
            foreach (var info in LoadSaved().Where(j => j.TrainDate >= cutoff))
                await StartTracking(info, settings);

            Save();
        }

        public async Task StopTracking(string trainCode)
        {
            if (!_tracked.TryRemove(trainCode, out var tracked))
                return;

            tracked.Cts.Cancel();
            Save();
            WeakReferenceMessenger.Default.Send(new TrackingStoppedMessage(trainCode));

            LocalNotificationCenter.Current.Cancel(tracked.NotificationId);

            await _serviceLock.WaitAsync();
            try
            {
                if (_tracked.IsEmpty)
                    await StopForegroundServiceAsync();
            }
            finally
            {
                _serviceLock.Release();
            }
        }

        private async Task StartTracking(TrackedJourneyInfo info, Settings settings)
        {
            var tracked = new TrackedJourney
            {
                Info = info,
                NotificationId = Interlocked.Increment(ref _nextNotificationId)
            };

            if (!_tracked.TryAdd(info.TrainCode, tracked))
                return;

            Save();

            await _serviceLock.WaitAsync();
            try
            {
                if (_tracked.Count == 1)
                    await StartForegroundServiceAsync(tracked);
            }
            finally
            {
                _serviceLock.Release();
            }

            _ = PollLoop(tracked, settings);
        }

        private async Task PollLoop(TrackedJourney tracked, Settings settings)
        {
            var intervalSeconds = Math.Max(Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30), 15);

            try
            {
                await PollOnce(tracked, settings);

                while (!tracked.Cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), tracked.Cts.Token);
                    await PollOnce(tracked, settings);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                // A fire-and-forget loop must never crash the app; the next start will retry.
                System.Diagnostics.Debug.WriteLine($"Journey tracking for {tracked.TrainCode} stopped: {ex}");
            }
        }

        private async Task PollOnce(TrackedJourney tracked, Settings settings)
        {
            if (tracked.Cts.IsCancellationRequested)
                return;

            var info = tracked.Info;
            var stationService = new StationService();
            var movements = await stationService.GetTrainMovementsAsync(settings, info.TrainCode, info.TrainDate, _ => { });

            if (movements.Count == 0 || tracked.Cts.IsCancellationRequested)
                return;

            var now = IrishTime.Now;

            if (TrainMovementAnalysis.HasJourneyEnded(movements, now, info.TrainDate, JourneyEndGrace))
            {
                await StopTracking(info.TrainCode);
                return;
            }

            var currentStop = TrainMovementAnalysis.DetermineCurrentStop(movements, now, info.TrainDate);
            var currentLocation = currentStop?.LocationFullName ?? info.Origin;
            var lateMinutes = await GetOfficialDelayMinutes(stationService, settings, info, movements, now);

            if (tracked.Cts.IsCancellationRequested)
                return;

            await ShowStatusNotification(tracked, currentLocation, lateMinutes);

            if (Preferences.Get(AppPreferences.DelayNotificationsEnabled, false))
            {
                var threshold = Preferences.Get(AppPreferences.DelayNotificationThresholdMinutes, 5);
                var delay = new TrainDelayInfo(info.TrainCode, info.Origin, info.Destination, lateMinutes);
                await DelayNotificationService.Instance.CheckAndNotify(delay, threshold);
            }
        }

        /// <summary>
        /// Looks up the train's official, live "Late" value from the departure board of the next
        /// station it hasn't reached yet - the same field and endpoint the Home page cards use, so the
        /// tracking notification always agrees with what's shown there.
        /// </summary>
        private static async Task<int> GetOfficialDelayMinutes(
            StationService stationService, Settings settings, TrackedJourneyInfo info, List<TrainMovement> movements, DateTime now)
        {
            var nextStop = TrainMovementAnalysis.DetermineNextStop(movements, now, info.TrainDate);
            if (nextStop is null)
                return 0;

            var board = await stationService.GetTrainServicesAsync(settings, nextStop.LocationCode, _ => { });
            var match = board.FirstOrDefault(s => s.Traincode.Trim() == info.TrainCode.Trim());

            return match?.Late ?? 0;
        }

        private void Save()
        {
            var infos = _tracked.Values.Select(t => t.Info).ToList();
            Preferences.Set(StorageKey, JsonSerializer.Serialize(infos, TrackingJsonContext.Default.ListTrackedJourneyInfo));
        }

        private static List<TrackedJourneyInfo> LoadSaved()
        {
            var json = Preferences.Get(StorageKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return [];

            try
            {
                return JsonSerializer.Deserialize(json, TrackingJsonContext.Default.ListTrackedJourneyInfo) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static NotificationRequest BuildNotification(TrackedJourney tracked, string description) => new()
        {
            NotificationId = tracked.NotificationId,
            Title = $"{tracked.Info.Origin} → {tracked.Info.Destination}",
            Description = description,
            ReturningData = tracked.TrainCode,
            CategoryType = NotificationCategoryType.Status,
            Android =
            {
                ChannelId = "journey_status",
                Ongoing = true,
                OnlyAlertOnce = true,
                AutoCancel = false
            }
        };

        private static async Task ShowStatusNotification(TrackedJourney tracked, string currentLocation, int lateMinutes)
        {
            var statusText = lateMinutes switch
            {
                > 0 => $"Near {currentLocation} · Delayed {lateMinutes} min",
                < 0 => $"Near {currentLocation} · Early by {Math.Abs(lateMinutes)} min",
                _ => $"Near {currentLocation} · On time"
            };

            await LocalNotificationCenter.Current.Show(BuildNotification(tracked, statusText));
        }

        private static async Task StartForegroundServiceAsync(TrackedJourney tracked)
        {
#if ANDROID
            if (LocalNotificationCenter.Current is not IAndroidNotificationService androidService)
                return;

            // Android 15+ limits dataSync foreground services to about 6 hours a day, which a long
            // day of tracking can exceed. specialUse (Android 14+) has no time limit; older
            // versions keep dataSync. Both types are declared in AndroidManifest.xml.
            var serviceType = OperatingSystem.IsAndroidVersionAtLeast(34)
                ? AndroidForegroundServiceType.SpecialUse
                : AndroidForegroundServiceType.DataSync;

            await androidService.StartForegroundServiceAsync(new AndroidForegroundServiceRequest
            {
                ForegroundServiceType = serviceType,
                Notification = BuildNotification(tracked, "Tracking journey…")
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

    [JsonSerializable(typeof(List<JourneyTrackingService.TrackedJourneyInfo>))]
    internal partial class TrackingJsonContext : JsonSerializerContext
    {
    }
}
