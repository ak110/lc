using System.Drawing;
using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// Configのデフォルト値・シリアライズテスト
/// </summary>
public sealed class ConfigTests
{
    // --- デフォルト値の検証 ---

    [Fact]
    public void デフォルト値_Debug()
    {
        var config = new Config();
        config.Debug.Should().BeFalse();
    }

    [Fact]
    public void デフォルト値_IconDoubleClick()
    {
        var config = new Config();
        config.IconDoubleClick.Should().Be(TrayIconAction.ShowHide);
    }

    [Fact]
    public void デフォルト値_ItemDoubleClick()
    {
        var config = new Config();
        config.ItemDoubleClick.Should().Be(ItemAction.Execute);
    }

    [Fact]
    public void デフォルト値_ProcessPriority()
    {
        var config = new Config();
        config.ProcessPriority.Should().Be(3);
    }

    [Fact]
    public void デフォルト値_HotKey()
    {
        var config = new Config();
        config.HotKey.Should().Be("Win+Space");
    }

    [Fact]
    public void デフォルト値_Filer関連()
    {
        var config = new Config();
        config.OpenDirByFiler.Should().BeTrue();
        config.Filer.Should().Be("Explorer.exe");
        config.OpenParentFiler.Should().Be("Explorer.exe");
        config.OpenParentFilerParam1.Should().Be("/select,");
        config.OpenParentFilerParam2.Should().BeEmpty();
    }

    [Fact]
    public void デフォルト値_ウィンドウ設定()
    {
        var config = new Config();
        config.LargeIcon.Should().BeTrue();
        config.TrayIcon.Should().BeTrue();
        config.CloseButton.Should().Be(CloseButtonBehavior.Hide);
        config.WindowNoResize.Should().BeFalse();
        config.WindowTopMost.Should().BeFalse();
        config.WindowHideNoActive.Should().BeTrue();
        config.HideOnRun.Should().BeFalse();
        config.CommandIgnoreCase.Should().BeTrue();
    }

    [Fact]
    public void デフォルト値_WindowPosとSize()
    {
        var config = new Config();
        config.WindowPos.Should().Be(new Point(200, 125));
        config.WindowSize.Should().Be(new Size(400, 350));
    }

    [Fact]
    public void デフォルト値_管理者権限関連()
    {
        var config = new Config();
        config.RunAsAdminType.Should().Be(AdminElevation.RunAs);
        config.RunAsCommandLine.Should().Be("/user:Administrator /savecred");
    }

    [Fact]
    public void デフォルト値_ButtonLauncherActivation()
    {
        var config = new Config();
        config.ButtonLauncherActivation.Should().Be(ButtonLauncherActivation.Disabled);
    }

    [Fact]
    public void デフォルト値_ReplaceEnv()
    {
        var config = new Config();
        config.ReplaceEnv.Should().BeEmpty();
    }

    // --- シリアライズ/デシリアライズ ラウンドトリップ ---

    [Fact]
    public void ラウンドトリップ_全プロパティが保持される()
    {
        var original = new Config
        {
            Debug = true,
            IconDoubleClick = TrayIconAction.ShowConfig,
            ItemDoubleClick = ItemAction.OpenDirectory,
            ProcessPriority = 1,
            HideFirst = true,
            HotKey = "Ctrl+Alt+L",
            OpenDirByFiler = false,
            Filer = "TotalCmd.exe",
            OpenParentFiler = "TotalCmd.exe",
            OpenParentFilerParam1 = "/O /T ",
            OpenParentFilerParam2 = "",
            LargeIcon = false,
            TrayIcon = false,
            ReplaceEnv = ["SystemRoot", "ProgramFiles"],
            CloseButton = CloseButtonBehavior.Close,
            WindowNoResize = true,
            WindowTopMost = true,
            WindowHideNoActive = false,
            HideOnRun = true,
            CommandIgnoreCase = false,
            WindowPos = new Point(100, 200),
            WindowSize = new Size(800, 600),
            RunAsAdminType = AdminElevation.VistaElevator,
            RunAsCommandLine = "/user:Admin",
            ButtonLauncherActivation = ButtonLauncherActivation.RightThenLeft,
        };

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<Config>(xml);

        restored.Debug.Should().Be(original.Debug);
        restored.IconDoubleClick.Should().Be(original.IconDoubleClick);
        restored.ItemDoubleClick.Should().Be(original.ItemDoubleClick);
        restored.ProcessPriority.Should().Be(original.ProcessPriority);
        restored.HideFirst.Should().Be(original.HideFirst);
        restored.HotKey.Should().Be(original.HotKey);
        restored.OpenDirByFiler.Should().Be(original.OpenDirByFiler);
        restored.Filer.Should().Be(original.Filer);
        restored.OpenParentFiler.Should().Be(original.OpenParentFiler);
        restored.OpenParentFilerParam1.Should().Be(original.OpenParentFilerParam1);
        restored.OpenParentFilerParam2.Should().Be(original.OpenParentFilerParam2);
        restored.LargeIcon.Should().Be(original.LargeIcon);
        restored.TrayIcon.Should().Be(original.TrayIcon);
        restored.ReplaceEnv.Should().BeEquivalentTo(original.ReplaceEnv);
        restored.CloseButton.Should().Be(original.CloseButton);
        restored.WindowNoResize.Should().Be(original.WindowNoResize);
        restored.WindowTopMost.Should().Be(original.WindowTopMost);
        restored.WindowHideNoActive.Should().Be(original.WindowHideNoActive);
        restored.HideOnRun.Should().Be(original.HideOnRun);
        restored.CommandIgnoreCase.Should().Be(original.CommandIgnoreCase);
        restored.WindowPos.Should().Be(original.WindowPos);
        restored.WindowSize.Should().Be(original.WindowSize);
        restored.RunAsAdminType.Should().Be(original.RunAsAdminType);
        restored.RunAsCommandLine.Should().Be(original.RunAsCommandLine);
        restored.ButtonLauncherActivation.Should().Be(original.ButtonLauncherActivation);
    }

    // --- Clone ---

    [Fact]
    public void Clone_全プロパティがコピーされる()
    {
        var original = new Config
        {
            HotKey = "Ctrl+Space",
            CommandIgnoreCase = false,
            CloseButton = CloseButtonBehavior.Close,
        };

        var clone = original.Clone();

        clone.HotKey.Should().Be("Ctrl+Space");
        clone.CommandIgnoreCase.Should().BeFalse();
        clone.CloseButton.Should().Be(CloseButtonBehavior.Close);
    }

    [Fact]
    public void Clone_変更が元に影響しない()
    {
        var original = new Config { HotKey = "Win+Space" };
        var clone = original.Clone();

        clone.HotKey = "Alt+Z";

        original.HotKey.Should().Be("Win+Space");
    }

    [Fact]
    public void Clone_ReplaceEnvがディープコピーされる()
    {
        var original = new Config
        {
            ReplaceEnv = ["SystemRoot", "ProgramFiles"],
        };

        var clone = original.Clone();

        // clone側を変更しても元に影響しないことを確認
        clone.ReplaceEnv.Add("TEMP");
        clone.ReplaceEnv.Remove("SystemRoot");

        original.ReplaceEnv.Should().HaveCount(2);
        original.ReplaceEnv[0].Should().Be("SystemRoot");
        original.ReplaceEnv[1].Should().Be("ProgramFiles");
    }
}
