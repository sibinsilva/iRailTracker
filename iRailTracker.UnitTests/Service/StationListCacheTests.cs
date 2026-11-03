using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class StationListCacheTests
{
    [Fact]
    public void SerializeThenParse_RoundTripsAllFields()
    {
        var stations = new List<Station>
        {
            new() { StationDesc = "Dun Laoghaire", StationAlias = "Mallin", StationCode = "DLERY", StationLatitude = 53.2951, StationLongitude = -6.13498, StationId = 129 },
            new() { StationDesc = "Bray", StationAlias = "", StationCode = "BRAY", StationLatitude = 53.2043, StationLongitude = -6.10046, StationId = 104 },
        };

        var parsed = StationListCache.Parse(StationListCache.Serialize(stations));

        Assert.Equal(2, parsed.Count);
        Assert.Equal("Mallin", parsed[0].StationAlias);
        Assert.Equal(-6.13498, parsed[0].StationLongitude);
        Assert.Equal(104, parsed[1].StationId);
        Assert.Equal("", parsed[1].StationAlias);
    }

    [Fact]
    public void Serialize_TabsAndNewlinesInNames_DoNotBreakTheFormat()
    {
        var stations = new List<Station> { new() { StationDesc = "Odd\tName\n", StationCode = "ODD" } };

        var parsed = StationListCache.Parse(StationListCache.Serialize(stations));

        Assert.Single(parsed);
        Assert.Equal("ODD", parsed[0].StationCode);
    }

    [Fact]
    public void Parse_SkipsCorruptLines()
    {
        var parsed = StationListCache.Parse("Bray\t\tBRAY\t53.2\t-6.1\t104\ngarbage\nX\t\tX\tnot-a-number\t0\t1\n");

        Assert.Single(parsed);
    }
}
