using System.Net;
using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

[Collection(IrishClockCollection.Name)]
public class RouteFilterServiceTests
{
    private static TrainMovement Stop(int order, string code, string type = "S") =>
        new() { LocationOrder = order, LocationCode = code, LocationFullName = code, LocationType = type };

    private static readonly List<TrainMovement> BrayToHowth =
    [
        Stop(1, "BRAY", "O"),
        Stop(2, "DLERY"),
        Stop(3, "SKILL", "T"),
        Stop(4, "PERSE"),
        Stop(5, "HOWTH", "D"),
    ];

    [Fact]
    public void CallsAtAfter_DestinationLaterOnRoute_IsTrue()
    {
        Assert.True(RouteFilterService.CallsAtAfter(BrayToHowth, "DLERY", "HOWTH"));
    }

    [Fact]
    public void CallsAtAfter_DestinationBeforeOrigin_IsFalse()
    {
        // Wrong direction: the train has already been to Bray
        Assert.False(RouteFilterService.CallsAtAfter(BrayToHowth, "PERSE", "BRAY"));
    }

    [Fact]
    public void CallsAtAfter_DestinationOnlyAPassingTimingPoint_IsFalse()
    {
        Assert.False(RouteFilterService.CallsAtAfter(BrayToHowth, "BRAY", "SKILL"));
    }

    [Fact]
    public void CallsAtAfter_IgnoresCaseAndPadding_AndUsesRouteOrder()
    {
        var shuffled = BrayToHowth.AsEnumerable().Reverse().ToList();
        Assert.True(RouteFilterService.CallsAtAfter(shuffled, " bray ", "perse"));
    }

    [Fact]
    public void CallsAtAfter_OriginNotOnRoute_IsFalse()
    {
        Assert.False(RouteFilterService.CallsAtAfter(BrayToHowth, "MHIDE", "HOWTH"));
    }

    [Fact]
    public void CallsAtAfter_StationWithDifferentCodeInMovements_MatchesByName()
    {
        // Real data (3 Nov 2026): the station list has Adamstown as ADAMS, train movements as ADMTN.
        // Train D415 Dublin Connolly -> Newbridge from Adamstown wrongly showed "No trains" before.
        var d415 = new List<TrainMovement>
        {
            new() { LocationOrder = 13, LocationCode = "KISHO", LocationFullName = "Kishoge", LocationType = "S" },
            new() { LocationOrder = 14, LocationCode = "ADMTN", LocationFullName = "Adamstown", LocationType = "S" },
            new() { LocationOrder = 15, LocationCode = "HZLCH", LocationFullName = "Hazelhatch", LocationType = "S" },
            new() { LocationOrder = 20, LocationCode = "NBRGE", LocationFullName = "Newbridge", LocationType = "D" },
        };

        Assert.False(RouteFilterService.CallsAtAfter(d415, "ADAMS", "NBRGE"));
        Assert.True(RouteFilterService.CallsAtAfter(d415, "ADAMS", "NBRGE", "Adamstown", "Newbridge"));
    }

    [Fact]
    public async Task FilterCallingAtAsync_TrainEndingAtDestination_KeptWithoutRouteLookup()
    {
        var handler = new RouteHandler(new());
        var board = new List<StationData> { new() { Traincode = "D415", Destination = "Newbridge ", Traindate = IrishTime.FormatTrainDate(IrishTime.Today) } };

        var result = await new RouteFilterService().FilterCallingAtAsync(
            new Settings { GetTrainMovementsUrl = "https://example.test/m" }, board,
            new Station { StationCode = "ADAMS", StationDesc = "Adamstown" },
            new Station { StationCode = "NBRGE", StationDesc = "Newbridge" },
            new StationService(new HttpClient(handler)));

        Assert.Single(result);
        Assert.Equal(0, handler.Requests);
    }

    private sealed class RouteHandler(Dictionary<string, string> routesByTrain) : HttpMessageHandler
    {
        private int _requests;
        public int Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            var query = request.RequestUri!.Query;
            var match = routesByTrain.FirstOrDefault(kv => query.Contains($"TrainId={kv.Key}&"));
            return Task.FromResult(match.Key is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(match.Value) });
        }
    }

    private static string RouteXml(params string[] codes) =>
        "<ArrayOfObjTrainMovements xmlns=\"http://api.irishrail.ie/realtime/\">" +
        string.Concat(codes.Select((c, i) =>
            $"<objTrainMovements><LocationCode>{c}</LocationCode><LocationFullName>{c}</LocationFullName>" +
            $"<LocationOrder>{i + 1}</LocationOrder><LocationType>S</LocationType></objTrainMovements>")) +
        "</ArrayOfObjTrainMovements>";

    [Fact]
    public async Task FilterCallingAtAsync_KeepsMatchingAndUnknownRoutes_CachesPerTrain()
    {
        var handler = new RouteHandler(new()
        {
            ["E101"] = RouteXml("BRAY", "DLERY", "HOWTH"),
            ["E102"] = RouteXml("BRAY", "DLERY", "GSTNS"),
            // E103 has no route data (404): kept, not hidden
        });
        var stationService = new StationService(new HttpClient(handler));
        var settings = new Settings { GetTrainMovementsUrl = "https://example.test/movements" };
        var today = IrishTime.FormatTrainDate(IrishTime.Today);
        var board = new List<StationData>
        {
            new() { Traincode = "E101", Traindate = today },
            new() { Traincode = "E102", Traindate = today },
            new() { Traincode = "E103", Traindate = today },
        };
        var filter = new RouteFilterService();

        var from = new Station { StationCode = "DLERY", StationDesc = "Dun Laoghaire" };
        var to = new Station { StationCode = "HOWTH", StationDesc = "Howth" };

        var result = await filter.FilterCallingAtAsync(settings, board, from, to, stationService);

        Assert.Equal(["E101", "E103"], result.Select(t => t.Traincode));

        // Second refresh: known routes come from the cache, only the failed one is retried
        await filter.FilterCallingAtAsync(settings, board, from, to, stationService);
        Assert.Equal(4, handler.Requests);
    }
}
