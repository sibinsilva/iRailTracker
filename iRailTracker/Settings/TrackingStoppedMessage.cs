namespace iRailTracker
{
    /// <summary>Sent when tracking a train stops (from the notification, or because it arrived), so the bell on Home updates.</summary>
    public sealed record TrackingStoppedMessage(string TrainCode);
}
