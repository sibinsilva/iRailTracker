namespace iRailTracker.Service
{
    /// <summary>
    /// Preferences that degrade to defaults where the platform has none (unit tests run on plain
    /// net10.0, where MAUI Preferences throws), so view models can be tested.
    /// </summary>
    public static class SafePreferences
    {
        public static string Get(string key, string defaultValue)
        {
            try
            {
                return Preferences.Get(key, defaultValue);
            }
            catch (NotImplementedException)
            {
                return defaultValue;
            }
        }

        public static void Set(string key, string value)
        {
            try
            {
                Preferences.Set(key, value);
            }
            catch (NotImplementedException)
            {
            }
        }
    }
}
