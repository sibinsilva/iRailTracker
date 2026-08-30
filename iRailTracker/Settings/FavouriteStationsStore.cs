using System.Text.Json;

namespace iRailTracker
{
    public static class FavouriteStationsStore
    {
        public static List<string> Load()
        {
            var json = Preferences.Get(AppPreferences.FavouriteStations, string.Empty);

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
                catch (JsonException)
                {
                    return new List<string>();
                }
            }

            // One-time migration from the old single-favourite preference.
            var legacyFavourite = Preferences.Get(AppPreferences.FavouriteStation, string.Empty);
            if (string.IsNullOrEmpty(legacyFavourite))
                return new List<string>();

            Preferences.Remove(AppPreferences.FavouriteStation);
            var migrated = new List<string> { legacyFavourite };
            Save(migrated);
            return migrated;
        }

        public static void Save(List<string> stations)
        {
            Preferences.Set(AppPreferences.FavouriteStations, JsonSerializer.Serialize(stations));
        }
    }
}
