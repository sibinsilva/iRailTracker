using Android.App;
using Android.Runtime;

namespace iRailTracker
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();

            // Replaced by "journey_status" (Android won't let an app raise a channel's importance,
            // so the old low-importance channel is removed rather than left behind in Settings)
            if (OperatingSystem.IsAndroidVersionAtLeast(26) &&
                GetSystemService(NotificationService) is NotificationManager manager)
            {
                manager.DeleteNotificationChannel("journey_tracking");
            }
        }
    }
}
