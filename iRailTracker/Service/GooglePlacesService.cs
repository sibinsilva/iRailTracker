using iRailTracker.Model;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static iRailTracker.Model.NearbyStation;

namespace iRailTracker.Service
{
    public class GooglePlacesService
    {
        private DataService<Settings> settings;

        public GooglePlacesService(DataService<Settings> settings)
        {
            this.settings = settings;
        }

        public async Task<List<string>> GetLocationAsync()
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

                    foreach (var station in stationItems.places)
                    {
                        stations.Add(station.name);
                    }
                }
                else
                {
                    Console.WriteLine("No GPS data available.");
                }
            }
            catch (Exception ex)
            {
                // Unable to get location
            }
            return stations;
        }

        public async Task<string> GetNearbyStation(string devicelatitude, string devicelongitude)
        {
            string stationList = null;
            try
            {
                string url = settings.Data.GetNearbyStationUrl;
                var client = new RestClient(url);
                var request = new RestRequest();
                request.Method = Method.Post;
                request.AddHeader("Content-Type", "application/json");

                var body = new
                {
                    latitude = devicelatitude,
                    longitude = devicelongitude
                };

                request.AddJsonBody(body);

                RestResponse response = client.Execute(request);
                stationList = response.Content;
            }
            catch (Exception)
            {

                throw;
            }
            return stationList;
        }
    }
}
