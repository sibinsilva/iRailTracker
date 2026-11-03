using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

[Collection(IrishClockCollection.Name)]
public class DeparturesSummaryTests
{
    [Fact]
    public void FormatRows_SoonestFirst_LimitedToThree()
    {
        var board = new List<StationData>
        {
            new() { Duein = 12, Destination = "Howth", Late = 0 },
            new() { Duein = 3, Destination = "Greystones ", Late = 2 },
            new() { Duein = 0, Destination = "Malahide", Late = -1 },
            new() { Duein = 25, Destination = "Bray", Late = 0 },
        };

        var rows = DeparturesSummary.FormatRows(board);

        Assert.Equal(["Due → Malahide · -1", "3 min → Greystones · +2", "12 min → Howth"], rows);
    }

    [Fact]
    public void ParseStationCodes_RoundTripsStoredMap_IgnoresBadLines()
    {
        var map = DeparturesSummary.ParseStationCodes("Bray\tBRAY\nDun Laoghaire\tDLERY\nbroken line\n\n");

        Assert.Equal(2, map.Count);
        Assert.Equal("DLERY", map["Dun Laoghaire"]);
    }

    [Fact]
    public void LoadEmbeddedSettings_ReadsCommittedAppSettings()
    {
        var settings = DeparturesSummary.LoadEmbeddedSettings();

        Assert.NotNull(settings);
        Assert.StartsWith("https://api.irishrail.ie/", settings!.GetTrainMovementsUrl);
    }
}
