using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

public class AppVersionTests
{
    [Fact]
    public void VersionString_ShouldNotBeNullOrEmpty()
    {
        AppVersion.VersionString.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VersionString_ShouldNotContainPlusSign()
    {
        // +以降のcommithash部分が除去されていること
        AppVersion.VersionString.Should().NotContain("+");
    }

    [Fact]
    public void TagName_ShouldStartWithV()
    {
        AppVersion.TagName.Should().StartWith("v");
    }

    [Fact]
    public void Title_ShouldStartWithExpectedPrefix()
    {
        AppVersion.Title.Should().StartWith("らんちゃ v");
    }

    [Fact]
    public void TagName_ShouldBeVPrefixedVersionString()
    {
        AppVersion.TagName.Should().Be("v" + AppVersion.VersionString);
    }
}
