using System.Reflection;
using FluentAssertions;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

public sealed class AppVersionTests
{
    [Fact]
    public void 製品の版を起動ホストの版と区別してビルド情報を除いて取得する()
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
    public void Title_ShouldStartWithExpectedPrefix()
    {
        AppVersion.Title.Should().StartWith("らんちゃ v");
    }
}
