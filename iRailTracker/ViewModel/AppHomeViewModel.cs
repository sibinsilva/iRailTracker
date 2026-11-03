using CommunityToolkit.Mvvm.Messaging;
using iRailTracker.Model;
using iRailTracker.Resources.Strings;
using iRailTracker.Service;
using iRailTracker.View;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace iRailTracker.ViewModel
{
    public class AppHomeViewModel : BaseViewModel,
        IRecipient<AutoRefreshMessage>,
        IRecipient<AutoRefreshSettingsChangedMessage>,
        IRecipient<FavouriteStationsChangedMessage>,
        IRecipient<TrackingStoppedMessage>,
        IRecipient<TrackingRestoredMessage>,
        IRecipient<NavigateToTrackedJourneyMessage>
    {
        #region Fields

        private readonly DataService<List<Station>> _stationListService;
        private readonly DataService<Settings> _settings;
        private readonly StationService _stationService;
        private readonly RouteFilterService _routeFilter;
        private readonly object _journeysLock = new();
        private List<TrainJourney> _allJourneys = new();
        private string _selectedDirection = DirectionChip.All;
        private ObservableCollection<string> _stationOptions;
        private ObservableCollection<TrainJourney> _trainJourneys;
        private ObservableCollection<string> _favouriteStations;
        private readonly List<Station> stationList;
        private readonly List<string> _stationNames = new List<string>();
        private bool _isFindNearbyStationChecked;
        private bool _isLocateStationChecked;
        private bool _isStationListLoaded;
        private bool _hideSearchButton;
        private bool _isBusy;
        private bool _enableListView;
        private bool _noJourneys;
        private string _selectedStation;
        private string _selectedDestination = AnyDestination;
        private readonly ObservableCollection<string> _destinationOptions;
        public const string AnyDestination = "Any destination";
        private const string LastStationKey = "last_station";
        private string _noJourneyMsg;
        private string _refreshTime;
        private string _buttonText;
        private readonly Dictionary<string, (DateTime fetchedAt, List<StationData> data)> _journeyCache = new();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
        private ICommand? _refreshCommand;
        private ICommand? _searchServiceCommand;
        private ICommand? _openJourneyCommand;
        private readonly SemaphoreSlim _searchLock = new(1, 1);
        private bool _isRefreshing;
        private IDispatcherTimer? _countdownTimer;
        private int _refreshCountdown;
        private bool _isAutoRefreshEnabled;
        private bool _isRegisteredForAutoRefreshMessages;
        #endregion

        #region Constructor

        /// <param name="stationService">Optional: tests pass one backed by a fake HttpClient.</param>
        public AppHomeViewModel(DataService<List<Station>> stationListService, DataService<Settings> settingsService,
            StationService? stationService = null, RouteFilterService? routeFilter = null)
        {
            _stationListService = stationListService;
            _settings = settingsService;
            _stationService = stationService ?? new StationService();
            _routeFilter = routeFilter ?? RouteFilterService.Instance;

            // Null if the station list couldn't be loaded at startup; App shows an alert for that
            stationList = _stationListService.Data ?? [];

            var stationNames = stationList.Select(station => station.StationDesc)
                                          .Distinct()
                                          .OrderBy(name => name)
                                          .ToList();

            _stationNames = stationNames;
            _stationOptions = new ObservableCollection<string>(stationNames);
            _destinationOptions = new ObservableCollection<string>(stationNames.Prepend(AnyDestination));
            _trainJourneys = new ObservableCollection<TrainJourney>();
            _favouriteStations = new ObservableCollection<string>();
            _selectedStation = string.Empty;
            _noJourneyMsg = string.Empty;
            _refreshTime = string.Empty;
            _buttonText = AppStrings.FindServices;
        }

        #endregion

        #region Properties

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowStartHint)); }
        }

        public string NoJourneyMsg
        {
            get => _noJourneyMsg;
            set { _noJourneyMsg = value; OnPropertyChanged(); }
        }

        public string RefreshTime
        {
            get => _refreshTime;
            set { _refreshTime = value; OnPropertyChanged(); }
        }

        public string ButtonText
        {
            get => _buttonText;
            set { _buttonText = value; OnPropertyChanged(); }
        }

        public bool NoJourneyFound
        {
            get => _noJourneys;
            set { _noJourneys = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowStartHint)); }
        }

        /// <summary>True before any board has been loaded, to explain the empty screen.</summary>
        public bool ShowStartHint => !EnableListView && !NoJourneyFound && !IsBusy;

        public bool EnableListView
        {
            get => _enableListView;
            set { _enableListView = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowStartHint)); }
        }

        public bool IsFindNearbyStationChecked
        {
            get => _isFindNearbyStationChecked;
            set
            {
                if (_isFindNearbyStationChecked != value)
                {
                    _isFindNearbyStationChecked = value;
                    EnableListView = false;
                    HideSearchButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLocateStationChecked
        {
            get => _isLocateStationChecked;
            set
            {
                if (_isLocateStationChecked != value)
                {
                    _isLocateStationChecked = value;
                    IsStationListLoaded = value;
                    EnableListView = false;
                    ButtonText = AppStrings.FindServices;

                    if (_isLocateStationChecked)
                    {
                        StationOptions = new ObservableCollection<string>(_stationNames);
                    }

                    OnPropertyChanged();
                }
            }
        }

        public bool HideSearchButton
        {
            get => _hideSearchButton;
            set
            {
                if (_hideSearchButton != value)
                {
                    _hideSearchButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsStationListLoaded
        {
            get => _isStationListLoaded;
            set
            {
                if (_isStationListLoaded != value)
                {
                    _isStationListLoaded = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (!SetProperty(ref _isAutoRefreshEnabled, value))
                    return;

                OnPropertyChanged(nameof(RefreshStatusText));

                if (value)
                    StartCountdown();
                else
                    StopCountdown();
            }
        }

        public ObservableCollection<string> StationOptions
        {
            get => _stationOptions;
            private set
            {
                if (_stationOptions != value)
                {
                    _stationOptions = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> FavouriteStations
        {
            get => _favouriteStations;
            private set
            {
                if (_favouriteStations != value)
                {
                    _favouriteStations = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TrainJourney> TrainJourneys
        {
            get => _trainJourneys;
            private set
            {
                if (_trainJourneys != value)
                {
                    _trainJourneys = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedStation
        {
            get => _selectedStation;
            set
            {
                ButtonText = AppStrings.FindServices;
                EnableListView = false;
                _selectedStation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StationDisplay));
            }
        }

        public ObservableCollection<string> DestinationOptions => _destinationOptions;

        /// <summary>Direction chips for the current board; empty when it only has one direction.</summary>
        public ObservableCollection<DirectionChip> DirectionChips { get; } = new();

        public ICommand SelectDirectionCommand => new Command<DirectionChip>(chip =>
        {
            if (chip is null)
                return;
            _selectedDirection = chip.Name;
            foreach (var c in DirectionChips)
                c.IsSelected = c.Name == chip.Name;
            ApplyDirectionFilter();
        });

        public string StationDisplay => string.IsNullOrEmpty(_selectedStation) ? "Select a station" : _selectedStation;

        public string DestinationDisplay => $"Going to: {_selectedDestination}";

        /// <summary>
        /// Optional "going to" station: when set, only trains that stop there after the selected
        /// station are listed.
        /// </summary>
        public string SelectedDestination
        {
            get => _selectedDestination;
            set
            {
                var newValue = string.IsNullOrEmpty(value) ? AnyDestination : value;
                if (!SetProperty(ref _selectedDestination, newValue))
                    return;

                OnPropertyChanged(nameof(DestinationDisplay));

                // Re-filter the list that's already showing
                if (EnableListView || NoJourneyFound)
                    _ = ExecuteTrainServiceSearch(waitForLock: true);
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged();
                }
            }
        }

        public int RefreshCountdown
        {
            get => _refreshCountdown;
            set
            {
                if (SetProperty(ref _refreshCountdown, value))
                    OnPropertyChanged(nameof(RefreshStatusText));
            }
        }

        public string RefreshStatusText =>
                                IsAutoRefreshEnabled
                                    ? $"Refreshing in {RefreshCountdown} sec"
                                    : AppStrings.Refresh;

        #endregion

        #region Commands

        public ICommand SearchCommand => new Command(async () => await ExecuteLocationSearch());
        public ICommand SearchServiceCommand => _searchServiceCommand ??= new Command(async () => await ExecuteTrainServiceSearch());
        public ICommand RefreshCommand => _refreshCommand ??= new Command(async () => await RefreshJourneys());
        public ICommand OpenSettingsCommand => new Command(async () => await GoToSettings());
        public ICommand OpenJourneyCommand => _openJourneyCommand ??= new Command<TrainJourney>(async (journey) => await OpenJourney(journey));
        public ICommand SelectFavouriteCommand => new Command<string>(async (station) => await LoadStationAsync(station));
        // The two mode toggles are mutually exclusive (formerly a radio group)
        public ICommand SelectNearbyModeCommand => new Command(() =>
        {
            IsLocateStationChecked = false;
            IsFindNearbyStationChecked = true;
        });
        public ICommand SelectPickModeCommand => new Command(() =>
        {
            IsFindNearbyStationChecked = false;
            IsLocateStationChecked = true;
        });
        public ICommand ChooseStationCommand => new Command(async () => await ChooseStationAsync());
        public ICommand ChooseDestinationCommand => new Command(async () => await ChooseDestinationAsync());
        public ICommand ToggleTrackingCommand => new Command<TrainJourney>(async (journey) => await ToggleTracking(journey));

        #endregion

        #region Private Methods

        internal async Task ExecuteTrainServiceSearch(bool waitForLock = false, bool bypassCache = false)
        {
            if (string.IsNullOrEmpty(_selectedStation))
            {
                ShowError(AppMessages.SelectStationError);
                return;
            }

            if (waitForLock)
            {
                await _searchLock.WaitAsync();
            }
            else
            {
                if (!await _searchLock.WaitAsync(0))
                    return;
            }

            try
            {
                //if (IsBusy) return;
                IsBusy = true;

                var stationCode = BoardFormatting.FindStationCode(stationList, _selectedStation) ?? "";

                List<StationData> journeyList;
                DateTime? offlineDataFrom = null;

                if (!bypassCache && _journeyCache.TryGetValue(stationCode, out var cacheEntry) &&
                    DateTime.UtcNow - cacheEntry.fetchedAt < CacheDuration)
                {
                    // Use cached data
                    journeyList = cacheEntry.data;
                }
                else
                {
                    // Fetch fresh data
                    string? fetchError = null;
                    journeyList = await _stationService.GetTrainServicesAsync(
                        _settings.Data,
                        stationCode,
                        error => fetchError = error);

                    if (fetchError is null)
                    {
                        _journeyCache[stationCode] = (DateTime.UtcNow, journeyList);
                    }
                    else if (_journeyCache.TryGetValue(stationCode, out var lastGood))
                    {
                        // Offline or the API is down: keep showing the last board we got, marked
                        // with its age, instead of an alert on every auto-refresh.
                        journeyList = lastGood.data;
                        offlineDataFrom = lastGood.fetchedAt.ToLocalTime();
                    }
                    else
                    {
                        ShowError(IsOffline()
                            ? "You're offline. Connect to the internet to see live train times."
                            : fetchError);
                    }
                }

                var destination = GetDestinationStation();
                var destinationCode = destination?.StationCode;
                var origin = stationList.FirstOrDefault(s => s.StationCode == stationCode);
                if (destination is not null && origin is not null && destination.StationCode != origin.StationCode)
                {
                    // Show matching trains as each route is confirmed instead of after all of them
                    _allJourneys = new List<TrainJourney>();
                    TrainJourneys = new ObservableCollection<TrainJourney>();
                    NoJourneyFound = false;
                    EnableListView = true;

                    journeyList = await _routeFilter.FilterCallingAtAsync(
                        _settings.Data, journeyList, origin, destination, _stationService,
                        onKept: train => RunOnUiThread(() => InsertByDueTime(ToJourney(train))));
                }

                if (journeyList.Count > 0)
                {
                    NoJourneyFound = false;
                    EnableListView = true;

                    journeyList.Sort((x, y) => x.Duein.CompareTo(y.Duein));

                    _allJourneys = journeyList.Select(ToJourney).ToList();
                    UpdateDirectionChips();
                    ApplyDirectionFilter();
                }
                else
                {
                    EnableListView = false;
                    NoJourneyFound = true;
                    ButtonText = AppStrings.FindServices;
                    NoJourneyMsg = destinationCode is null
                        ? string.Format(AppMessages.NoJourneysForStation, _selectedStation)
                        : $"No trains from {_selectedStation} to {_selectedDestination} right now.";
                }

                SafePreferences.Set(LastStationKey, _selectedStation);

                RefreshTime = offlineDataFrom is { } dataFrom
                    ? $"⚠ Offline · showing times from {dataFrom:hh:mm tt}"
                    : string.Format(AppMessages.CurrentStatusAsOf, DateTime.Now.ToString("hh:mm:ss tt"));
            }
            catch (Exception ex)
            {
                ShowError(string.Format(AppMessages.TrainServiceError, ex.Message));
            }
            finally
            {
                IsBusy = false;
                _searchLock.Release();
            }
        }

        private void InsertByDueTime(TrainJourney journey)
        {
            lock (_journeysLock)
            {
                _allJourneys.Add(journey);
                if (!MatchesDirection(journey))
                    return;

                var index = 0;
                while (index < TrainJourneys.Count && TrainJourneys[index].DueMinutes <= journey.DueMinutes)
                    index++;
                TrainJourneys.Insert(index, journey);
            }
        }

        private bool MatchesDirection(TrainJourney journey) =>
            _selectedDirection == DirectionChip.All || journey.Direction == _selectedDirection;

        private void ApplyDirectionFilter()
        {
            lock (_journeysLock)
            {
                TrainJourneys = new ObservableCollection<TrainJourney>(
                    _allJourneys.Where(MatchesDirection).OrderBy(j => j.DueMinutes));
            }
        }

        /// <summary>Rebuilds the chips from the board's directions, keeping the current choice if it still exists.</summary>
        private void UpdateDirectionChips()
        {
            var directions = _allJourneys.Select(j => j.Direction)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d.StartsWith("To ") ? 1 : 0)
                .ThenBy(d => d)
                .ToList();

            if (!directions.Contains(_selectedDirection))
                _selectedDirection = DirectionChip.All;

            DirectionChips.Clear();
            if (directions.Count < 2)
                return;

            foreach (var name in directions.Prepend(DirectionChip.All))
                DirectionChips.Add(new DirectionChip(name) { IsSelected = name == _selectedDirection });
        }

        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
                action(); // unit tests: no UI thread
            else
                dispatcher.Dispatch(action);
        }

        private static TrainJourney ToJourney(StationData journey) => new()
                        {
                            TrainCode = journey.Traincode,
                            TrainDate = IrishTime.ParseTrainDate(journey.Traindate),
                            Origin = journey.Origin,
                            Destination = journey.Destination,
                            CurrentStatus = !string.IsNullOrEmpty(journey.Lastlocation)
                                ? journey.Lastlocation
                                : journey.Status,
                            LastLocation = journey.Lastlocation,
                            DueIn = $"{journey.Duein} min{(journey.Duein > 1 ? "s" : "")}",
                            DueText = journey.Duein <= 0 ? "Due" : $"{journey.Duein} min",
                            LateBadgeColor = journey.Late <= 0 ? Color.FromArgb("#2916A34A") : Color.FromArgb("#29DC2626"),
                            ExpectedTimeLabel = BoardFormatting.DescribeTime(journey).Label,
                            ExpectedArrival = BoardFormatting.DescribeTime(journey).Time,
                            Late = Math.Abs(journey.Late).ToString(),
                            LateDisplay = journey.Late == 0
                            ? "On time"
                            : journey.Late < 0
                                        ? $"Early by {Math.Abs(journey.Late)} min{(Math.Abs(journey.Late) > 1 ? "s" : "")}"
                                        : $"Delayed by {journey.Late} min{(journey.Late > 1 ? "s" : "")}",
                            LateColor = journey.Late <= 0 ? Color.FromArgb("#16A34A") : Color.FromArgb("#DC2626"),
                            DueMinutes = journey.Duein,
                            Direction = journey.Direction.Trim(),
                            IsDart = journey.Traintype.Trim().Equals("DART", StringComparison.OrdinalIgnoreCase),
                            IsTracking = JourneyTrackingService.Instance.IsTracking(journey.Traincode)
                        };

        private async Task ChooseStationAsync()
        {
            var nav = Application.Current?.Windows[0].Page?.Navigation;
            if (nav is null)
                return;

            var station = await StationSearchPage.PickAsync(nav, "Select station", StationOptions);
            if (station is null)
                return;

            SelectedStation = station;
            await ExecuteTrainServiceSearch(waitForLock: true);
        }

        private async Task ChooseDestinationAsync()
        {
            var nav = Application.Current?.Windows[0].Page?.Navigation;
            if (nav is null)
                return;

            var destination = await StationSearchPage.PickAsync(nav, "Going to", DestinationOptions);
            if (destination is not null)
                SelectedDestination = destination;
        }

        private static bool IsOffline()
        {
            try
            {
                return Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
            }
            catch (NotImplementedException)
            {
                return false;
            }
        }

        private Station? GetDestinationStation()
        {
            if (string.IsNullOrEmpty(_selectedDestination) || _selectedDestination == AnyDestination)
                return null;

            return stationList.FirstOrDefault(s => s.StationDesc.Equals(_selectedDestination, StringComparison.OrdinalIgnoreCase));
        }

        private async Task ExecuteLocationSearch()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                if (IsFindNearbyStationChecked)
                {
                    PlacesService _placesService = new PlacesService(_settings, _stationListService);
                    var stationList = await _placesService.GetNearbyStationList(ShowError);

                    if (stationList.Count > 0)
                    {
                        StationOptions = new ObservableCollection<string>(stationList);
                        IsStationListLoaded = true;
                        HideSearchButton = false;
                        ButtonText = AppStrings.FindServices;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(string.Format(AppMessages.NearbyStationError, ex.Message));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshJourneys()
        {
            if (string.IsNullOrEmpty(SelectedStation))
            {
                IsRefreshing = false;
                return;
            }

            try
            {
                IsRefreshing = true;
                await ExecuteTrainServiceSearch(waitForLock: true, bypassCache: true);
            }
            catch (Exception ex)
            {
                ShowError(string.Format(AppMessages.TrainServiceError, ex.Message));
            }
            finally
            {
                IsRefreshing = false;
            }

        }

        private async Task GoToSettings()
        {
            var nav = Application.Current?.Windows[0].Page?.Navigation;

            if (nav is null)
                return;

            var settingsPage = MauiProgram.Current?.GetService<AppSettings>();
            if (settingsPage is null)
                return;

            await nav.PushAsync(settingsPage);
        }

        private async Task ToggleTracking(TrainJourney journey)
        {
            if (journey is null)
                return;

            if (JourneyTrackingService.Instance.IsTracking(journey.TrainCode))
            {
                await JourneyTrackingService.Instance.StopTracking(journey.TrainCode);
                journey.IsTracking = false;
            }
            else
            {
                if (!await NotificationPermissionHelper.EnsureAsync(
                        "Journey tracking shows the train's live position and delay in a notification. " +
                        "Allow notifications for iRailTracker in its settings to use it."))
                    return;

                await JourneyTrackingService.Instance.StartTracking(journey, _settings.Data);
                journey.IsTracking = true;
            }
        }

        private async Task OpenJourney(TrainJourney journey)
        {
            if (journey is null)
                return;

            var nav = Application.Current?.Windows[0].Page?.Navigation;

            if (nav is null)
                return;

            await nav.PushAsync(new TrainLiveStatusPage(journey, stationList));
        }

        public async void Receive(AutoRefreshMessage message)
        {
            if (IsBusy || IsRefreshing) return;
            if (!EnableListView) return;

            if (!IsAutoRefreshEnabled)
                IsAutoRefreshEnabled = true;

            RefreshCountdown = GetRefreshIntervalSeconds();

            try
            {
                await ExecuteTrainServiceSearch();
            }
            catch (Exception ex)
            {
                ShowError(string.Format(AppMessages.TrainServiceError, ex.Message));
            }
        }

        public void Receive(AutoRefreshSettingsChangedMessage message)
        {
            var intervalSeconds = message.IntervalSeconds < 1 ? 1 : message.IntervalSeconds;

            if (!message.Enabled)
            {
                IsAutoRefreshEnabled = false;
                RefreshCountdown = 0;
                return;
            }

            if (!EnableListView)
                return;

            if (!IsAutoRefreshEnabled)
                IsAutoRefreshEnabled = true;

            RefreshCountdown = intervalSeconds;
        }

        public void OnAppearing()
        {
            // Tracking may have stopped while another page was open (notification button, or the
            // train arrived); messages aren't received then, so re-sync the bells.
            SyncTrackingState();

            if (_isRegisteredForAutoRefreshMessages)
                return;

            WeakReferenceMessenger.Default.RegisterAll(this);
            _isRegisteredForAutoRefreshMessages = true;
        }

        public void OnDisappearing()
        {
            IsAutoRefreshEnabled = false;

            if (_isRegisteredForAutoRefreshMessages)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                _isRegisteredForAutoRefreshMessages = false;
            }
        }

        public async Task LoadFavouriteStationAsync()
        {
            FavouriteStations = new ObservableCollection<string>(FavouriteStationsStore.Load());

            // First favourite, otherwise the station that was open last time
            var station = FavouriteStations.FirstOrDefault()
                          ?? SafePreferences.Get(LastStationKey, string.Empty);

            if (string.IsNullOrEmpty(station))
                return;

            await LoadStationAsync(station);
        }

        private async Task LoadStationAsync(string? station)
        {
            if (string.IsNullOrEmpty(station) || !_stationNames.Contains(station))
                return;

            IsLocateStationChecked = true;
            SelectedStation = station;
            await ExecuteTrainServiceSearch(waitForLock: true);
        }

        public void Receive(FavouriteStationsChangedMessage message)
        {
            FavouriteStations = new ObservableCollection<string>(message.Stations);
        }

        public event EventHandler<TrainJourney>? ScrollToJourneyRequested;

        public void Receive(TrackingRestoredMessage message) =>
            MainThread.BeginInvokeOnMainThread(SyncTrackingState);

        private void SyncTrackingState()
        {
            foreach (var journey in TrainJourneys)
                journey.IsTracking = JourneyTrackingService.Instance.IsTracking(journey.TrainCode);
        }

        public void Receive(TrackingStoppedMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var journey in TrainJourneys.Where(j => j.TrainCode == message.TrainCode))
                    journey.IsTracking = false;
            });
        }

        public void Receive(NavigateToTrackedJourneyMessage message)
        {
            var journey = TrainJourneys.FirstOrDefault(j => j.TrainCode == message.TrainCode);

            if (journey is not null)
                ScrollToJourneyRequested?.Invoke(this, journey);
        }

        private static int GetRefreshIntervalSeconds()
        {
            var interval = Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30);
            return interval < 1 ? 1 : interval;
        }

        private void StartCountdown()
        {
            StopCountdown();

            RefreshCountdown = GetRefreshIntervalSeconds();

            _countdownTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_countdownTimer == null) return;

            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += OnCountdownTick;
            _countdownTimer.Start();
        }

        private void StopCountdown()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= OnCountdownTick;
                _countdownTimer = null;
            }
        }

        private void OnCountdownTick(object? sender, EventArgs e)
        {
            if (RefreshCountdown > 0)
                RefreshCountdown--;
        }

        #endregion
    }
}