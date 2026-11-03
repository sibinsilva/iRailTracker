namespace iRailTracker.UnitTests.Service;

/// <summary>
/// Tests that change or depend on the shared IrishTime clock must not run in parallel with each
/// other, or one test's fake "now" leaks into another.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class IrishClockCollection
{
    public const string Name = "IrishClock";
}
