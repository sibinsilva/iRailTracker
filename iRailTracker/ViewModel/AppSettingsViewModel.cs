using CommunityToolkit.Mvvm.Messaging;
using iRailTracker.Model;
using iRailTracker.Service;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace iRailTracker.ViewModel
{
    public partial class AppSettingsViewModel : BaseViewModel
    {
        #region Constructor

        public AppSettingsViewModel(DataService<List<Station>> stationListService)
        {
            // Auto refresh
            _isAutoRefreshEnabled = Preferences.Get(AppPreferences.AutoRefreshEnabled, false);

            // Interval
            var savedInterval = Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30);

            SelectedRefreshInterval =
                RefreshIntervals.FirstOrDefault(x => x.Value == savedInterval)
                ?? RefreshIntervals.First();

            // Favourite stations
            var stationNames = stationListService.Data
                .Select(s => s.StationDesc)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            StationNames = new ObservableCollection<string>(stationNames);

            FavouriteStations = new ObservableCollection<string>(FavouriteStationsStore.Load());

            // Delay notifications
            _isDelayNotificationsEnabled = Preferences.Get(AppPreferences.DelayNotificationsEnabled, false);

            var savedThreshold = Preferences.Get(AppPreferences.DelayNotificationThresholdMinutes, 5);
            SelectedDelayThreshold =
                DelayThresholds.FirstOrDefault(x => x.Value == savedThreshold)
                ?? DelayThresholds.First();
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
                    WeakReferenceMessenger.Default.Send(new AutoRefreshSettingsChangedMessage(IsAutoRefreshEnabled, RefreshIntervalSeconds));
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
                if(SetProperty(ref _selectedRefreshInterval, value) && value != null)
                {
                    RefreshIntervalSeconds = value.Value;

                    AutoRefreshService.Instance.UpdateSettings(
                        IsAutoRefreshEnabled,
                        RefreshIntervalSeconds);

                    WeakReferenceMessenger.Default.Send(new AutoRefreshSettingsChangedMessage(IsAutoRefreshEnabled, RefreshIntervalSeconds));
                }
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

        #region Favourite Stations

        public ObservableCollection<string> StationNames { get; }

        public ObservableCollection<string> FavouriteStations { get; }

        private int _selectedStationToAddIndex = -1;
        public int SelectedStationToAddIndex
        {
            get => _selectedStationToAddIndex;
            set => SetProperty(ref _selectedStationToAddIndex, value);
        }

        public ICommand AddFavouriteCommand => new Command(() =>
        {
            if (SelectedStationToAddIndex < 0 || SelectedStationToAddIndex >= StationNames.Count)
                return;

            var station = StationNames[SelectedStationToAddIndex];

            if (!FavouriteStations.Contains(station))
            {
                FavouriteStations.Add(station);
                SaveFavouriteStations();
            }

            SelectedStationToAddIndex = -1;
        });

        public ICommand RemoveFavouriteCommand => new Command<string>((station) =>
        {
            if (string.IsNullOrEmpty(station))
                return;

            if (FavouriteStations.Remove(station))
                SaveFavouriteStations();
        });

        private void SaveFavouriteStations()
        {
            FavouriteStationsStore.Save(FavouriteStations.ToList());
            WeakReferenceMessenger.Default.Send(new FavouriteStationsChangedMessage(FavouriteStations.ToList()));
        }

        #endregion

        #region Delay Notifications

        public ObservableCollection<RefreshIntervalOption> DelayThresholds { get; } =
            new()
            {
                new() { Display = "2 minutes", Value = 2 },
                new() { Display = "5 minutes", Value = 5 },
                new() { Display = "10 minutes", Value = 10 },
                new() { Display = "15 minutes", Value = 15 }
            };

        private RefreshIntervalOption? _selectedDelayThreshold;
        public RefreshIntervalOption? SelectedDelayThreshold
        {
            get => _selectedDelayThreshold;
            set
            {
                if (SetProperty(ref _selectedDelayThreshold, value) && value != null)
                    Preferences.Set(AppPreferences.DelayNotificationThresholdMinutes, value.Value);
            }
        }

        private bool _isDelayNotificationsEnabled;
        public bool IsDelayNotificationsEnabled
        {
            get => _isDelayNotificationsEnabled;
            set
            {
                if (!SetProperty(ref _isDelayNotificationsEnabled, value))
                    return;

                Preferences.Set(AppPreferences.DelayNotificationsEnabled, value);

                if (value)
                    _ = RequestNotificationPermissionAsync();
            }
        }

        private async Task RequestNotificationPermissionAsync()
        {
            var permission = new NotificationPermission();

            if (await LocalNotificationCenter.Current.AreNotificationsEnabled(permission))
                return;

            var granted = await LocalNotificationCenter.Current.RequestNotificationPermission(permission);

            if (!granted)
            {
                ShowError("Notification permission denied. Enable notifications in your device settings to receive delay alerts.");
                IsDelayNotificationsEnabled = false;
            }
        }

        #endregion
    }
}