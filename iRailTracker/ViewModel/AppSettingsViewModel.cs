using CommunityToolkit.Mvvm.Messaging;
using iRailTracker.Model;
using iRailTracker.Service;
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

            // Favourite station
            var stationNames = stationListService.Data
                .Select(s => s.StationDesc)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            StationNames = new ObservableCollection<string>(stationNames);

            _favouriteStation = Preferences.Get(AppPreferences.FavouriteStation, string.Empty);
            _selectedFavouriteIndex = string.IsNullOrEmpty(_favouriteStation)
                ? -1
                : stationNames.IndexOf(_favouriteStation);
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

        #region Favourite Station

        public ObservableCollection<string> StationNames { get; }

        private string _favouriteStation = string.Empty;
        public string FavouriteStation
        {
            get => _favouriteStation;
            set
            {
                if (SetProperty(ref _favouriteStation, value))
                {
                    Preferences.Set(AppPreferences.FavouriteStation, value ?? string.Empty);
                    OnPropertyChanged(nameof(HasFavouriteStation));
                }
            }
        }

        public bool HasFavouriteStation => !string.IsNullOrEmpty(_favouriteStation);

        private int _selectedFavouriteIndex = -1;
        public int SelectedFavouriteIndex
        {
            get => _selectedFavouriteIndex;
            set
            {
                if (SetProperty(ref _selectedFavouriteIndex, value))
                {
                    if (value >= 0 && value < StationNames.Count)
                        FavouriteStation = StationNames[value];
                }
            }
        }

        public ICommand ClearFavouriteCommand => new Command(() =>
        {
            FavouriteStation = string.Empty;
            SelectedFavouriteIndex = -1;
        });

        #endregion
    }
}