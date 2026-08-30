namespace iRailTracker
{
    public sealed record FavouriteStationsChangedMessage(IReadOnlyList<string> Stations);
}
