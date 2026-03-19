using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// ConfigStoreのシリアライズ/デシリアライズ ラウンドトリップテスト
/// </summary>
public class ConfigStoreTests
{
    // --- CommandList を使ったラウンドトリップ ---

    [Fact]
    public void CommandList_空オブジェクトのラウンドトリップ()
    {
        var original = new CommandList();

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<CommandList>(xml);

        restored.Commands.Should().BeEmpty();
        restored.Count.Should().Be(0);
    }

    [Fact]
    public void CommandList_コマンド付きオブジェクトのラウンドトリップ()
    {
        var original = new CommandList();
        original.Commands.Add(new Command
        {
            Name = "notepad",
            FileName = @"C:\Windows\notepad.exe",
            Param = "/test",
            WorkDir = @"C:\Windows",
            Show = WindowStyle.Maximized,
            Priority = ProcessPriorityLevel.High,
            RunAsAdmin = true,
        });
        original.Commands.Add(new Command
        {
            Name = "cmd",
            FileName = @"C:\Windows\System32\cmd.exe",
        });

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<CommandList>(xml);

        restored.Commands.Should().HaveCount(2);
        restored.Commands[0].Name.Should().Be("notepad");
        restored.Commands[0].FileName.Should().Be(@"C:\Windows\notepad.exe");
        restored.Commands[0].Param.Should().Be("/test");
        restored.Commands[0].WorkDir.Should().Be(@"C:\Windows");
        restored.Commands[0].Show.Should().Be(WindowStyle.Maximized);
        restored.Commands[0].Priority.Should().Be(ProcessPriorityLevel.High);
        restored.Commands[0].RunAsAdmin.Should().BeTrue();
        restored.Commands[1].Name.Should().Be("cmd");
    }

    // --- Config を使ったラウンドトリップ ---

    [Fact]
    public void Config_空オブジェクトのラウンドトリップ()
    {
        var original = new Config();

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<Config>(xml);

        restored.HotKey.Should().Be("Win+Space");
        restored.CommandIgnoreCase.Should().BeTrue();
        restored.LargeIcon.Should().BeTrue();
    }

    [Fact]
    public void Config_プロパティ設定済みオブジェクトのラウンドトリップ()
    {
        var original = new Config
        {
            HotKey = "Alt+Z",
            CommandIgnoreCase = false,
            LargeIcon = false,
            HideFirst = true,
            Filer = "explorer.exe",
            CloseButton = CloseButtonBehavior.Close,
            ButtonLauncherActivation = ButtonLauncherActivation.RightThenLeft,
        };

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<Config>(xml);

        restored.HotKey.Should().Be("Alt+Z");
        restored.CommandIgnoreCase.Should().BeFalse();
        restored.LargeIcon.Should().BeFalse();
        restored.HideFirst.Should().BeTrue();
        restored.Filer.Should().Be("explorer.exe");
        restored.CloseButton.Should().Be(CloseButtonBehavior.Close);
        restored.ButtonLauncherActivation.Should().Be(ButtonLauncherActivation.RightThenLeft);
    }

    // --- ファイル経由のラウンドトリップ ---

    [Fact]
    public void SerializeToFile_DeserializeFromFileのラウンドトリップ()
    {
        var original = new CommandList();
        original.Commands.Add(new Command { Name = "test", FileName = "test.exe" });

        var tmpFile = Path.GetTempFileName();
        try
        {
            original.SerializeToFile(tmpFile);
            var restored = ConfigStore.DeserializeFromFile<CommandList>(tmpFile);

            restored.Commands.Should().HaveCount(1);
            restored.Commands[0].Name.Should().Be("test");
            restored.Commands[0].FileName.Should().Be("test.exe");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    // --- SerializeToString が有効なXMLを返す ---

    [Fact]
    public void SerializeToString_XMLヘッダを含む文字列を返す()
    {
        var obj = new CommandList();
        var xml = obj.SerializeToString();

        xml.Should().StartWith("<?xml");
        xml.Should().Contain("CommandList");
    }
}
