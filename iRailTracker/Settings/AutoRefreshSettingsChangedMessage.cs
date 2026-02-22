namespace iRailTracker
{
    public sealed record AutoRefreshSettingsChangedMessage(bool Enabled, int IntervalSeconds);
}
