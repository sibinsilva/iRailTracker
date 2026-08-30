using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class TrainMovementAnalysisTests
{
    private static TrainMovement CreateMovement(
        int order, string name, string locationType, string locationCode = "XXX",
        string scheduledArrival = "00:00:00", string scheduledDeparture = "00:00:00",
        string expectedArrival = "00:00:00", string expectedDeparture = "00:00:00") =>
        new()
        {
            LocationOrder = order,
            LocationFullName = name,
            LocationCode = locationCode,
            LocationType = locationType,
            ScheduledArrival = scheduledArrival,
            ScheduledDeparture = scheduledDeparture,
            ExpectedArrival = expectedArrival,
            ExpectedDeparture = expectedDeparture
        };

    #region DetermineCurrentStop / DetermineCurrentLocation

    [Fact]
    public void DetermineCurrentLocation_SkipsBlankTimingPoints_PicksLastPassedNamedStop()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Dun Laoghaire", "S", expectedArrival: "13:33:48"),
            CreateMovement(3, "", "T", expectedArrival: "13:54:00"),
            CreateMovement(4, "Dublin Pearse", "S", expectedArrival: "13:55:30"),
            CreateMovement(5, "Howth", "D", expectedArrival: "14:29:30"),
        };

        var now = new DateTime(2026, 8, 30, 13, 40, 0);

        var result = TrainMovementAnalysis.DetermineCurrentLocation(movements, now);

        Assert.Equal("Dun Laoghaire", result);
    }

    [Fact]
    public void DetermineCurrentLocation_BeforeAnyStopReached_FallsBackToFirstNamedStop()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Dun Laoghaire", "S", expectedArrival: "13:33:48"),
        };

        var now = new DateTime(2026, 8, 30, 13, 0, 0);

        var result = TrainMovementAnalysis.DetermineCurrentLocation(movements, now);

        Assert.Equal("Bray", result);
    }

    [Fact]
    public void DetermineCurrentLocation_AfterDestination_ReturnsDestination()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Howth", "D", expectedArrival: "14:29:30"),
        };

        var now = new DateTime(2026, 8, 30, 14, 35, 0);

        var result = TrainMovementAnalysis.DetermineCurrentLocation(movements, now);

        Assert.Equal("Howth", result);
    }

    #endregion

    #region DetermineNextStop

    [Fact]
    public void DetermineNextStop_ReturnsFirstStopNotYetReached()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", locationCode: "BRAY", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Dun Laoghaire", "S", locationCode: "DLGH", expectedArrival: "13:33:48"),
            CreateMovement(3, "Dublin Pearse", "S", locationCode: "PERSE", expectedArrival: "13:55:30"),
            CreateMovement(4, "Howth", "D", locationCode: "HOWTH", expectedArrival: "14:29:30"),
        };

        var now = new DateTime(2026, 8, 30, 13, 40, 0);

        var result = TrainMovementAnalysis.DetermineNextStop(movements, now);

        Assert.NotNull(result);
        Assert.Equal("Dublin Pearse", result!.LocationFullName);
        Assert.Equal("PERSE", result.LocationCode);
    }

    [Fact]
    public void DetermineNextStop_SkipsBlankTimingPoints()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", locationCode: "BRAY", expectedDeparture: "13:11:00"),
            CreateMovement(2, "", "T", locationCode: "T1", expectedArrival: "13:20:00"),
            CreateMovement(3, "Dun Laoghaire", "S", locationCode: "DLGH", expectedArrival: "13:33:48"),
        };

        // After Bray's departure, so it's no longer "upcoming" - the blank T row should be skipped next.
        var now = new DateTime(2026, 8, 30, 13, 15, 0);

        var result = TrainMovementAnalysis.DetermineNextStop(movements, now);

        Assert.NotNull(result);
        Assert.Equal("Dun Laoghaire", result!.LocationFullName);
    }

    [Fact]
    public void DetermineNextStop_AfterDestinationReached_ReturnsNull()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", locationCode: "BRAY", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Howth", "D", locationCode: "HOWTH", expectedArrival: "14:29:30"),
        };

        var now = new DateTime(2026, 8, 30, 14, 35, 0);

        var result = TrainMovementAnalysis.DetermineNextStop(movements, now);

        Assert.Null(result);
    }

    [Fact]
    public void DetermineNextStop_BeforeDeparture_ReturnsFirstUpcomingStop()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", locationCode: "BRAY", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Dun Laoghaire", "S", locationCode: "DLGH", expectedArrival: "13:33:48"),
        };

        var now = new DateTime(2026, 8, 30, 13, 0, 0);

        var result = TrainMovementAnalysis.DetermineNextStop(movements, now);

        Assert.NotNull(result);
        Assert.Equal("Bray", result!.LocationFullName);
    }

    #endregion
}
