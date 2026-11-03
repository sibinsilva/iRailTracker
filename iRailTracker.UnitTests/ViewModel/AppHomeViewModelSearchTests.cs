using System.Net;
using iRailTracker.Model;
using iRailTracker.Service;
using iRailTracker.ViewModel;

namespace iRailTracker.UnitTests.ViewModel;

/// <summary>
/// End-to-end tests of the Home search path (board fetch, station matching, offline fallback,
/// destination filter) against a fake Irish Rail API.
/// </summary>
[Collection(iRailTracker.UnitTests.Service.IrishClockCollection.Name)]
public class AppHomeViewModelSearchTests
{
    private const string Ns = "http://api.irishrail.ie/realtime/";

    private static string Board(params (string code, string destination, int duein)[] trains) =>
        $"<ArrayOfObjStationData xmlns=\"{Ns}\">" +
        string.Concat(trains.Select(t =>
            $"<objStationData><Traincode>{t.code}</Traincode><Traindate>{IrishTime.FormatTrainDate(IrishTime.Today)}</Traindate>" +
            $"<Origin>Dublin Heuston</Origin><Destination>{t.destination}</Destination><Duein>{t.duein}</Duein>" +
            "<Late>0</Late><Exparrival>10:00</Exparrival><Expdepart>10:01</Expdepart><Locationtype>S</Locationtype></objStationData>")) +
        "</ArrayOfObjStationData>";

    private static string Route(params (string code, string name)[] stops) =>
        $"<ArrayOfObjTrainMovements xmlns=\"{Ns}\">" +
        string.Concat(stops.Select((s, i) =>
            $"<objTrainMovements><LocationCode>{s.code}</LocationCode><LocationFullName>{s.name}</LocationFullName>" +
            $"<LocationOrder>{i + 1}</LocationOrder><LocationType>S</LocationType></objTrainMovements>")) +
        "</ArrayOfObjTrainMovements>";

    /// <summary>Answers board and movement requests; can be switched offline.</summary>
    private sealed class FakeApi : HttpMessageHandler
    {
        public string BoardXml { get; set; } = Board();
        public Dictionary<string, string> Routes { get; } = new();
        public bool Offline { get; set; }
        public List<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            lock (Requests) Requests.Add(url);

            if (Offline)
                throw new HttpRequestException("Unable to resolve host api.irishrail.ie");

            var body = url.Contains("/board")
                ? BoardXml
                : Routes.FirstOrDefault(r => url.Contains($"TrainId={r.Key}&")).Value;

            return Task.FromResult(body is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    private static (AppHomeViewModel vm, FakeApi api, List<string> errors) Create()
    {
        var api = new FakeApi();
        var stations = new DataService<List<Station>>
        {
            Data =
            [
                new() { StationDesc = "Howth Junction", StationCode = "HWTHJ" },
                new() { StationDesc = "Howth", StationCode = "HOWTH" },
                new() { StationDesc = "Adamstown", StationCode = "ADAMS" },
                new() { StationDesc = "Newbridge", StationCode = "NBRGE" },
            ]
        };
        var settings = new DataService<Settings>
        {
            Data = new Settings
            {
                GetServiceByStationCodeURL = "https://example.test/board?StationCode=",
                GetTrainMovementsUrl = "https://example.test/movements"
            }
        };
        var stationService = new StationService(new HttpClient(api));
        var vm = new AppHomeViewModel(stations, settings, stationService, new RouteFilterService());
        var errors = new List<string>();
        vm.ErrorOccurred += (_, e) => errors.Add(e);
        return (vm, api, errors);
    }

    [Fact]
    public async Task Search_UsesExactStationMatch()
    {
        var (vm, api, _) = Create();
        api.BoardXml = Board(("E101", "Malahide", 5));

        vm.SelectedStation = "Howth";
        await vm.ExecuteTrainServiceSearch(waitForLock: true);

        Assert.Contains(api.Requests, r => r.EndsWith("StationCode=HOWTH"));
        Assert.Single(vm.TrainJourneys);
    }

    [Fact]
    public async Task Refresh_WhenOffline_KeepsLastBoardAndShowsBanner_WithoutAlert()
    {
        var (vm, api, errors) = Create();
        api.BoardXml = Board(("E101", "Malahide", 5), ("E102", "Bray", 9));
        vm.SelectedStation = "Howth";
        await vm.ExecuteTrainServiceSearch(waitForLock: true);

        api.Offline = true;
        await vm.ExecuteTrainServiceSearch(waitForLock: true, bypassCache: true);

        Assert.Equal(2, vm.TrainJourneys.Count);
        Assert.StartsWith("⚠ Offline", vm.RefreshTime);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Search_OfflineWithNothingToShow_ShowsOneError()
    {
        var (vm, api, errors) = Create();
        api.Offline = true;

        vm.SelectedStation = "Howth";
        await vm.ExecuteTrainServiceSearch(waitForLock: true);

        Assert.Single(errors);
        Assert.True(vm.NoJourneyFound);
    }

    [Fact]
    public async Task DestinationFilter_KeepsOnlyTrainsCallingThere_SortedByDueTime()
    {
        var (vm, api, _) = Create();
        api.BoardXml = Board(("P218", "Portlaoise", 30), ("D413", "Hazelhatch", 5), ("D415", "Newbridge", 12));
        // Movements use ADMTN for Adamstown (the station list says ADAMS)
        api.Routes["P218"] = Route(("ADMTN", "Adamstown"), ("HZLCH", "Hazelhatch"), ("NBRGE", "Newbridge"), ("PTLSE", "Portlaoise"));
        api.Routes["D413"] = Route(("ADMTN", "Adamstown"), ("HZLCH", "Hazelhatch"));

        vm.SelectedStation = "Adamstown";
        await vm.ExecuteTrainServiceSearch(waitForLock: true);
        vm.SelectedDestination = "Newbridge";
        await vm.ExecuteTrainServiceSearch(waitForLock: true);

        // D415 terminates at Newbridge (no route lookup); P218 passes through; D413 doesn't go there
        Assert.Equal(["D415", "P218"], vm.TrainJourneys.Select(j => j.TrainCode));
        Assert.DoesNotContain(api.Requests, r => r.Contains("TrainId=D415"));
    }
}
