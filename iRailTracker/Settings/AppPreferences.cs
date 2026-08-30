using System;
using System.Collections.Generic;
using System.Text;

namespace iRailTracker
{
    public static class AppPreferences
    {
        public const string AutoRefreshEnabled = "auto_refresh_enabled";
        public const string RefreshIntervalSeconds = "refresh_interval_seconds";
        public const string FavouriteStation = "favourite_station";
        public const string FavouriteStations = "favourite_stations";
        public const string LastSeenChangelogVersion = "last_seen_changelog_version";
        public const string DelayNotificationsEnabled = "delay_notifications_enabled";
        public const string DelayNotificationThresholdMinutes = "delay_notification_threshold_minutes";
    }
}
