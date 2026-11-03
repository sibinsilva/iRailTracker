using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class BoardFormattingTests
{
    private static readonly List<Station> Stations =
    [
        new() { StationDesc = "Howth Junction", StationCode = "HWTHJ" },
        new() { StationDesc = "Howth", StationCode = "HOWTH" },
        new() { StationDesc = "Dublin Connolly", StationCode = "CNLLY" },
    ];

    [Fact]
    public void FindStationCode_PrefersExactName_OverContainsMatch()
    {
        // "Howth Junction" comes first and contains "Howth"; the old logic picked it
        Assert.Equal("HOWTH", BoardFormatting.FindStationCode(Stations, "Howth"));
    }

    [Fact]
    public void FindStationCode_IsCaseAndSpaceInsensitive()
    {
        Assert.Equal("CNLLY", BoardFormatting.FindStationCode(Stations, "  dublin connolly "));
    }

    [Fact]
    public void FindStationCode_FallsBackToPartialMatchForCombinedNames()
    {
        Assert.Equal("CNLLY", BoardFormatting.FindStationCode(Stations, "Connolly & Amiens St"));
    }

    [Fact]
    public void FindStationCode_Empty_ReturnsNull()
    {
        Assert.Null(BoardFormatting.FindStationCode(Stations, " "));
    }

    [Fact]
    public void DescribeTime_PassingTrain_ShowsDeparture()
    {
        var train = new StationData { Locationtype = "S", Exparrival = "08:03", Expdepart = "08:04" };
        Assert.Equal(("Departs", "8:04 am"), BoardFormatting.DescribeTime(train));
    }

    [Fact]
    public void DescribeTime_TerminatingTrain_ShowsArrival()
    {
        var train = new StationData { Locationtype = "D", Exparrival = "17:45", Expdepart = "00:00" };
        Assert.Equal(("Arrives", "5:45 pm"), BoardFormatting.DescribeTime(train));
    }

    [Fact]
    public void DescribeTime_MissingDeparture_FallsBackToArrival()
    {
        var train = new StationData { Locationtype = "S", Exparrival = "12:10", Expdepart = "" };
        Assert.Equal(("Arrives", "12:10 pm"), BoardFormatting.DescribeTime(train));
    }

    [Fact]
    public void DescribeTime_GarbageTimes_DoNotThrow()
    {
        var train = new StationData { Locationtype = "S", Exparrival = "??", Expdepart = "n/a" };
        Assert.Equal(("Departs", "--"), BoardFormatting.DescribeTime(train));
    }
}
