using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class TrainMovementAnalysisTests
{
    private static TrainMovement CreateMovement(
        int order, string name, string locationType,
        string scheduledArrival = "00:00:00", string scheduledDeparture = "00:00:00",
        string expectedArrival = "00:00:00", string expectedDeparture = "00:00:00") =>
        new()
        {
            LocationOrder = order,
            LocationFullName = name,
            LocationType = locationType,
            ScheduledArrival = scheduledArrival,
            ScheduledDeparture = scheduledDeparture,
            ExpectedArrival = expectedArrival,
            ExpectedDeparture = expectedDeparture
        };

    #region TryComputeDelayMinutes

    [Fact]
    public void TryComputeDelayMinutes_OnTime_ReturnsZero()
    {
        var destination = CreateMovement(1, "Howth", "D", scheduledArrival: "14:26:00", expectedArrival: "14:26:00");

        var success = TrainMovementAnalysis.TryComputeDelayMinutes(destination, out var minutes);

        Assert.True(success);
        Assert.Equal(0, minutes);
    }

    [Fact]
    public void TryComputeDelayMinutes_Delayed_MatchesLiveSample()
    {
        // Live sample: train E908, destination Howth, sched 14:26:00 / expected 14:29:30 -> board Late was 4.
        var destination = CreateMovement(1, "Howth", "D", scheduledArrival: "14:26:00", expectedArrival: "14:29:30");

        var success = TrainMovementAnalysis.TryComputeDelayMinutes(destination, out var minutes);

        Assert.True(success);
        Assert.Equal(4, minutes);
    }

    [Fact]
    public void TryComputeDelayMinutes_UnparseableTime_ReturnsFalse()
    {
        var destination = CreateMovement(1, "Howth", "D", scheduledArrival: "not-a-time", expectedArrival: "14:29:30");

        var success = TrainMovementAnalysis.TryComputeDelayMinutes(destination, out var minutes);

        Assert.False(success);
        Assert.Equal(0, minutes);
    }

    [Fact]
    public void TryComputeDelayMinutes_EmptyTime_ReturnsFalse()
    {
        var destination = CreateMovement(1, "Howth", "D", scheduledArrival: "", expectedArrival: "");

        var success = TrainMovementAnalysis.TryComputeDelayMinutes(destination, out _);

        Assert.False(success);
    }

    #endregion

    #region DetermineCurrentLocation

    [Fact]
    public void DetermineCurrentLocation_SkipsBlankTimingPoints_PicksLastPassedNamedStop()
    {
        var movements = new List<TrainMovement>
        {
            CreateMovement(1, "Bray", "O", scheduledDeparture: "13:11:00", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Dun Laoghaire", "S", scheduledArrival: "13:31:00", expectedArrival: "13:33:48"),
            CreateMovement(3, "", "T", scheduledArrival: "13:50:30", expectedArrival: "13:54:00"),
            CreateMovement(4, "Dublin Pearse", "S", scheduledArrival: "13:52:00", expectedArrival: "13:55:30"),
            CreateMovement(5, "Howth", "D", scheduledArrival: "14:26:00", expectedArrival: "14:29:30"),
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
            CreateMovement(1, "Bray", "O", scheduledDeparture: "13:11:00", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Dun Laoghaire", "S", scheduledArrival: "13:31:00", expectedArrival: "13:33:48"),
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
            CreateMovement(1, "Bray", "O", scheduledDeparture: "13:11:00", expectedDeparture: "13:11:00"),
            CreateMovement(2, "Howth", "D", scheduledArrival: "14:26:00", expectedArrival: "14:29:30"),
        };

        var now = new DateTime(2026, 8, 30, 14, 35, 0);

        var result = TrainMovementAnalysis.DetermineCurrentLocation(movements, now);

        Assert.Equal("Howth", result);
    }

    #endregion
}
