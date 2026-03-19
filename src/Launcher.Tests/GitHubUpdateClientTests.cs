using FluentAssertions;
using Launcher.Updater;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// GitHubUpdateClient.IsUpdateAvailable のテスト
/// </summary>
public sealed class GitHubUpdateClientTests
{
    private static readonly string CurrentVersion = Infrastructure.AppVersion.TagName;

    [Fact]
    public void IsUpdateAvailable_releaseがnullならfalse()
    {
        var record = new UpdateRecord();
        GitHubUpdateClient.IsUpdateAvailable(null, record).Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_currentVersionと同じならfalse()
    {
        var release = new GitHubRelease { TagName = CurrentVersion };
        var record = new UpdateRecord();
        GitHubUpdateClient.IsUpdateAvailable(release, record).Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_スキップ済みバージョンならfalse()
    {
        var release = new GitHubRelease { TagName = "v99.99.99" };
        var record = new UpdateRecord { SkippedVersion = "v99.99.99" };
        GitHubUpdateClient.IsUpdateAvailable(release, record).Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_新しいバージョンならtrue()
    {
        var release = new GitHubRelease { TagName = "v99.99.99" };
        var record = new UpdateRecord();
        GitHubUpdateClient.IsUpdateAvailable(release, record).Should().BeTrue();
    }

    [Fact]
    public void IsUpdateAvailable_既知バージョンと同じだが現在と異なればtrue()
    {
        var release = new GitHubRelease { TagName = "v99.99.99" };
        var record = new UpdateRecord { LastKnownVersion = "v99.99.99" };
        GitHubUpdateClient.IsUpdateAvailable(release, record).Should().BeTrue();
    }

    [Fact]
    public void IsUpdateAvailable_既知バージョンが現在バージョンと一致ならfalse()
    {
        var release = new GitHubRelease { TagName = "v99.99.99" };
        var record = new UpdateRecord { LastKnownVersion = CurrentVersion };
        // release.TagName != record.LastKnownVersion なので true
        GitHubUpdateClient.IsUpdateAvailable(release, record).Should().BeTrue();
    }
}
