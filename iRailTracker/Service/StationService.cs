using iRailTracker.Model;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace iRailTracker.Service
{
    /// <summary>
    /// Client for the Irish Rail realtime API. All instances share one HttpClient, so creating a
    /// StationService is cheap and doesn't open new connections.
    /// Every method reports failures once through <c>errorCallback</c> and returns an empty list;
    /// none of them throw.
    /// </summary>
    public class StationService
    {
        private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(20) };

        private readonly HttpClient _httpClient;

        public StationService() : this(SharedClient)
        {
        }

        public StationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StationCollection))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Station))]
        public async Task<List<Station>> GetAllStationsAsync(Settings settings, Action<string>? errorCallback)
        {
            var result = await FetchXmlAsync<StationCollection>(settings.GetAllStationsUrl, errorCallback);
            return result?.Stations ?? [];
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TrainService))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StationData))]
        public async Task<List<StationData>> GetTrainServicesAsync(Settings settings, string stationCode, Action<string>? errorCallback)
        {
            var url = settings.GetServiceByStationCodeURL + Uri.EscapeDataString(stationCode);
            var result = await FetchXmlAsync<TrainService>(url, errorCallback);
            return result?.ObjStationData ?? [];
        }

        /// <param name="trainDate">The date the train's service started (the station board's
        /// Traindate), not today's date: trains running past midnight keep their original date.</param>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TrainMovementsResponse))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TrainMovement))]
        public async Task<List<TrainMovement>> GetTrainMovementsAsync(Settings settings, string trainCode, DateTime trainDate, Action<string>? errorCallback)
        {
            var url = $"{settings.GetTrainMovementsUrl}?TrainId={Uri.EscapeDataString(trainCode.Trim())}" +
                      $"&TrainDate={Uri.EscapeDataString(IrishTime.FormatTrainDate(trainDate))}";
            var result = await FetchXmlAsync<TrainMovementsResponse>(url, errorCallback);
            return result?.Movements ?? [];
        }

        /// <summary>Deserializes an Irish Rail API XML response. Returns null if it isn't valid.</summary>
        public static T? ParseXml<T>(string xml) where T : class
        {
            try
            {
                using var reader = new StringReader(xml);
                return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
            }
            catch (InvalidOperationException)
            {
                // XmlSerializer wraps malformed or unexpected XML in InvalidOperationException
                return null;
            }
        }

        private async Task<T?> FetchXmlAsync<T>(string url, Action<string>? errorCallback) where T : class
        {
            string xml;

            try
            {
                using var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    errorCallback?.Invoke($"Error fetching data from {url}. Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                xml = await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException)
            {
                errorCallback?.Invoke($"Request timed out: {url}");
                return null;
            }
            catch (Exception ex)
            {
                // On Android a DNS failure in airplane mode can surface as a Java exception
                // (e.g. "Unable to resolve host") rather than HttpRequestException, so treat any
                // failure here as a network error instead of letting it reach the UI as a crash/alert.
                errorCallback?.Invoke($"Network error fetching {url}: {ex.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(xml))
            {
                errorCallback?.Invoke($"Empty response from {url}.");
                return null;
            }

            var result = ParseXml<T>(xml);
            if (result is null)
                errorCallback?.Invoke($"Unexpected response format from {url}.");

            return result;
        }
    }
}
