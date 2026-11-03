namespace iRailTracker.Service
{
    /// <summary>
    /// How long to wait before the next tracking poll. Each poll costs two API requests, so poll
    /// rarely while the train is far from its next stop and at the user's interval when it's close.
    /// </summary>
    public static class TrackingPollPolicy
    {
        public const int MinimumSeconds = 15;

        public static TimeSpan NextDelay(TimeSpan? untilNextStop, int userIntervalSeconds)
        {
            var user = TimeSpan.FromSeconds(Math.Max(userIntervalSeconds, MinimumSeconds));

            var relaxed = untilNextStop switch
            {
                { } t when t > TimeSpan.FromMinutes(15) => TimeSpan.FromMinutes(2),
                { } t when t > TimeSpan.FromMinutes(5) => TimeSpan.FromMinutes(1),
                _ => user
            };

            // Never poll more often than the user asked for
            return relaxed > user ? relaxed : user;
        }
    }
}
