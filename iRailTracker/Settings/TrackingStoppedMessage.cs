namespace iRailTracker
{
    /// <summary>Sent when tracking a train stops (from the notification, or because it arrived), so the bell on Home updates.</summary>
    public sealed record TrackingStoppedMessage(string TrainCode);

    /// <summary>Sent when tracked journeys have been resumed at startup, so Home re-syncs its bells.</summary>
    public sealed record TrackingRestoredMessage;
}
