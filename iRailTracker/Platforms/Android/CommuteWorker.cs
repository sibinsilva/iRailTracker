using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using iRailTracker.Service;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace iRailTracker
{
    /// <summary>
    /// Background check for the saved commute, run by WorkManager about every 15 minutes (Android's
    /// minimum) while there's a network connection. Does nothing outside the commute window.
    /// A fixed Java name keeps already-scheduled work valid across app updates.
    /// </summary>
    [Register("com.app.irailtracker.CommuteWorker")]
    public class CommuteWorker : Worker
    {
        public CommuteWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }

        public override Result DoWork()
        {
            try
            {
                var settings = DeparturesSummary.LoadEmbeddedSettings();
                if (settings is not null)
                {
                    CommuteService.CheckAsync(settings, ShowAlertAsync)
                        .WaitAsync(TimeSpan.FromSeconds(60))
                        .GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                // A failed check just waits for the next run
                CrashLog.Write("Commute check failed", ex);
            }

            return Result.InvokeSuccess();
        }

        private static Task ShowAlertAsync(CommuteAlert alert) =>
            LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = 7000 + (alert.Key.GetHashCode() & 0x3FF),
                Title = alert.Title,
                Description = alert.Message
            });
    }

    public static class CommuteScheduler
    {
        private const string WorkName = "commute_check";

        /// <summary>Schedules the periodic check while the commute is enabled; cancels it otherwise.</summary>
        public static void Update(CommutePreset preset)
        {
            var workManager = WorkManager.GetInstance(Android.App.Application.Context);

            if (!preset.Enabled || !preset.IsComplete)
            {
                workManager.CancelUniqueWork(WorkName);
                return;
            }

            var constraints = new Constraints.Builder()
                .SetRequiredNetworkType(NetworkType.Connected!)
                .Build();

            var request = PeriodicWorkRequest.Builder.From<CommuteWorker>(TimeSpan.FromMinutes(15))
                .SetConstraints(constraints)
                .Build();

            workManager.EnqueueUniquePeriodicWork(WorkName, ExistingPeriodicWorkPolicy.Update!, request);
        }

        /// <summary>Runs the check once now (Settings → Check now), e.g. to test the setup.</summary>
        public static void RunNow()
        {
            var request = OneTimeWorkRequest.Builder.From<CommuteWorker>().Build();
            WorkManager.GetInstance(Android.App.Application.Context)
                .EnqueueUniqueWork("commute_check_now", ExistingWorkPolicy.Replace!, request);
        }
    }
}
