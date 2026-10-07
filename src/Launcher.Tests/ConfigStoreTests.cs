using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// ConfigStoreのシリアライズ/デシリアライズ ラウンドトリップテスト
/// </summary>
public sealed class ConfigStoreTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void 既存XMLの数値優先度を同じ数値で保存する(int value)
    {
        var config = ConfigStore.DeserializeFromString<Config>(
            $"<Config><ProcessPriority>{value}</ProcessPriority></Config>");
        ((int)config.ProcessPriority).Should().Be(value);
        config.SerializeToString().Should().Contain($"<ProcessPriority>{value}</ProcessPriority>");
    }

    [Theory]
    [InlineData(ItemAction.Execute, 0)]
    [InlineData(ItemAction.OpenDirectory, 1)]
    [InlineData(ItemAction.EditCommand, 2)]
    [InlineData(ItemAction.Delete, 3)]
    public void アイテム動作は既存の数値で保存し再読込できる(ItemAction action, int value)
    {
        var xml = new Config { ItemDoubleClick = action }.SerializeToString();
        xml.Should().Contain($"<ItemDoubleClick>{value}</ItemDoubleClick>");
        ConfigStore.DeserializeFromString<Config>(xml).ItemDoubleClick.Should().Be(action);
    }

    [Theory]
    [InlineData(CloseButtonBehavior.Disabled, 0)]
    [InlineData(CloseButtonBehavior.Hide, 1)]
    [InlineData(CloseButtonBehavior.Exit, 2)]
    public void 閉じる動作は既存の数値で保存し再読込できる(CloseButtonBehavior action, int value)
    {
        var xml = new Config { CloseButton = action }.SerializeToString();
        xml.Should().Contain($"<CloseButton>{value}</CloseButton>");
        ConfigStore.DeserializeFromString<Config>(xml).CloseButton.Should().Be(action);
    }

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

    // --- シリアライズ/デシリアライズ ラウンドトリップ ---

    [Fact]
    public void ラウンドトリップ_全プロパティが保持される()
    {
        var original = new Config
        {
            Debug = true,
            IconDoubleClick = TrayIconAction.ShowConfig,
            ItemDoubleClick = ItemAction.EditCommand,
            ProcessPriority = ProcessPriorityLevel.High,
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
            CloseButton = CloseButtonBehavior.Hide,
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

    // --- ファイル経由のラウンドトリップ ---

    [Fact]
    public void 保存窓口を通したファイルのラウンドトリップ()
    {
        var original = new CommandList();
        original.Commands.Add(new Command { Name = "test", FileName = "test.exe" });

        var baseName = Path.Combine(Path.GetTempPath(), "lc_store_" + Guid.NewGuid().ToString("N"));
        var store = new ConfigFile<CommandList>(".cmd.cfg");
        try
        {
            store.Save(original, _ => Assert.Fail("保存に失敗した"), baseName).Should().BeTrue();
            var restored = store.Load(baseName).Value;

            restored.Commands.Should().HaveCount(1);
            restored.Commands[0].Name.Should().Be("test");
            restored.Commands[0].FileName.Should().Be("test.exe");
        }
        finally
        {
            File.Delete(store.FileName(baseName));
            File.Delete(store.FileName(baseName) + ".bak");
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
    // --- Command ラウンドトリップ ---

    [Fact]
    public void Command_ラウンドトリップで全フィールドが保持される()
    {
        var original = new Command
        {
            Name = "notepad",
            FileName = @"C:\Windows\notepad.exe",
            Param = "/test",
            WorkDir = @"C:\Windows",
            Show = WindowStyle.Maximized,
            Priority = ProcessPriorityLevel.High,
            RunAsAdmin = true,
        };
        var xml = SerializeXml(original);
        var deserialized = ConfigStore.DeserializeFromString<Command>(xml);

        deserialized.Name.Should().Be("notepad");
        deserialized.FileName.Should().Be(@"C:\Windows\notepad.exe");
        deserialized.Param.Should().Be("/test");
        deserialized.WorkDir.Should().Be(@"C:\Windows");
        deserialized.Show.Should().Be(WindowStyle.Maximized);
        deserialized.Priority.Should().Be(ProcessPriorityLevel.High);
        deserialized.RunAsAdmin.Should().BeTrue();
    }

    // --- Config ButtonLauncherActivation ---

    [Theory]
    [InlineData(ButtonLauncherActivation.Disabled)]
    [InlineData(ButtonLauncherActivation.LeftThenRight)]
    [InlineData(ButtonLauncherActivation.RightThenLeft)]
    public void Config_ButtonLauncherActivationがラウンドトリップで保持される(ButtonLauncherActivation value)
    {
        var original = new Config { ButtonLauncherActivation = value };
        var xml = SerializeXml(original);
        var deserialized = ConfigStore.DeserializeFromString<Config>(xml);
        deserialized.ButtonLauncherActivation.Should().Be(value);
    }

    static string SerializeXml<T>(T value)
    {
        using var writer = new StringWriter();
        new System.Xml.Serialization.XmlSerializer(typeof(T)).Serialize(writer, value);
        return writer.ToString();
    }
}
