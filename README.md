# iRailTracker

iRailTracker is an improved MAUI version of my old Xamarin project designed to track Irish rail journeys.

## Builds and releases

Every push to `master` is built and tested by GitHub Actions (`.github/workflows/android.yml`).
The signed APK is attached to each run as the `iRailTracker-apk` artifact.

The version shown in the app comes from the latest `vX.Y.Z` tag, or from `VersionMajor`,
`VersionMinor` and `VersionPatch` in `iRailTracker.csproj` when there is none. The Android
versionCode is `2100000 + commit count`, so every build installs over the previous one.
"What's New" appears only when `ChangelogPage.xaml.cs` gets a new top entry.

To publish a release (add a What's New entry first if it has user-visible changes):

```bash
git tag v2.61.0 && git push origin v2.61.0
```

The workflow then creates a GitHub Release with the signed APK and a Play Store bundle (`.aab`).
If a `PLAY_SERVICE_ACCOUNT_JSON` repository secret is set (a Play Console service account key),
the bundle is also uploaded to the Play Console internal testing track.
