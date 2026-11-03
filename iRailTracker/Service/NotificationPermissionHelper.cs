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
        /// <summary>
        /// Returns true if notifications are allowed, asking the user first if needed. If the user
        /// has turned them off, offers to open the app's settings and returns false.
        /// </summary>
        public static async Task<bool> EnsureAsync(string reason)
        {
            var permission = new NotificationPermission();

            if (await LocalNotificationCenter.Current.AreNotificationsEnabled(permission))
                return true;

            if (await LocalNotificationCenter.Current.RequestNotificationPermission(permission))
                return true;

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
