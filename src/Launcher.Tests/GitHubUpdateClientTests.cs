using FluentAssertions;
using Launcher.Updater;
using Xunit;

namespace Launcher.Tests;

public sealed class GitHubUpdateClientTests
{
    private static readonly string CurrentVersion = "v" + Infrastructure.AppVersion.VersionString;

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

    [Theory]
    [InlineData("v0.0.0")]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("v99.99")]
    [InlineData("v99.99.99.1")]
    [InlineData("v99.99.99-rc.1")]
    [InlineData("99.99.99")]
    public void IsUpdateAvailable_旧版と解釈できないタグは案内しない(string tag)
    {
        var release = new GitHubRelease { TagName = tag };
        GitHubUpdateClient.IsUpdateAvailable(release).Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_版の各桁を数値の大小で比較する()
    {
        var current = Version.Parse(Infrastructure.AppVersion.VersionString);
        var newerTags = new[]
        {
            $"v{current.Major + 1}.0.0",
            $"v{current.Major}.{current.Minor + 1}.0",
            $"v{current.Major}.{current.Minor}.{current.Build + 1}",
        };
        foreach (var tag in newerTags)
        {
            GitHubUpdateClient.IsUpdateAvailable(new GitHubRelease { TagName = tag }).Should().BeTrue();
        }
        if (current.Major > 0)
        {
            GitHubUpdateClient.IsUpdateAvailable(new GitHubRelease
            {
                TagName = $"v{current.Major - 1}.999.999",
            }).Should().BeFalse();
        }
        if (current.Minor > 0)
        {
            GitHubUpdateClient.IsUpdateAvailable(new GitHubRelease
            {
                TagName = $"v{current.Major}.{current.Minor - 1}.999",
            }).Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("v1.2.4", "1.2.3", true)]
    [InlineData("v1.2.3", "1.2.3", false)]
    [InlineData("v1.2.2", "1.2.3", false)]
    [InlineData("v1.2.4.1", "1.2.3", false)]
    [InlineData("1.2.4", "1.2.3", false)]
    public void IsUpdateAvailable_現在の版と比べて新しい配布版だけを案内する(string tag, string currentVersion, bool expected)
    {
        GitHubUpdateClient.IsUpdateAvailable(new GitHubRelease { TagName = tag }, currentVersion).Should().Be(expected);
    }
}
