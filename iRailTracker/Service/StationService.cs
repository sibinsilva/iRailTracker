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

        public async Task<List<Station>> GetAllStationsAsync(Settings settings, Action<string>? errorCallback)
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

                string? responseXml = response.Content;

                if (string.IsNullOrEmpty(responseXml))
                {
                    errorCallback?.Invoke($"Empty response from {url}.");
                    throw new Exception($"Empty response from {url}.");
                }

                XmlSerializer serializer = new XmlSerializer(typeof(StationCollection));

                using (StringReader reader = new StringReader(responseXml))
                {
                    StationCollection? stationCollection = serializer.Deserialize(reader) as StationCollection;
                    return stationCollection?.Stations ?? [];
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

        public async Task<List<StationData>> GetTrainServicesAsync(Settings settings, string stationCode, Action<string>? errorCallback)
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

                string? responseXml = response.Content;

                if (string.IsNullOrEmpty(responseXml))
                {
                    errorCallback?.Invoke($"Empty response from {url}.");
                    throw new Exception($"Empty response from {url}.");
                }

                XmlSerializer serializer = new XmlSerializer(typeof(TrainService));

                using (StringReader reader = new StringReader(responseXml))
                {
                    TrainService? trainService = serializer.Deserialize(reader) as TrainService;
                    return trainService?.ObjStationData ?? [];
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

        public async Task<List<TrainMovement>> GetTrainMovementsAsync(Settings settings, string trainCode, Action<string>? errorCallback)
        {
            string url = $"{settings.GetTrainMovementsUrl}?TrainId={trainCode}&TrainDate={DateTime.Now:dd MMM yyyy}";
            var request = new RestRequest(url, Method.Get);

            try
            {
                var response = await _restClient.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    errorCallback?.Invoke($"Error fetching movements from {url}. Status: {response.StatusCode}");
                    return [];
                }

                string? responseXml = response.Content;

                if (string.IsNullOrEmpty(responseXml))
                {
                    errorCallback?.Invoke($"Empty response from {url}.");
                    return [];
                }

                XmlSerializer serializer = new XmlSerializer(typeof(TrainMovementsResponse));

                using (StringReader reader = new StringReader(responseXml))
                {
                    TrainMovementsResponse? result = serializer.Deserialize(reader) as TrainMovementsResponse;
                    return result?.Movements ?? [];
                }
            }
            catch (XmlException xmlEx)
            {
                errorCallback?.Invoke($"XML deserialization error: {xmlEx.Message}");
                return [];
            }
            catch (Exception ex)
            {
                errorCallback?.Invoke($"Unexpected error: {ex.Message}");
                return [];
            }
        }
    }
}
