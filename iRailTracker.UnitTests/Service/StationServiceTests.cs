using System.Net;
using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class StationServiceTests
{
    private const string MovementsXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <ArrayOfObjTrainMovements xmlns="http://api.irishrail.ie/realtime/">
          <objTrainMovements>
            <TrainCode>E808 </TrainCode>
            <LocationCode>BRAY</LocationCode>
            <LocationFullName>Bray</LocationFullName>
            <LocationOrder>1</LocationOrder>
            <LocationType>O</LocationType>
            <ExpectedArrival>00:00:00</ExpectedArrival>
            <ExpectedDeparture>13:11:00</ExpectedDeparture>
          </objTrainMovements>
          <objTrainMovements>
            <TrainCode>E808 </TrainCode>
            <LocationCode>HOWTH</LocationCode>
            <LocationFullName>Howth</LocationFullName>
            <LocationOrder>2</LocationOrder>
            <LocationType>D</LocationType>
            <ExpectedArrival>14:29:30</ExpectedArrival>
            <ExpectedDeparture>00:00:00</ExpectedDeparture>
          </objTrainMovements>
        </ArrayOfObjTrainMovements>
        """;

    private static readonly Settings TestSettings = new()
    {
        GetAllStationsUrl = "https://example.test/stations",
        GetServiceByStationCodeURL = "https://example.test/board?StationCode=",
        GetTrainMovementsUrl = "https://example.test/movements"
    };

    private sealed class FakeHandler(HttpStatusCode status, string body, Exception? throws = null) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (throws is not null)
                throw throws;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    [Fact]
    public void ParseXml_ValidMovements_ReturnsAllStops()
    {
        var result = StationService.ParseXml<TrainMovementsResponse>(MovementsXml.Trim());

        Assert.NotNull(result);
        Assert.Equal(2, result!.Movements.Count);
        Assert.Equal("Howth", result.Movements[1].LocationFullName);
        Assert.Equal("14:29:30", result.Movements[1].ExpectedArrival);
    }

    [Fact]
    public void ParseXml_Malformed_ReturnsNull()
    {
        Assert.Null(StationService.ParseXml<TrainMovementsResponse>("<not-xml"));
    }

    [Fact]
    public async Task GetTrainMovementsAsync_SendsInvariantServiceDate()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, MovementsXml.Trim());
        var service = new StationService(new HttpClient(handler));

        var movements = await service.GetTrainMovementsAsync(TestSettings, "E808 ", new DateTime(2026, 11, 3), null);

        Assert.Equal(2, movements.Count);
        Assert.Equal("https://example.test/movements?TrainId=E808&TrainDate=03%20Nov%202026",
            handler.LastRequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task HttpError_ReportsOnceAndReturnsEmpty()
    {
        var errors = new List<string>();
        var service = new StationService(new HttpClient(new FakeHandler(HttpStatusCode.ServiceUnavailable, "")));

        var board = await service.GetTrainServicesAsync(TestSettings, "BRAY", errors.Add);

        Assert.Empty(board);
        Assert.Single(errors);
        Assert.Contains("503", errors[0]);
    }

    [Fact]
    public async Task NetworkFailure_ReportsOnceAndReturnsEmpty()
    {
        var errors = new List<string>();
        var handler = new FakeHandler(HttpStatusCode.OK, "", new HttpRequestException("no route to host"));
        var service = new StationService(new HttpClient(handler));

        var stations = await service.GetAllStationsAsync(TestSettings, errors.Add);

        Assert.Empty(stations);
        Assert.Single(errors);
        Assert.Contains("no route to host", errors[0]);
    }

    [Fact]
    public async Task UnexpectedBody_ReportsOnceAndReturnsEmpty()
    {
        var errors = new List<string>();
        var service = new StationService(new HttpClient(new FakeHandler(HttpStatusCode.OK, "<html>maintenance</html>")));

        var movements = await service.GetTrainMovementsAsync(TestSettings, "E808", new DateTime(2026, 11, 3), errors.Add);

        Assert.Empty(movements);
        Assert.Single(errors);
    }
}
