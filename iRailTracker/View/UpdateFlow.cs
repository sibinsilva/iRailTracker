using iRailTracker.Service;

namespace iRailTracker.View
{
    /// <summary>
    /// The in-app side of self-updating: offers a new release in a dialog, then downloads it with
    /// a progress page and opens the system installer. Does nothing in builds without SELF_UPDATE.
    /// </summary>
    public static class UpdateFlow
    {
        private static bool _busy;

        /// <summary>Quiet check at app start (at most every few hours); errors are ignored.</summary>
        public static async Task CheckOnStartupAsync()
        {
            if (!UpdateService.IsSupported || !UpdateService.IsCheckDue())
                return;

            try
            {
                var release = await UpdateService.GetAvailableUpdateAsync();
                if (release is not null)
                    await MainThread.InvokeOnMainThreadAsync(() => OfferAsync(release));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup update check failed: {ex.Message}");
            }
        }

        /// <summary>Settings → Check for updates, and taps on the "update available" notification.</summary>
        public static async Task CheckNowAsync()
        {
            if (!UpdateService.IsSupported || _busy)
                return;

            ReleaseInfo? release;
            try
            {
                release = await UpdateService.GetAvailableUpdateAsync();
            }
            catch (Exception)
            {
                await AlertAsync("Couldn't check for updates", "Check your internet connection and try again.");
                return;
            }

            if (release is null)
                await AlertAsync("You're up to date", $"iRailTracker {AppInfo.Current.VersionString} is the latest version.");
            else
                await OfferAsync(release);
        }

        private static async Task OfferAsync(ReleaseInfo release)
        {
            var page = CurrentPage;
            if (page is null || _busy)
                return;

            _busy = true;
            try
            {
                var message = $"Version {release.Version} is available (you have {AppInfo.Current.VersionString}).";
                if (release.Notes.Length > 0)
                    message += $"\n\nWhat's new:\n{release.Notes}";

                if (!await page.DisplayAlertAsync("Update available", message, "Update", "Later"))
                    return;

                await DownloadAndInstallAsync(page, release);
            }
            finally
            {
                _busy = false;
            }
        }

        private static async Task DownloadAndInstallAsync(Page page, ReleaseInfo release)
        {
#if SELF_UPDATE
            if (!AppUpdater.CanInstallPackages &&
                !await page.DisplayAlertAsync("One more step",
                    "Android will ask you to allow iRailTracker to install apps. Turn on \"Allow from this source\", then go back to finish updating.",
                    "Continue", "Cancel"))
            {
                return;
            }

            var downloadPage = new UpdateDownloadPage(release);
            await page.Navigation.PushModalAsync(downloadPage);
            var apkPath = await downloadPage.Result;
            await page.Navigation.PopModalAsync();

            if (apkPath is null)
                return;

            Exception? installError = null;
            try
            {
                AppUpdater.Install(apkPath);
            }
            catch (Exception ex)
            {
                installError = ex;
            }

            if (installError is not null)
            {
                CrashLog.Write("Opening the installer failed", installError);
                if (await page.DisplayAlertAsync("Couldn't open the installer",
                        $"Download version {release.Version} from GitHub instead?", "Open GitHub", "Cancel") &&
                    release.PageUrl.Length > 0)
                {
                    await Launcher.OpenAsync(release.PageUrl);
                }
            }
#else
            await Task.CompletedTask;
#endif
        }

        private static Task AlertAsync(string title, string message) =>
            CurrentPage?.DisplayAlertAsync(title, message, "OK") ?? Task.CompletedTask;

        private static Page? CurrentPage
        {
            get
            {
                var root = Application.Current?.Windows.FirstOrDefault()?.Page;
                return root?.Navigation.ModalStack.LastOrDefault() ?? root;
            }
        }
    }

#if SELF_UPDATE
    /// <summary>Modal "Downloading update… 42%" page with a Cancel button.</summary>
    public sealed class UpdateDownloadPage : ContentPage
    {
        private readonly TaskCompletionSource<string?> _result = new();
        private readonly CancellationTokenSource _cts = new();

        /// <summary>The downloaded APK's path, or null if cancelled or failed.</summary>
        public Task<string?> Result => _result.Task;

        public UpdateDownloadPage(ReleaseInfo release)
        {
            var status = new Label
            {
                Text = $"Downloading iRailTracker {release.Version}…",
                FontSize = 16,
                FontFamily = "OpenSansSemibold",
                HorizontalTextAlignment = TextAlignment.Center
            };
            var percent = new Label { Text = "0%", HorizontalTextAlignment = TextAlignment.Center, Opacity = 0.7 };
            var bar = new ProgressBar { Progress = 0 };
            var cancel = new Button { Text = "Cancel", HorizontalOptions = LayoutOptions.Center, Padding = new Thickness(24, 8) };
            cancel.Clicked += (_, _) => _cts.Cancel();

            SemanticProperties.SetDescription(bar, "Download progress");

            Content = new VerticalStackLayout
            {
                Padding = new Thickness(32),
                Spacing = 16,
                VerticalOptions = LayoutOptions.Center,
                Children = { status, bar, percent, cancel }
            };

            var progress = new Progress<double>(p =>
            {
                bar.Progress = p;
                percent.Text = $"{p:P0}";
            });

            _ = RunAsync(release, progress, status, cancel);
        }

        private async Task RunAsync(ReleaseInfo release, IProgress<double> progress, Label status, Button cancel)
        {
            try
            {
                _result.TrySetResult(await AppUpdater.DownloadAsync(release, progress, _cts.Token));
            }
            catch (OperationCanceledException)
            {
                _result.TrySetResult(null);
            }
            catch (Exception ex)
            {
                CrashLog.Write("Update download failed", ex);
                status.Text = "The download failed. Check your connection and try again.";
                cancel.Text = "Close";
                cancel.Clicked += (_, _) => _result.TrySetResult(null);
            }
        }

        // Back button behaves like Cancel
        protected override bool OnBackButtonPressed()
        {
            _cts.Cancel();
            _result.TrySetResult(null);
            return true;
        }
    }
#endif
}
