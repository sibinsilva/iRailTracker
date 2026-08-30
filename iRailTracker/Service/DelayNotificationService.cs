using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace iRailTracker.Service
{
    public record TrainDelayInfo(string TrainCode, string Origin, string Destination, int LateMinutes);

    public class DelayNotificationService
    {
        private static DelayNotificationService? _instance;
        public static DelayNotificationService Instance =>
            _instance ??= new DelayNotificationService();

        private readonly Dictionary<string, int> _lastNotifiedDelay = new();
        private int _nextNotificationId = 1000;

        public DelayNotificationService() { }

        public async Task CheckAndNotify(TrainDelayInfo delay, int thresholdMinutes)
        {
            var toNotify = GetDelayToNotify(delay, thresholdMinutes);

            if (toNotify is not null)
                await ShowNotification(toNotify);
        }

        /// <summary>
        /// Applies the dedupe rule (notify once per newly-crossed or increased delay, per train)
        /// and returns the delay to notify about, or null if nothing should be shown. Split out from
        /// <see cref="CheckAndNotify"/> so the dedupe logic can be unit tested without a live notification host.
        /// </summary>
        public TrainDelayInfo? GetDelayToNotify(TrainDelayInfo delay, int thresholdMinutes)
        {
            if (delay.LateMinutes < thresholdMinutes)
            {
                _lastNotifiedDelay.Remove(delay.TrainCode);
                return null;
            }

            if (_lastNotifiedDelay.TryGetValue(delay.TrainCode, out var previouslyNotifiedDelay) &&
                delay.LateMinutes <= previouslyNotifiedDelay)
            {
                return null;
            }

            _lastNotifiedDelay[delay.TrainCode] = delay.LateMinutes;
            return delay;
        }

        private async Task ShowNotification(TrainDelayInfo delay)
        {
            var notification = new NotificationRequest
            {
                NotificationId = _nextNotificationId++,
                Title = $"{delay.TrainCode} delayed by {delay.LateMinutes} min",
                Description = $"{delay.Origin} to {delay.Destination}",
                ReturningData = delay.TrainCode
            };

            await LocalNotificationCenter.Current.Show(notification);
        }
    }
}
