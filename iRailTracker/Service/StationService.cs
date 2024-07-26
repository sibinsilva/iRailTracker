using iRailTracker.Model;
using RestSharp;
using System.Xml.Serialization;

namespace iRailTracker.Service
{
    public class StationService
    {
        private readonly RestClient _restClient;

        public StationService()
        {
           _restClient = new RestClient();
        }

        public async Task<List<Station>> GetAllStationsAsync(Settings settings)
        {
            string url = settings.GetAllStationsUrl;
            var request = new RestRequest(url, Method.Get);

            var response = await _restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Error fetching data from {url}. Status: {response.StatusCode}, Message: {response.ErrorMessage}");
            }

            string responseXml = response.Content;

            XmlSerializer serializer = new XmlSerializer(typeof(StationCollection));

            using (StringReader reader = new StringReader(responseXml))
            {
                StationCollection stationCollection = (StationCollection)serializer.Deserialize(reader);
                return stationCollection.Stations;
            }
        }

        public async Task<List<StationData>> GetTrainServicesAsync(DataService<Settings> settings, string stationCode)
        {
            string url = settings.Data.GetServiceByStationCodeURL + stationCode;
            var request = new RestRequest(url, Method.Get);

            var response = await _restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Error fetching data from {url}. Status: {response.StatusCode}, Message: {response.ErrorMessage}");
            }

            string responseXml = response.Content;

            XmlSerializer serializer = new XmlSerializer(typeof(TrainService));

            using (StringReader reader = new StringReader(responseXml))
            {
                TrainService trainService = (TrainService)serializer.Deserialize(reader);
                return trainService.ObjStationData;
            }
        }

    }
}
