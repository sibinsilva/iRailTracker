using Newtonsoft.Json;
using RestSharp;
using static iRailTracker.Model.NearbyStation;

namespace iRailTracker.Service
{
    public class GooglePlacesService
    {
        private readonly DataService<Settings> _settings;

        public GooglePlacesService(DataService<Settings> settings)
        {
            _settings = settings;
        }

        public async Task<List<string>> GetLocationAsync(Action<string> errorCallback)
        {
            List<string> stations = new List<string>();

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
                    var nearbyStations = await GetNearbyStation(location.Latitude.ToString(), location.Longitude.ToString());
                    NearbyStations stationItems = JsonConvert.DeserializeObject<NearbyStations>(nearbyStations);

                    if (stationItems != null)
                    {
                        foreach (var station in stationItems.places)
                        {
                            stations.Add(station.name);
                        }
                    }
                    else
                    {
                        errorCallback?.Invoke("No stations found in the response.");
                    }
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

            return stations;
        }

        private async Task<string> GetNearbyStation(string latitude, string longitude)
        {
            try
            {
                string url = _settings.Data.GetNearbyStationUrl;
                var client = new RestClient(url);
                var request = new RestRequest
                {
                    Method = Method.Post
                };
                request.AddHeader("Content-Type", "application/json");

                var body = new
                {
                    latitude,
                    longitude
                };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    return response.Content;
                }
                else
                {
                    throw new Exception($"Error fetching data from {url}. Status: {response.StatusCode}, Message: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching nearby stations: {ex.Message}");
            }
        }
    }
}
