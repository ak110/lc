using FluentAssertions;
using Launcher.Core;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

public sealed class CommandFactoryTests
{
    [Fact]
    public void ショートカットの属性からコマンドを作る()
    {
        var command = CommandFactory.CreateFromShortcutProperties(
            @"C:\Tools\app.exe", "-x", @"D:\Work", false, ShellLink.ShellLinkDisplayMode.Normal);

        command.Name.Should().Be("app");
        command.FileName.Should().Be(@"C:\Tools\app.exe");
        command.Param.Should().Be("-x");
        command.WorkDir.Should().Be(@"D:\Work");
        command.Show.Should().Be(WindowStyle.Normal);
    }

    [Fact]
    public void 作業フォルダーがターゲットの親と一致するドライブパスなら作業フォルダーを省く()
    {
        var command = CommandFactory.CreateFromShortcutProperties(
            @"C:\Tools\app.exe", null, @"C:\Tools", true, ShellLink.ShellLinkDisplayMode.Normal);

        command.WorkDir.Should().BeNull();
        command.Param.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ShellLink.ShellLinkDisplayMode.Maximized, WindowStyle.Maximized)]
    [InlineData(ShellLink.ShellLinkDisplayMode.Minimized, WindowStyle.Minimized)]
    public void 表示モードを起動時のウィンドウ状態へ変換する(ShellLink.ShellLinkDisplayMode displayMode, WindowStyle expected)
    {
        var command = CommandFactory.CreateFromShortcutProperties(
            @"C:\Tools\app.exe", null, @"D:\Work", false, displayMode);

        command.Show.Should().Be(expected);
    }
}
