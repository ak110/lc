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
        GitHubUpdateClient.IsUpdateAvailable(null).Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_currentVersionと同じならfalse()
    {
        var release = new GitHubRelease { TagName = CurrentVersion };
        GitHubUpdateClient.IsUpdateAvailable(release).Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_異なるバージョンならtrue()
    {
        var release = new GitHubRelease { TagName = "v99.99.99" };
        GitHubUpdateClient.IsUpdateAvailable(release).Should().BeTrue();
    }
}
