using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class TrackingPollPolicyTests
{
    [Theory]
    [InlineData(30.0, 30, 120)]   // 30 min to next stop: every 2 min
    [InlineData(10.0, 30, 60)]    // 10 min: every minute
    [InlineData(3.0, 30, 30)]     // close: the user's interval
    [InlineData(3.0, 5, 15)]      // never below the 15 s floor
    [InlineData(30.0, 300, 300)]  // never more often than the user's interval
    public void NextDelay_DependsOnDistanceToNextStop(double minutesToNextStop, int userSeconds, int expectedSeconds)
    {
        var delay = TrackingPollPolicy.NextDelay(TimeSpan.FromMinutes(minutesToNextStop), userSeconds);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void NextDelay_UnknownNextStop_UsesUserInterval()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), TrackingPollPolicy.NextDelay(null, 30));
    }
}
