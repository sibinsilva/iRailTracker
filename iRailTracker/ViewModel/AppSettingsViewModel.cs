using iRailTracker.Model;
using iRailTracker.Service;
using System.Collections.ObjectModel;

namespace iRailTracker.ViewModel
{
    public partial class AppSettingsViewModel : BaseViewModel
    {
        #region Constructor

        public AppSettingsViewModel()
        {
            // Auto refresh
            _isAutoRefreshEnabled =
                Preferences.ContainsKey(AppPreferences.AutoRefreshEnabled)
                    ? Preferences.Get(AppPreferences.AutoRefreshEnabled, false)
                    : false;

            // Interval
            var savedInterval =
                Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30);

            SelectedRefreshInterval =
                RefreshIntervals.FirstOrDefault(x => x.Value == savedInterval)
                ?? RefreshIntervals.First();
        }

        #endregion

        #region Auto Refresh Toggle

        private bool _isAutoRefreshEnabled;
        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (SetProperty(ref _isAutoRefreshEnabled, value))
                {
                    Preferences.Set(AppPreferences.AutoRefreshEnabled, value);
                    AutoRefreshService.Instance.UpdateSettings(IsAutoRefreshEnabled, RefreshIntervalSeconds);
                }
            }
        }

        #endregion

        #region Refresh Interval Options

        public ObservableCollection<RefreshIntervalOption> RefreshIntervals { get; } =
            new()
            {
                new() { Display = "30 seconds", Value = 30 },
                new() { Display = "1 minute", Value = 60 },
                new() { Display = "2 minutes", Value = 120 },
                new() { Display = "5 minutes", Value = 300 }
            };

        private RefreshIntervalOption? _selectedRefreshInterval;
        public RefreshIntervalOption? SelectedRefreshInterval
        {
            get => _selectedRefreshInterval;
            set
            {
                if (SetProperty(ref _selectedRefreshInterval, value) && value != null)
                    RefreshIntervalSeconds = value.Value;
                AutoRefreshService.Instance.UpdateSettings(IsAutoRefreshEnabled, RefreshIntervalSeconds);
            }
        }

        private int _refreshIntervalSeconds;
        public int RefreshIntervalSeconds
        {
            get => _refreshIntervalSeconds;
            set
            {
                if (SetProperty(ref _refreshIntervalSeconds, value))
                    Preferences.Set(AppPreferences.RefreshIntervalSeconds, value);
            }
        }

        #endregion
    }
}