#if SELF_UPDATE
using Android.App;
using Android.Content;
using iRailTracker.Service;

// Lets the app hand a downloaded APK to the system installer. Only in the sideloaded APK: the
// Play Store restricts this permission and doesn't allow apps to update themselves.
[assembly: UsesPermission("android.permission.REQUEST_INSTALL_PACKAGES")]

namespace iRailTracker
{
    /// <summary>Downloads a release APK and opens the system installer on it.</summary>
    public static class AppUpdater
    {
        private const string ApkMimeType = "application/vnd.android.package-archive";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

        private static string UpdatesDirectory => Path.Combine(FileSystem.Current.CacheDirectory, "updates");

        /// <summary>Downloads the APK into the cache (covered by MAUI's FileProvider) and returns its path.</summary>
        public static async Task<string> DownloadAsync(ReleaseInfo release, IProgress<double> progress, CancellationToken ct)
        {
            Directory.CreateDirectory(UpdatesDirectory);

            // Only the newest download is ever needed
            foreach (var old in Directory.EnumerateFiles(UpdatesDirectory))
                TryDelete(old);

            var path = Path.Combine(UpdatesDirectory, $"iRailTracker-{release.Tag}.apk");
            var partial = path + ".part";

            try
            {
                using var response = await Http.GetAsync(release.ApkUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var total = response.Content.Headers.ContentLength ?? release.ApkSize;
                await using (var source = await response.Content.ReadAsStreamAsync(ct))
                await using (var target = File.Create(partial))
                {
                    var buffer = new byte[81920];
                    long received = 0;
                    int read;
                    while ((read = await source.ReadAsync(buffer, ct)) > 0)
                    {
                        await target.WriteAsync(buffer.AsMemory(0, read), ct);
                        received += read;
                        if (total > 0)
                            progress.Report((double)received / total);
                    }
                }

                if (release.ApkSize > 0 && new FileInfo(partial).Length != release.ApkSize)
                    throw new IOException("The download was incomplete. Please try again.");

                File.Move(partial, path, overwrite: true);
                return path;
            }
            catch
            {
                TryDelete(partial);
                throw;
            }
        }

        /// <summary>
        /// Opens the system installer on the APK. If installing from this app isn't allowed yet, the
        /// installer itself explains that and links to the setting.
        /// </summary>
        public static void Install(string apkPath)
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var file = new Java.IO.File(apkPath);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.PackageName + ".fileProvider", file);

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, ApkMimeType);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            if (context is not Activity)
                intent.AddFlags(ActivityFlags.NewTask);

            context.StartActivity(intent);
        }

        /// <summary>False until the user allows "Install unknown apps" for iRailTracker (Android 8+).</summary>
        public static bool CanInstallPackages =>
            !OperatingSystem.IsAndroidVersionAtLeast(26) ||
            Android.App.Application.Context.PackageManager?.CanRequestPackageInstalls() == true;

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
#endif
