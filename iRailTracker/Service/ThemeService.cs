using iRailTracker.Resources.Themes;

namespace iRailTracker.Service
{
    public enum ThemeStyle { Glass, Classic }

    public enum ThemeMode { System, Light, Dark }

    /// <summary>
    /// Applies the user's theme (Liquid Glass or Classic surfaces) and light/dark mode. Themes are
    /// resource dictionaries with the same keys; pages reference them with DynamicResource, so
    /// swapping the dictionary restyles open pages straight away.
    /// </summary>
    public static class ThemeService
    {
        private const string StyleKey = "theme_style";
        private const string ModeKey = "theme_mode";

        public static ThemeStyle Style => Load(StyleKey, ThemeStyle.Glass);
        public static ThemeMode Mode => Load(ModeKey, ThemeMode.System);

        /// <summary>
        /// Call once from the App constructor, before the first page is created (pages look up
        /// theme resources when they're built). Takes the app because Application.Current may not
        /// be set yet at that point.
        /// </summary>
        public static void ApplySaved(Application app)
        {
            ApplyStyle(Style, save: false, app);
            ApplyMode(Mode, save: false, app);
        }

        public static void ApplyStyle(ThemeStyle style, bool save = true, Application? app = null)
        {
            if (save)
                Preferences.Set(StyleKey, style.ToString());

            var merged = (app ?? Application.Current)?.Resources.MergedDictionaries;
            if (merged is null)
                return;

            foreach (var old in merged.Where(d => d is GlassTheme or ClassicTheme).ToList())
                merged.Remove(old);

            merged.Add(style == ThemeStyle.Classic ? new ClassicTheme() : new GlassTheme());
        }

        public static void ApplyMode(ThemeMode mode, bool save = true, Application? app = null)
        {
            if (save)
                Preferences.Set(ModeKey, mode.ToString());

            app ??= Application.Current;
            if (app is not null)
            {
                app.UserAppTheme = mode switch
                {
                    ThemeMode.Light => AppTheme.Light,
                    ThemeMode.Dark => AppTheme.Dark,
                    _ => AppTheme.Unspecified
                };
            }
        }

        private static T Load<T>(string key, T fallback) where T : struct, Enum =>
            Enum.TryParse<T>(Preferences.Get(key, fallback.ToString()), out var value) ? value : fallback;
    }
}
