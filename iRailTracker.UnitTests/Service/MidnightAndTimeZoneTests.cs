using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

[Collection(IrishClockCollection.Name)]
public class MidnightAndTimeZoneTests
{
    private static readonly DateTime ServiceDate = new(2026, 11, 3);

    private static TrainMovement Stop(int order, string name, string type, string expectedArrival = "00:00:00",
        string expectedDeparture = "00:00:00", string code = "XXX") =>
        new()
        {
            LocationOrder = order,
            LocationFullName = name,
            LocationCode = code,
            LocationType = type,
            ExpectedArrival = expectedArrival,
            ExpectedDeparture = expectedDeparture
        };

    /// <summary>A late train leaving at 23:40 and arriving after midnight.</summary>
    private static List<TrainMovement> LateNightTrain() =>
    [
        Stop(1, "Dublin Connolly", "O", expectedDeparture: "23:40:00", code: "CNLLY"),
        Stop(2, "Malahide", "S", expectedArrival: "23:58:00", code: "MHIDE"),
        Stop(3, "Drogheda", "S", expectedArrival: "00:20:00", code: "DGHDA"),
        Stop(4, "Dundalk", "D", expectedArrival: "00:45:00", code: "DDALK"),
    ];

    [Fact]
    public void ResolveTimes_StopsAfterMidnight_MoveToNextDay()
    {
        var times = TrainMovementAnalysis.ResolveTimes(LateNightTrain(), ServiceDate);

        Assert.Equal(new DateTime(2026, 11, 3, 23, 40, 0), times[0].EffectiveTime);
        Assert.Equal(new DateTime(2026, 11, 3, 23, 58, 0), times[1].EffectiveTime);
        Assert.Equal(new DateTime(2026, 11, 4, 0, 20, 0), times[2].EffectiveTime);
        Assert.Equal(new DateTime(2026, 11, 4, 0, 45, 0), times[3].EffectiveTime);
    }

    [Fact]
    public void ResolveTimes_SmallBackwardsJitter_StaysOnSameDay()
    {
        // Expected times can be revised slightly out of order; that's not a midnight crossing.
        var movements = new List<TrainMovement>
        {
            Stop(1, "A", "O", expectedDeparture: "10:05:00"),
            Stop(2, "B", "S", expectedArrival: "10:03:00"),
        };

        var times = TrainMovementAnalysis.ResolveTimes(movements, ServiceDate);

        Assert.Equal(new DateTime(2026, 11, 3, 10, 3, 0), times[1].EffectiveTime);
    }

    [Fact]
    public void DetermineCurrentStop_AfterMidnight_UsesServiceDate()
    {
        var now = new DateTime(2026, 11, 4, 0, 25, 0);

        var current = TrainMovementAnalysis.DetermineCurrentStop(LateNightTrain(), now, ServiceDate);

        Assert.Equal("Drogheda", current?.LocationFullName);
    }

    [Fact]
    public void DetermineNextStop_BeforeMidnight_FindsStopAfterMidnight()
    {
        var now = new DateTime(2026, 11, 3, 23, 59, 0);

        var next = TrainMovementAnalysis.DetermineNextStop(LateNightTrain(), now, ServiceDate);

        Assert.Equal("Drogheda", next?.LocationFullName);
    }

    [Fact]
    public void HasJourneyEnded_JustBeforeMidnight_IsFalse()
    {
        // The old code compared 23:59 with "today 00:45" and wrongly decided the journey had ended.
        var now = new DateTime(2026, 11, 3, 23, 59, 0);

        Assert.False(TrainMovementAnalysis.HasJourneyEnded(LateNightTrain(), now, ServiceDate, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void HasJourneyEnded_AfterArrivalPlusGrace_IsTrue()
    {
        var now = new DateTime(2026, 11, 4, 0, 51, 0);

        Assert.True(TrainMovementAnalysis.HasJourneyEnded(LateNightTrain(), now, ServiceDate, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void HasJourneyEnded_WithinGrace_IsFalse()
    {
        var now = new DateTime(2026, 11, 4, 0, 48, 0);

        Assert.False(TrainMovementAnalysis.HasJourneyEnded(LateNightTrain(), now, ServiceDate, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void HasJourneyEnded_UnknownArrival_IsFalse()
    {
        var movements = new List<TrainMovement> { Stop(1, "A", "O", expectedDeparture: "10:00:00"), Stop(2, "B", "D") };

        Assert.False(TrainMovementAnalysis.HasJourneyEnded(movements, new DateTime(2026, 11, 3, 23, 0, 0), ServiceDate, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void IrishTime_Now_ConvertsFromUtcUsingIrishSummerTime()
    {
        var original = IrishTime.UtcNowProvider;
        try
        {
            // 12:00 UTC in July is 13:00 Irish Summer Time, whatever the device's zone is.
            IrishTime.UtcNowProvider = () => new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(new DateTime(2026, 7, 1, 13, 0, 0), IrishTime.Now);

            // In winter Ireland is on UTC.
            IrishTime.UtcNowProvider = () => new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(new DateTime(2026, 12, 1, 12, 0, 0), IrishTime.Now);
        }
        finally
        {
            IrishTime.UtcNowProvider = original;
        }
    }

    [Fact]
    public void TrainDate_FormatsAndParsesInvariantly()
    {
        Assert.Equal("03 Nov 2026", IrishTime.FormatTrainDate(ServiceDate));
        Assert.Equal(ServiceDate, IrishTime.ParseTrainDate("03 Nov 2026"));
        Assert.Equal(ServiceDate, IrishTime.ParseTrainDate(" 03 Nov 2026 "));
    }

    [Fact]
    public void TrainDate_Unparseable_FallsBackToIrishToday()
    {
        Assert.Equal(IrishTime.Today, IrishTime.ParseTrainDate("garbage"));
        Assert.Equal(IrishTime.Today, IrishTime.ParseTrainDate(null));
    }
}
