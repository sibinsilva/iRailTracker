using iRailTracker.Service;

namespace iRailTracker.UnitTests.Service;

public class UpdateServiceTests
{
    private const string Release = """
        {
          "tag_name": "v2.63.0",
          "html_url": "https://github.com/sibinsilva/iRailTracker/releases/tag/v2.63.0",
          "draft": false,
          "prerelease": false,
          "body": "## What's Changed\r\n* Add in-app updates by @sibinsilva in https://github.com/sibinsilva/iRailTracker/pull/12\r\n\r\n**Full Changelog**: https://github.com/sibinsilva/iRailTracker/compare/v2.62.1...v2.63.0",
          "assets": [
            { "name": "iRailTracker-v2.63.0.aab", "browser_download_url": "https://example.test/a.aab", "size": 45000000 },
            { "name": "iRailTracker-v2.63.0.apk", "browser_download_url": "https://example.test/a.apk", "size": 27555218 }
          ]
        }
        """;

    [Theory]
    [InlineData("v2.62.1", 2, 62, 1)]
    [InlineData("2.62", 2, 62, 0)]
    [InlineData("V3.0.0-beta", 3, 0, 0)]
    [InlineData("iRailTracker 2.62.1", 2, 62, 1)]
    public void ParseVersion_ReadsTags(string tag, int major, int minor, int patch) =>
        Assert.Equal(new Version(major, minor, patch), UpdateService.ParseVersion(tag));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    public void ParseVersion_ReturnsNullWithoutANumber(string? tag) =>
        Assert.Null(UpdateService.ParseVersion(tag));

    [Fact]
    public void ParseRelease_PicksTheApkNotTheBundle()
    {
        var release = UpdateService.ParseRelease(Release);

        Assert.NotNull(release);
        Assert.Equal(new Version(2, 63, 0), release.Version);
        Assert.Equal("v2.63.0", release.Tag);
        Assert.Equal("https://example.test/a.apk", release.ApkUrl);
        Assert.Equal(27555218, release.ApkSize);
    }

    [Fact]
    public void ParseRelease_CleansGeneratedNotes()
    {
        var release = UpdateService.ParseRelease(Release);

        Assert.Equal("• Add in-app updates", release!.Notes);
    }

    [Theory]
    [InlineData("\"draft\": false", "\"draft\": true")]
    [InlineData("\"prerelease\": false", "\"prerelease\": true")]
    [InlineData(".apk\"", ".zip\"")]
    public void ParseRelease_IgnoresDraftsPrereleasesAndReleasesWithoutAnApk(string from, string to) =>
        Assert.Null(UpdateService.ParseRelease(Release.Replace(from, to)));

    [Fact]
    public void ParseRelease_ReturnsNullForBadJson() =>
        Assert.Null(UpdateService.ParseRelease("{ not json"));

    [Theory]
    [InlineData("2.62.1", true)]
    [InlineData("2.62.9", true)]
    [InlineData("2.63", false)]   // same version: a missing patch number counts as .0
    [InlineData("2.63.0", false)]
    [InlineData("2.63.1", false)]
    [InlineData("3.0.0", false)]
    public void IsNewer_ComparesAgainstTheInstalledVersion(string installed, bool expected) =>
        Assert.Equal(expected, UpdateService.IsNewer(UpdateService.ParseRelease(Release)!, Version.Parse(installed)));

    [Fact]
    public void CleanNotes_TruncatesLongNotes()
    {
        var notes = UpdateService.CleanNotes(new string('x', 1000), maxLength: 100);

        Assert.Equal(101, notes.Length);
        Assert.EndsWith("…", notes);
    }
}
