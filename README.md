# iRailTracker

iRailTracker is an improved MAUI version of my old Xamarin project designed to track Irish rail journeys.

## Builds and releases

Every push to `master` is built and tested by GitHub Actions (`.github/workflows/android.yml`).
The signed APK is attached to each run as the `iRailTracker-apk` artifact.

Versions come from git: `2.<commit count>.<patch from latest tag>`, and the Android
versionCode is `major*1000000 + minor*1000 + patch` (e.g. 2.48.0 → 2048000), so every
build installs over the previous one.

To publish a release:

```bash
git tag v2.48.1 && git push origin v2.48.1
```

The workflow then creates a GitHub Release with the signed APK and a Play Store bundle (`.aab`).
If a `PLAY_SERVICE_ACCOUNT_JSON` repository secret is set (a Play Console service account key),
the bundle is also uploaded to the Play Console internal testing track.
