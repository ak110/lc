using System.Reflection;
using FluentAssertions;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

public sealed class AppVersionTests
{
    [Fact]
    public void 製品の版を起動ホストの版と区別して取得する()
    {
        string version = typeof(ApplicationHostForm).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
        AppVersion.VersionString.Should().Be(version);
    }

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
