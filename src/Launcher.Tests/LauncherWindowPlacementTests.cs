using System.Drawing;
using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class LauncherWindowPlacementTests
{
    [Theory]
    [InlineData(-100000, -100000)]
    [InlineData(100000, 100000)]
    public void 画面外の保存位置から起動して再表示してもBoundsが作業領域内に収まる(int x, int y) => UiAcceptance.Run(() =>
    {
        using var data = new UiHostData(new Config
        {
            TrayIcon = false,
            HotKey = "",
            WindowHideNoActive = false,
            WindowPos = new Point(x, y),
            WindowSize = new Size(400, 350),
        });
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
            launcher.Visible.Should().BeTrue();
            Screen.AllScreens.Any(screen => screen.WorkingArea.Contains(launcher.Bounds)).Should().BeTrue();
            launcher.HideWindow();
            host.Config.WindowPos = new Point(x, y);
            launcher.ShowWindow();
            launcher.Visible.Should().BeTrue();
            Screen.AllScreens.Any(screen => screen.WorkingArea.Contains(launcher.Bounds)).Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });
}
