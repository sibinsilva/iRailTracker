using iRailTracker.Model;
using iRailTracker.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Graphics;

namespace iRailTracker.ViewModel
{
    public class AppHomeViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DataService<List<Station>> _stationListService;
        private readonly DataService<Settings> _settings;
        private ObservableCollection<string> _stationOptions;
        private ObservableCollection<TrainJourney> _trainJourneys;
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
        private string _noJourneyMsg;
        private string _refreshTime;
        private string _buttonText;

        #endregion

        #region Constructor

        public AppHomeViewModel(DataService<List<Station>> stationListService, DataService<Settings> settingsService)
        {
            _stationListService = stationListService;
            _settings = settingsService;

            stationList = _stationListService.Data;

            var stationNames = stationList.Select(station => station.StationDesc)
                                          .Distinct()
                                          .OrderBy(name => name)
                                          .ToList();

            _stationNames = stationNames;
            _stationOptions = new ObservableCollection<string>(stationNames);
            _trainJourneys = new ObservableCollection<TrainJourney>();
            _selectedStation = string.Empty;
            _noJourneyMsg = string.Empty;
            _refreshTime = string.Empty;
            _buttonText = "Find Services";
        }

        #endregion

        #region Properties

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
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
            set { _noJourneys = value; OnPropertyChanged(); }
        }

        public bool EnableListView
        {
            get => _enableListView;
            set { _enableListView = value; OnPropertyChanged(); }
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
                    ButtonText = "Find Services";

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
                if (_selectedStation != value)
                {
                    ButtonText = "Find Services";
                    EnableListView = false;
                    _selectedStation = value;
                    OnPropertyChanged();
                }
                else
                {
                    ButtonText = "Refresh Journeys";
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler<string>? ErrorOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Commands

        public ICommand SearchCommand => new Command(async () => await ExecuteLocationSearch());
        public ICommand SearchServiceCommand => new Command(async () => await ExecuteTrainServiceSearch());

        #endregion

        #region Private Methods

        private async Task ExecuteTrainServiceSearch()
        {
            if (string.IsNullOrEmpty(_selectedStation))
            {
                ShowError("You haven't selected a train station to view the available journeys.");
                return;
            }

            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var stationNames = _selectedStation
                    .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                var matchingStation = stationList.FirstOrDefault(station =>
                    stationNames.Any(name => station.StationDesc.Contains(name, StringComparison.OrdinalIgnoreCase)));

                var stationCode = matchingStation != null ? matchingStation.StationCode : "";

                StationService trainService = new StationService();
                var journeyList = await trainService.GetTrainServicesAsync(_settings.Data, stationCode, ShowError);

                if (journeyList.Count > 0)
                {
                    NoJourneyFound = false;
                    EnableListView = true;
                    ButtonText = "Refresh Journeys";

                    journeyList.Sort((x, y) => x.Duein.CompareTo(y.Duein));

                    TrainJourneys = new ObservableCollection<TrainJourney>(
                        journeyList.Select(journey => new TrainJourney
                        {
                            Origin = journey.Origin,
                            Destination = journey.Destination,
                            CurrentStatus = !string.IsNullOrEmpty(journey.Lastlocation)
                                ? journey.Lastlocation
                                : journey.Status,
                            DueIn = $"{journey.Duein} min{(journey.Duein > 1 ? "s" : "")}",
                            ExpectedArrival = DateTime.ParseExact(journey.Exparrival, "HH:mm", null)
                                .ToString("hh:mm tt").ToLower(),
                            Late = Math.Abs(journey.Late).ToString(),
                            LateDisplay = journey.Late == 0
                            ? "On time"
                            : journey.Late < 0
                                        ? $"Early by {Math.Abs(journey.Late)} min{(Math.Abs(journey.Late) > 1 ? "s" : "")}"
                                        : $"Delayed by {journey.Late} min{(journey.Late > 1 ? "s" : "")}",
                            LateColor = journey.Late <= 0 ? Colors.Green : Colors.OrangeRed
                        }));
                }
                else
                {
                    EnableListView = false;
                    NoJourneyFound = true;
                    ButtonText = "Find Services";
                    NoJourneyMsg = $"Currently, there are no journeys available for {_selectedStation} station.";
                }

                RefreshTime = $"Current Status as of {DateTime.Now:hh:mm tt}";
            }
            catch (Exception ex)
            {
                ShowError($"An unexpected error occurred while searching for train services: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
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
                        ButtonText = "Find Services";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"An unexpected error occurred while searching for nearby stations: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowError(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}