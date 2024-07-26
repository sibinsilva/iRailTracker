using iRailTracker.Model;
using iRailTracker.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace iRailTracker.ViewModel
{
    public class AppHomeViewModel : INotifyPropertyChanged
    {
        private readonly DataService<List<Station>> _stationListService;
        private readonly DataService<Settings> _settings;
        private readonly List<Station> stationList;
        private bool _isFindNearbyStationChecked;
        private bool _isLocateStationChecked;
        private bool _isStationListLoaded;
        private bool _hideSearchButton;
        private ObservableCollection<string> _stationOptions;
        private ObservableCollection<TrainJourney> _trainJourneys;
        private string _selectedStation;
        private readonly List<string> _stationNames = new List<string>();
        private bool _isBusy;
        private bool _enableListView;
        private bool _noJourneys;
        private string _noJourneyMsg;

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
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
        public string NoJourneyMsg
        {
            get => _noJourneyMsg;
            set
            {
                _noJourneyMsg = value;
                OnPropertyChanged();
            }
        }

        public bool NoJourneyFound
        {
            get => _noJourneys;
            set
            {
                _noJourneys = value;
                OnPropertyChanged();
            }
        }

        public bool EnableListView
        {
            get => _enableListView;
            set
            {
                _enableListView = value;
                OnPropertyChanged();
            }
        }

        // Property to determine if 'Locate Nearby Station' is selected
        public bool IsFindNearbyStationChecked
        {
            get => _isFindNearbyStationChecked;
            set
            {
                if (_isFindNearbyStationChecked != value)
                {
                    _isFindNearbyStationChecked = value;
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

        // Property to determine if 'Search for Station' is selected
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

        // Property for the dropdown options
        public ObservableCollection<string> StationOptions
        {
            get => _stationOptions;
            private  set
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

        // Property for the selected item in the dropdown
        public string SelectedStation
        {
            get => _selectedStation;
            set
            {
                if (_selectedStation != value)
                {
                    _selectedStation = value;
                    OnPropertyChanged();
                    // Optionally, update other properties or logic based on the selected station
                }
            }
        }


        // Command for the search button
        public ICommand SearchCommand => new Command(async () => await ExecuteLocationSearch());
        public ICommand SearchServiceCommand => new Command(async () => await ExecuteTrainServiceSearch());

        private async Task ExecuteTrainServiceSearch()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var stationNames = _selectedStation
                                    .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .ToList();

                var matchingStation = stationList.FirstOrDefault(station =>
                    stationNames.Any(name => station.StationDesc.Contains(name, StringComparison.OrdinalIgnoreCase))
                );

                var stationCode = matchingStation != null ? matchingStation.StationCode : null;
                StationService trainService = new StationService();
                var journeyList = await trainService.GetTrainServicesAsync(_settings, stationCode);
                if (journeyList.Count > 0)
                {
                    NoJourneyFound = false;
                    EnableListView = true;
                    TrainJourneys = new ObservableCollection<TrainJourney>(
                    journeyList.Select(journey => new TrainJourney
                    {
                        Origin = journey.Origin,
                        Destination = journey.Destination,
                        CurrentStatus = journey.Status,
                        DueIn = journey.Duein.ToString(),
                        ExpectedArrival = journey.Exparrival,
                        Late = journey.Late.ToString()
                    })
                    );
                }
                else
                {
                    EnableListView = false;
                    NoJourneyFound = true;
                    NoJourneyMsg = $"No Journeys are Available at the moment for {_selectedStation} station.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteLocationSearch()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                if (IsFindNearbyStationChecked)
                {
                    GooglePlacesService _placesService = new GooglePlacesService(_settings);
                    var stationList = await _placesService.GetLocationAsync();
                    if (stationList.Count > 0)
                    {
                        StationOptions = new ObservableCollection<string>(stationList);
                        IsStationListLoaded = true;
                        HideSearchButton = false;
                    }

                }
            }
            finally
            {
                IsBusy = false;
            }
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
