using iRailTracker.Model;
using RestSharp;
using System.Xml;
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

        public async Task<List<Station>> GetAllStationsAsync(Settings settings, Action<string> errorCallback)
        {
            string url = settings.GetAllStationsUrl;
            var request = new RestRequest(url, Method.Get);

            try
            {
                var response = await _restClient.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    var errorMsg = $"Error fetching data from {url}. Status: {response.StatusCode}, Message: {response.ErrorMessage}";
                    errorCallback?.Invoke(errorMsg);
                    throw new Exception(errorMsg);
                }

                string responseXml = response.Content;

                XmlSerializer serializer = new XmlSerializer(typeof(StationCollection));

                using (StringReader reader = new StringReader(responseXml))
                {
                    StationCollection stationCollection = (StationCollection)serializer.Deserialize(reader);
                    return stationCollection.Stations;
                }
            }
            catch (XmlException xmlEx)
            {
                var errorMsg = $"XML deserialization error: {xmlEx.Message}";
                errorCallback?.Invoke(errorMsg);
                throw new Exception(errorMsg);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Unexpected error: {ex.Message}";
                errorCallback?.Invoke(errorMsg);
                throw new Exception(errorMsg);
            }
        }

        public async Task<List<StationData>> GetTrainServicesAsync(Settings settings, string stationCode, Action<string> errorCallback)
        {
            string url = settings.GetServiceByStationCodeURL + stationCode;
            var request = new RestRequest(url, Method.Get);

            try
            {
                var response = await _restClient.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    var errorMsg = $"Error fetching data from {url}. Status: {response.StatusCode}, Message: {response.ErrorMessage}";
                    errorCallback?.Invoke(errorMsg);
                    throw new Exception(errorMsg);
                }

                string responseXml = response.Content;

                XmlSerializer serializer = new XmlSerializer(typeof(TrainService));

                using (StringReader reader = new StringReader(responseXml))
                {
                    TrainService trainService = (TrainService)serializer.Deserialize(reader);
                    return trainService.ObjStationData;
                }
            }
            catch (XmlException xmlEx)
            {
                var errorMsg = $"XML deserialization error: {xmlEx.Message}";
                errorCallback?.Invoke(errorMsg);
                throw new Exception(errorMsg);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Unexpected error: {ex.Message}";
                errorCallback?.Invoke(errorMsg);
                throw new Exception(errorMsg);
            }
        }
    }
}
