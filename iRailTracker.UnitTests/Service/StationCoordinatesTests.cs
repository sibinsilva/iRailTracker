using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class StationCoordinatesTests
{
    private static Station S(string name, string code, double lat, double lon) =>
        new() { StationDesc = name, StationCode = code, StationLatitude = lat, StationLongitude = lon };

    [Fact]
    public void Repair_FixesAdamstownFromTheOtherEntries()
    {
        // As returned by getAllStationsXML (Nov 2026): ADAMS first, with longitude -7
        var stations = new List<Station>
        {
            S("Adamstown", "ADAMS", 53.3353, -7),
            S("Adamstown", "ADAMF", 53.3353, -6.45233),
            S("Adamstown", "ADMTN", 53.3353, -6.45233),
            S("Hazelhatch", "HZLCH", 53.3223, -6.52356),
        };

        StationCoordinates.Repair(stations);

        Assert.All(stations.Where(s => s.StationDesc == "Adamstown"), s =>
        {
            Assert.Equal(53.3353, s.StationLatitude);
            Assert.Equal(-6.45233, s.StationLongitude);
        });
        Assert.Equal(-6.52356, stations[3].StationLongitude);
    }

    [Fact]
    public void Repair_LeavesTiesAndSingleEntriesAlone()
    {
        var stations = new List<Station>
        {
            S("Somewhere", "AAA", 53.1, -6.1),
            S("Somewhere", "BBB", 53.2, -6.2),
            S("Elsewhere", "CCC", 52.0, -7.0),
        };

        StationCoordinates.Repair(stations);

        Assert.Equal(-6.1, stations[0].StationLongitude);
        Assert.Equal(-6.2, stations[1].StationLongitude);
        Assert.Equal(-7.0, stations[2].StationLongitude);
    }

    [Fact]
    public void Repair_MatchesNamesIgnoringCaseAndSpaces()
    {
        var stations = new List<Station>
        {
            S("Adamstown ", "ADAMS", 53.3353, -7),
            S("adamstown", "ADAMF", 53.3353, -6.45233),
            S("ADAMSTOWN", "ADMTN", 53.3353, -6.45233),
        };

        StationCoordinates.Repair(stations);

        Assert.Equal(-6.45233, stations[0].StationLongitude);
    }

    [Fact]
    public void CacheLoadPath_RepairsOldCachedCoordinates()
    {
        var cached = StationListCache.Serialize([
            S("Adamstown", "ADAMS", 53.3353, -7),
            S("Adamstown", "ADMTN", 53.3353, -6.45233),
            S("Adamstown", "ADAMF", 53.3353, -6.45233),
        ]);

        var stations = StationCoordinates.Repair(StationListCache.Parse(cached));

        Assert.Equal(-6.45233, stations.Single(s => s.StationCode == "ADAMS").StationLongitude);
    }
}
