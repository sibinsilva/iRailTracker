using System.Text;

namespace iRailTracker.Service
{
    /// <summary>
    /// Records unhandled .NET exceptions to a file on the device, which Settings can share, so a
    /// crash can be diagnosed without adb. Native crashes (e.g. in the renderer) kill the process
    /// before managed code runs and are not captured.
    /// </summary>
    public static class CrashLog
    {
        private const int MaxBytes = 100_000;
        private static readonly object Gate = new();
        private static bool _initialized;

        public static string FilePath => Path.Combine(FileSystem.Current.AppDataDirectory, "crash.log");

        public static void Init()
        {
            if (_initialized)
                return;
            _initialized = true;

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Write("Unhandled exception", e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, e) =>
                Write("Unobserved task exception", e.Exception);
        }

        public static void Write(string kind, Exception? exception)
        {
            try
            {
                var entry = new StringBuilder()
                    .AppendLine($"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} · {kind} · v{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString}) · Android {DeviceInfo.Current.VersionString} · {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}")
                    .AppendLine(exception?.ToString() ?? "(no exception details)")
                    .AppendLine()
                    .ToString();

                lock (Gate)
                {
                    File.AppendAllText(FilePath, entry);

                    // Keep only the newest ~100 KB
                    var info = new FileInfo(FilePath);
                    if (info.Length > MaxBytes)
                    {
                        var text = File.ReadAllText(FilePath);
                        File.WriteAllText(FilePath, text[^(MaxBytes / 2)..]);
                    }
                }
            }
            catch
            {
                // Never let crash logging itself crash
            }
        }

        public static bool HasEntries
        {
            get
            {
                try
                {
                    return File.Exists(FilePath) && new FileInfo(FilePath).Length > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static void Clear()
        {
            try
            {
                lock (Gate)
                    File.Delete(FilePath);
            }
            catch (IOException)
            {
            }
        }
    }
}
