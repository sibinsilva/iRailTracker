using iRailTracker.Resources.Strings;
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
            _isAutoRefreshEnabled = Preferences.Get(AppPreferences.AutoRefreshEnabled, true);

            // Interval
            var savedInterval = Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30);

            SelectedRefreshInterval =
                RefreshIntervals.FirstOrDefault(x => x.Value == savedInterval)
                ?? RefreshIntervals.First();

            // Favourite stations
            var stationNames = (stationListService.Data ?? [])
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

            // Commute
            _commute = CommuteService.Load();
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                               DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            WeekdayChips = new ObservableCollection<WeekdayChip>(days.Select(d => new WeekdayChip(d, _commute.Days.Contains(d))));
            _selectedCommuteThreshold = DelayThresholds.FirstOrDefault(x => x.Value == _commute.ThresholdMinutes) ?? DelayThresholds.First();
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
            var granted = await NotificationPermissionHelper.EnsureAsync(AppMessages.NotificationsNeededForDelayAlerts);

            if (!granted)
                IsDelayNotificationsEnabled = false;
        }

        #endregion

        #region Appearance

        // Order matches the ThemeStyle / ThemeMode enums
        public IReadOnlyList<string> ThemeStyleOptions { get; } = ["Liquid Glass", "Classic"];
        public IReadOnlyList<string> ThemeModeOptions { get; } = ["System default", "Light", "Dark"];

        public int SelectedThemeStyleIndex
        {
            get => (int)ThemeService.Style;
            set
            {
                if (value < 0 || value == (int)ThemeService.Style)
                    return;
                ThemeService.ApplyStyle((ThemeStyle)value);
                OnPropertyChanged();
            }
        }

        public int SelectedThemeModeIndex
        {
            get => (int)ThemeService.Mode;
            set
            {
                if (value < 0 || value == (int)ThemeService.Mode)
                    return;
                ThemeService.ApplyMode((ThemeMode)value);
                OnPropertyChanged();
            }
        }

        #endregion

        #region App updates

        public bool IsSelfUpdateSupported => UpdateService.IsSupported;

        public string AppVersionDisplay => $"Version {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";

        public bool IsUpdateNotificationsEnabled
        {
            get => UpdateService.NotificationsEnabled;
            set
            {
                if (value == UpdateService.NotificationsEnabled)
                    return;
                UpdateService.NotificationsEnabled = value;
#if SELF_UPDATE
                UpdateScheduler.Update(value);
#endif
                OnPropertyChanged();
            }
        }

        public ICommand CheckForUpdatesCommand => new Command(async () => await View.UpdateFlow.CheckNowAsync());

        #endregion

        #region Commute

        private CommutePreset _commute;
        private RefreshIntervalOption _selectedCommuteThreshold;

        public ObservableCollection<WeekdayChip> WeekdayChips { get; }

        public bool IsCommuteEnabled
        {
            get => _commute.Enabled;
            set
            {
                if (value == _commute.Enabled)
                    return;
                if (value)
                    _ = EnableCommuteAsync();
                else
                    SaveCommute(_commute with { Enabled = false });
            }
        }

        public string CommuteFromDisplay => string.IsNullOrEmpty(_commute.From) ? "Choose station" : _commute.From;
        public string CommuteToDisplay => string.IsNullOrEmpty(_commute.To) ? "Choose station" : _commute.To;

        public TimeSpan CommuteStart
        {
            get => _commute.WindowStart;
            set { if (value != _commute.WindowStart) SaveCommute(_commute with { WindowStart = value }); }
        }

        public TimeSpan CommuteEnd
        {
            get => _commute.WindowEnd;
            set { if (value != _commute.WindowEnd) SaveCommute(_commute with { WindowEnd = value }); }
        }

        public RefreshIntervalOption SelectedCommuteThreshold
        {
            get => _selectedCommuteThreshold;
            set
            {
                if (value is null || !SetProperty(ref _selectedCommuteThreshold, value))
                    return;
                SaveCommute(_commute with { ThresholdMinutes = value.Value });
            }
        }

        public string CommuteStatus => !_commute.IsComplete
            ? "Choose both stations, then switch it on."
            : _commute.Enabled
                ? $"Checking every 15 min, {_commute.WindowStart:hh\\:mm}–{_commute.WindowEnd:hh\\:mm} on the selected days."
                : "Off";

        public ICommand CheckCommuteNowCommand => new Command(() =>
        {
            if (!_commute.IsComplete)
            {
                ShowError("Choose the stations you travel from and to first.");
                return;
            }
            if (!_commute.Enabled)
            {
                // IsActiveAt is false when off, which used to be reported as "outside your window"
                ShowError("Commute alerts are off. Switch them on to get alerts.");
                return;
            }
#if ANDROID
            CommuteScheduler.RunNow();
#endif
            ShowError(_commute.IsActiveAt(IrishTime.Now)
                ? "Checking now. You'll get a notification if a train in your window is running late."
                : "Checking now, but it's outside your commute window or days, so no alerts will be sent.");
        });

        public ICommand ChooseCommuteFromCommand => new Command(async () => await ChooseCommuteStationAsync(isFrom: true));
        public ICommand ChooseCommuteToCommand => new Command(async () => await ChooseCommuteStationAsync(isFrom: false));

        public ICommand ToggleWeekdayCommand => new Command<WeekdayChip>(chip =>
        {
            if (chip is null)
                return;
            chip.IsSelected = !chip.IsSelected;
            SaveCommute(_commute with { Days = WeekdayChips.Where(c => c.IsSelected).Select(c => c.Day).ToArray() });
        });

        private async Task ChooseCommuteStationAsync(bool isFrom)
        {
            var nav = Application.Current?.Windows[0].Page?.Navigation;
            if (nav is null)
                return;

            var station = await View.StationSearchPage.PickAsync(nav, isFrom ? "Commute from" : "Commute to", StationNames);
            if (station is null)
                return;

            SaveCommute(isFrom ? _commute with { From = station } : _commute with { To = station });
        }

        private async Task EnableCommuteAsync()
        {
            if (!_commute.IsComplete)
            {
                ShowError("Choose the stations you travel from and to first.");
                OnPropertyChanged(nameof(IsCommuteEnabled));
                return;
            }

            var granted = await NotificationPermissionHelper.EnsureAsync(AppMessages.NotificationsNeededForDelayAlerts);
            SaveCommute(_commute with { Enabled = granted });
        }

        private void SaveCommute(CommutePreset preset)
        {
            _commute = preset;
            CommuteService.Save(preset);
            OnPropertyChanged(nameof(IsCommuteEnabled));
            OnPropertyChanged(nameof(CommuteFromDisplay));
            OnPropertyChanged(nameof(CommuteToDisplay));
            OnPropertyChanged(nameof(CommuteStart));
            OnPropertyChanged(nameof(CommuteEnd));
            OnPropertyChanged(nameof(CommuteStatus));
            WeakReferenceMessenger.Default.Send(new CommuteChangedMessage());
        }

        #endregion

        #region Diagnostics

        public bool HasCrashLog => CrashLog.HasEntries;

        public string CrashLogStatus => HasCrashLog
            ? "A crash was recorded. Share the log with the developer to help fix it."
            : "No crashes recorded.";

        public ICommand ShareCrashLogCommand => new Command(async () =>
        {
            if (!CrashLog.HasEntries)
                return;
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "iRailTracker crash log",
                File = new ShareFile(CrashLog.FilePath, "text/plain")
            });
        });

        public ICommand ClearCrashLogCommand => new Command(() =>
        {
            CrashLog.Clear();
            OnPropertyChanged(nameof(HasCrashLog));
            OnPropertyChanged(nameof(CrashLogStatus));
        });

        #endregion
    }
}