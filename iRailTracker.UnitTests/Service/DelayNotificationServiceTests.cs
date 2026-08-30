using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class DelayNotificationServiceTests
{
    private static TrainDelayInfo CreateDelay(string trainCode, int late) =>
        new(trainCode, "Dublin Connolly", "Cork", late);

    [Fact]
    public void GetDelayToNotify_DelayAtOrAboveThreshold_ReturnsDelay()
    {
        var service = new DelayNotificationService();
        var delay = CreateDelay("E101", 5);

        var result = service.GetDelayToNotify(delay, thresholdMinutes: 5);

        Assert.NotNull(result);
        Assert.Equal("E101", result!.TrainCode);
    }

    [Fact]
    public void GetDelayToNotify_DelayBelowThreshold_ReturnsNull()
    {
        var service = new DelayNotificationService();
        var delay = CreateDelay("E101", 3);

        var result = service.GetDelayToNotify(delay, thresholdMinutes: 5);

        Assert.Null(result);
    }

    [Fact]
    public void GetDelayToNotify_SameDelayOnSubsequentCall_DoesNotNotifyAgain()
    {
        var service = new DelayNotificationService();
        var delay = CreateDelay("E101", 5);

        service.GetDelayToNotify(delay, thresholdMinutes: 5);
        var secondResult = service.GetDelayToNotify(delay, thresholdMinutes: 5);

        Assert.Null(secondResult);
    }

    [Fact]
    public void GetDelayToNotify_DelayIncreasesFurther_NotifiesAgain()
    {
        var service = new DelayNotificationService();

        service.GetDelayToNotify(CreateDelay("E101", 5), thresholdMinutes: 5);
        var secondResult = service.GetDelayToNotify(CreateDelay("E101", 8), thresholdMinutes: 5);

        Assert.NotNull(secondResult);
        Assert.Equal(8, secondResult!.LateMinutes);
    }

    [Fact]
    public void GetDelayToNotify_DelayDropsThenReCrossesThreshold_NotifiesAgain()
    {
        var service = new DelayNotificationService();

        service.GetDelayToNotify(CreateDelay("E101", 5), thresholdMinutes: 5);
        service.GetDelayToNotify(CreateDelay("E101", 2), thresholdMinutes: 5);
        var thirdResult = service.GetDelayToNotify(CreateDelay("E101", 5), thresholdMinutes: 5);

        Assert.NotNull(thirdResult);
    }

    [Fact]
    public void GetDelayToNotify_DifferentTrainCode_TrackedIndependently()
    {
        var service = new DelayNotificationService();

        service.GetDelayToNotify(CreateDelay("E101", 5), thresholdMinutes: 5);
        var otherTrainResult = service.GetDelayToNotify(CreateDelay("E202", 5), thresholdMinutes: 5);

        Assert.NotNull(otherTrainResult);
    }
}
