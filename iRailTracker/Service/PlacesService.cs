using iRailTracker.Model;

namespace iRailTracker.Service
{
    public class PlacesService
    {
        private readonly DataService<Settings> _settings;
        private readonly DataService<List<Station>> _stationListService;

        public PlacesService(DataService<Settings> settings, DataService<List<Station>> stationListService)
        {
            _settings = settings;
            _stationListService = stationListService;
        }

        private async Task<Coordinate> GetLocationAsync(Action<string> errorCallback)
        {
            Coordinate? _location = null;
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.High,
                        Timeout = TimeSpan.FromSeconds(30)
                    });
                }

                if (location != null)
                {
                    _location = new Coordinate(location.Latitude, location.Longitude);
                }
                else
                {
                    errorCallback?.Invoke("No GPS data available.");
                }
            }
            catch (Exception ex)
            {
                errorCallback?.Invoke($"An error occurred while getting location: {ex.Message}");
            }
            return _location;
        }

        private double CalculateDistance(Coordinate c1, Coordinate c2)
        {
            // Haversine formula to calculate distance in meters
            const double R = 6371000; // Radius of the Earth in meters
            double lat1 = c1.Latitude * (Math.PI / 180);
            double lon1 = c1.Longitude * (Math.PI / 180);
            double lat2 = c2.Latitude * (Math.PI / 180);
            double lon2 = c2.Longitude * (Math.PI / 180);

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; // Distance in meters
        }

        private List<string> GetNearestStationNames(Coordinate target, List<Station> stations)
        {
            // Convert stations to coordinates with their names
            var stationCoordinates = stations.Select(station => new
            {
                Station = station,
                Coordinate = new Coordinate(station.StationLatitude, station.StationLongitude)
            }).ToList();

            // Use a HashSet to track unique coordinates
            var uniqueCoordinates = new HashSet<(double, double)>();

            // Find the nearest coordinates
            var nearestCoordinates = stationCoordinates
                    .OrderBy(sc => CalculateDistance(target, sc.Coordinate))
                    .Where(sc => uniqueCoordinates.Add((sc.Station.StationLatitude, sc.Station.StationLongitude)))
                    .Select(sc => sc.Station.StationDesc)
                    .Where(desc => !string.IsNullOrWhiteSpace(desc))
                    .Take(5)
                    .Select(desc => desc!) 
                    .ToList();

            return nearestCoordinates;
        }



        public async Task<List<string>> GetNearbyStationList(Action<string> errorCallback)
        {
            List<string> stations = new List<string>();
            try
            {
                var _currentLocationcords = await GetLocationAsync(errorCallback);
                var _nearbyStations = GetNearestStationNames(_currentLocationcords, _stationListService.Data);

                if (_nearbyStations.Count == 0)
                {
                    errorCallback?.Invoke("No stations found in the response.");
                }
                else
                {
                    stations = _nearbyStations;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching nearby stations: {ex.Message}");
            }
            return stations;
        }
    }
}
