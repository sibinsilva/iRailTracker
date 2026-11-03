using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace iRailTracker.Service
{
    /// <summary>
    /// Makes sure the app may post notifications (Android 13+ asks at runtime). Without it,
    /// Android silently drops the journey tracking notification.
    /// </summary>
    public static class NotificationPermissionHelper
    {
        private const string AskedKey = "notification_permission_asked";

        /// <summary>
        /// Returns true if notifications are allowed, asking the user first if needed. If the user
        /// has turned them off, offers to open the app's settings and returns false.
        /// </summary>
        public static async Task<bool> EnsureAsync(string reason)
        {
            var permission = new NotificationPermission();

            if (await LocalNotificationCenter.Current.AreNotificationsEnabled(permission))
                return true;

            // The first time, the system prompt is the explanation; if the user says no there,
            // respect it rather than following up with a second dialog straight away.
            var askedBefore = Preferences.Get(AskedKey, false);
            Preferences.Set(AskedKey, true);

            if (await LocalNotificationCenter.Current.RequestNotificationPermission(permission))
                return true;

            if (!askedBefore)
                return false;

            // Android won't show the prompt again once the user has said no; send them to settings
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null &&
                await page.DisplayAlertAsync("Notifications are off", reason, "Open settings", "Not now"))
            {
                AppInfo.Current.ShowSettingsUI();
            }

            return false;
        }
    }
}
