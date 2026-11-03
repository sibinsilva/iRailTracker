using iRailTracker.Model;
using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class CommuteServiceTests
{
    private static readonly CommutePreset Preset = CommutePreset.Default with
    {
        Enabled = true, From = "Adamstown", To = "Dublin Heuston",
        WindowStart = new TimeSpan(7, 30, 0), WindowEnd = new TimeSpan(9, 0, 0), ThresholdMinutes = 5
    };

    private static readonly DateTime TuesdayMorning = new(2026, 11, 3, 7, 45, 0); // a Tuesday

    private static StationData Train(string code, string scheduled, int late) =>
        new() { Traincode = code, Schdepart = scheduled, Expdepart = scheduled, Late = late, Origin = "Portlaoise", Destination = "Dublin Heuston" };

    [Fact]
    public void FindAlerts_LateTrainInsideWindow_Alerts()
    {
        var alerts = CommuteService.FindAlerts(Preset, [Train("P201", "08:05", 7)], TuesdayMorning, new HashSet<string>());

        var alert = Assert.Single(alerts);
        Assert.Equal("Your 08:05 to Dublin Heuston is 7 min late", alert.Title);
    }

    [Fact]
    public void FindAlerts_IgnoresOnTimeOutsideWindowAndSmallDelays()
    {
        var board = new List<StationData>
        {
            Train("P201", "08:05", 2),   // under threshold
            Train("P202", "09:30", 12),  // after the window
            Train("P203", "07:00", 12),  // before the window
        };

        Assert.Empty(CommuteService.FindAlerts(Preset, board, TuesdayMorning, new HashSet<string>()));
    }

    [Fact]
    public void FindAlerts_SameDelayNotRepeated_ButGrowingDelayAlertsAgain()
    {
        var first = CommuteService.FindAlerts(Preset, [Train("P201", "08:05", 6)], TuesdayMorning, new HashSet<string>());
        var notified = first.Select(a => a.Key).ToHashSet();

        Assert.Empty(CommuteService.FindAlerts(Preset, [Train("P201", "08:05", 8)], TuesdayMorning, notified));
        Assert.Single(CommuteService.FindAlerts(Preset, [Train("P201", "08:05", 11)], TuesdayMorning, notified));
    }

    [Fact]
    public void IsActiveAt_RespectsDaysWindowAndLeadTime()
    {
        Assert.True(Preset.IsActiveAt(TuesdayMorning));
        Assert.True(Preset.IsActiveAt(new DateTime(2026, 11, 3, 7, 5, 0)));    // 25 min early: warn ahead
        Assert.False(Preset.IsActiveAt(new DateTime(2026, 11, 3, 6, 50, 0)));  // too early
        Assert.False(Preset.IsActiveAt(new DateTime(2026, 11, 3, 9, 5, 0)));   // after the window
        Assert.False(Preset.IsActiveAt(new DateTime(2026, 11, 8, 7, 45, 0)));  // Sunday
        Assert.False((Preset with { Enabled = false }).IsActiveAt(TuesdayMorning));
    }
}
