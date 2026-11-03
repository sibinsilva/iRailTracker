using Android.App;
using Android.Content;
using iRailTracker.Service;

namespace iRailTracker
{
    /// <summary>
    /// Resumes journey tracking after the phone restarts or the app is updated, so a tracked train
    /// doesn't silently stop until the app is next opened. Both broadcasts are exempt from Android's
    /// background foreground-service start limits (and specialUse may start from boot on Android 15).
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced])]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (!JourneyTrackingService.HasSavedJourneys())
                return;

            var settings = DeparturesSummary.LoadEmbeddedSettings();
            if (settings is null)
                return;

            var pending = GoAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    // The foreground service keeps polling after the receiver finishes
                    await JourneyTrackingService.Instance.RestoreAsync(settings).WaitAsync(TimeSpan.FromSeconds(8));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Resuming tracking after boot failed: {ex}");
                }
                finally
                {
                    pending?.Finish();
                }
            });
        }
    }
}
