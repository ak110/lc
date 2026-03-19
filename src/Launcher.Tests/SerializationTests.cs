using System.Xml.Serialization;
using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// enum化・property化後もXMLシリアライズ互換性が保たれることを検証
/// </summary>
public sealed class SerializationTests
{
    // --- Config ラウンドトリップ ---

    [Fact]
    public void Config_ラウンドトリップでデフォルト値が保持される()
    {
        var original = new Config();
        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<Config>(xml);

        deserialized.CloseButton.Should().Be(original.CloseButton);
        deserialized.IconDoubleClick.Should().Be(original.IconDoubleClick);
        deserialized.ItemDoubleClick.Should().Be(original.ItemDoubleClick);
        deserialized.RunAsAdminType.Should().Be(original.RunAsAdminType);
        deserialized.CommandIgnoreCase.Should().Be(original.CommandIgnoreCase);
        deserialized.HotKey.Should().Be(original.HotKey);
        deserialized.Filer.Should().Be(original.Filer);
    }

    [Fact]
    public void Config_全enum値がラウンドトリップで保持される()
    {
        var original = new Config
        {
            CloseButton = CloseButtonBehavior.Disabled,
            IconDoubleClick = TrayIconAction.ShowConfig,
            ItemDoubleClick = ItemAction.OpenDirectory,
            RunAsAdminType = AdminElevation.VistaElevator,
        };
        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<Config>(xml);

        deserialized.CloseButton.Should().Be(CloseButtonBehavior.Disabled);
        deserialized.IconDoubleClick.Should().Be(TrayIconAction.ShowConfig);
        deserialized.ItemDoubleClick.Should().Be(ItemAction.OpenDirectory);
        deserialized.RunAsAdminType.Should().Be(AdminElevation.VistaElevator);
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
        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<Command>(xml);

        deserialized.Name.Should().Be("notepad");
        deserialized.FileName.Should().Be(@"C:\Windows\notepad.exe");
        deserialized.Param.Should().Be("/test");
        deserialized.WorkDir.Should().Be(@"C:\Windows");
        deserialized.Show.Should().Be(WindowStyle.Maximized);
        deserialized.Priority.Should().Be(ProcessPriorityLevel.High);
        deserialized.RunAsAdmin.Should().BeTrue();
    }

    // --- 旧int値との互換性 ---

    [Fact]
    public void Command_旧int値のShowフィールドがenum値にデシリアライズされる()
    {
        // 旧形式: <Show>2</Show> → WindowStyle.Maximized
        var xml = """
            <?xml version="1.0"?>
            <Command>
              <Name>test</Name>
              <FileName>test.exe</FileName>
              <Param></Param>
              <WorkDir></WorkDir>
              <Show>2</Show>
              <Priority>1</Priority>
              <RunAsAdmin>false</RunAsAdmin>
            </Command>
            """;
        var command = DeserializeFromString<Command>(xml);
        command.Show.Should().Be(WindowStyle.Maximized);
        command.Priority.Should().Be(ProcessPriorityLevel.High);
    }

    [Fact]
    public void Config_旧int値のCloseButtonがenum値にデシリアライズされる()
    {
        // 旧形式: <CloseButton>0</CloseButton> → CloseButtonBehavior.Disabled
        var xml = """
            <?xml version="1.0"?>
            <Config>
              <CloseButton>0</CloseButton>
              <IconDoubleClick>1</IconDoubleClick>
              <ItemDoubleClick>2</ItemDoubleClick>
              <RunAsAdminType>2</RunAsAdminType>
            </Config>
            """;
        var config = DeserializeFromString<Config>(xml);
        config.CloseButton.Should().Be(CloseButtonBehavior.Disabled);
        config.IconDoubleClick.Should().Be(TrayIconAction.ShowConfig);
        config.ItemDoubleClick.Should().Be(ItemAction.OpenDirectory);
        config.RunAsAdminType.Should().Be(AdminElevation.VistaElevator);
    }

    // --- Config ButtonLauncherActivation ---

    [Theory]
    [InlineData(ButtonLauncherActivation.Disabled)]
    [InlineData(ButtonLauncherActivation.LeftThenRight)]
    [InlineData(ButtonLauncherActivation.RightThenLeft)]
    public void Config_ButtonLauncherActivationがラウンドトリップで保持される(ButtonLauncherActivation value)
    {
        var original = new Config { ButtonLauncherActivation = value };
        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<Config>(xml);
        deserialized.ButtonLauncherActivation.Should().Be(value);
    }

    [Fact]
    public void Config_旧UseTreeLauncher_trueがLeftThenRightにマッピングされる()
    {
        // 旧形式: <UseTreeLauncher>true</UseTreeLauncher>
        var xml = """
            <?xml version="1.0"?>
            <Config>
              <UseTreeLauncher>true</UseTreeLauncher>
            </Config>
            """;
        var config = DeserializeFromString<Config>(xml);
        config.ButtonLauncherActivation.Should().Be(ButtonLauncherActivation.LeftThenRight);
    }

    [Fact]
    public void Config_旧UseTreeLauncher_falseでDisabledのまま()
    {
        var xml = """
            <?xml version="1.0"?>
            <Config>
              <UseTreeLauncher>false</UseTreeLauncher>
            </Config>
            """;
        var config = DeserializeFromString<Config>(xml);
        config.ButtonLauncherActivation.Should().Be(ButtonLauncherActivation.Disabled);
    }

    // --- CommandList ラウンドトリップ ---

    [Fact]
    public void CommandList_ラウンドトリップで全コマンドが保持される()
    {
        var original = new CommandList();
        original.Commands.Add(new Command { Name = "alpha", FileName = "a.exe" });
        original.Commands.Add(new Command { Name = "beta", FileName = "b.exe", Show = WindowStyle.Hidden });

        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<CommandList>(xml);

        deserialized.Commands.Should().HaveCount(2);
        deserialized.Commands[0].Name.Should().Be("alpha");
        deserialized.Commands[1].Name.Should().Be("beta");
        deserialized.Commands[1].Show.Should().Be(WindowStyle.Hidden);
    }

    // --- ヘルパー ---

    private static string SerializeToString<T>(T obj)
    {
        using var writer = new StringWriter();
        var serializer = new XmlSerializer(typeof(T));
        serializer.Serialize(writer, obj);
        return writer.ToString();
    }

    private static T DeserializeFromString<T>(string xml)
    {
        using var reader = new StringReader(xml);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(reader)!;
    }
}
