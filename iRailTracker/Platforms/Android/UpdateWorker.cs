#if SELF_UPDATE
using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using iRailTracker.Service;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace iRailTracker
{
    /// <summary>
    /// Background check for a new GitHub release, run by WorkManager a few times a day. Posts one
    /// notification per new version; tapping it opens the in-app update flow.
    /// A fixed Java name keeps already-scheduled work valid across app updates.
    /// </summary>
    [Register("com.app.irailtracker.UpdateWorker")]
    public class UpdateWorker : Worker
    {
        public UpdateWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }

        public override Result DoWork()
        {
            try
            {
                if (!UpdateService.NotificationsEnabled)
                    return Result.InvokeSuccess();

                var release = UpdateService.GetAvailableUpdateAsync()
                    .WaitAsync(TimeSpan.FromSeconds(60))
                    .GetAwaiter().GetResult();

                if (release is not null && !UpdateService.WasNotified(release))
                {
                    LocalNotificationCenter.Current.Show(new NotificationRequest
                    {
                        NotificationId = UpdateService.NotificationId,
                        Title = "iRailTracker update available",
                        Description = $"Version {release.Version} is ready to install. Tap to update.",
                        ReturningData = UpdateService.NotificationTag
                    }).GetAwaiter().GetResult();

                    UpdateService.MarkNotified(release);
                }
            }
            catch (Exception ex)
            {
                // No network, GitHub rate limit, etc.: the next run tries again
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            return Result.InvokeSuccess();
        }
    }

    public static class UpdateScheduler
    {
        private const string WorkName = "update_check";

        /// <summary>Schedules the periodic check while update notifications are on; cancels it otherwise.</summary>
        public static void Update(bool enabled)
        {
            var workManager = WorkManager.GetInstance(Android.App.Application.Context);

            if (!enabled)
            {
                workManager.CancelUniqueWork(WorkName);
                return;
            }

            var constraints = new Constraints.Builder()
                .SetRequiredNetworkType(NetworkType.Connected!)
                .Build();

            var request = PeriodicWorkRequest.Builder.From<UpdateWorker>(UpdateService.CheckInterval)
                .SetConstraints(constraints)
                .Build();

            // Keep: re-scheduling on every launch must not push the next run further away
            workManager.EnqueueUniquePeriodicWork(WorkName, ExistingPeriodicWorkPolicy.Keep!, request);
        }
    }
}
#endif
