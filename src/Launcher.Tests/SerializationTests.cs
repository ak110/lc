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
    [Theory]
    [InlineData(0, ItemAction.Execute)]
    [InlineData(1, ItemAction.OpenDirectory)]
    [InlineData(2, ItemAction.EditCommand)]
    [InlineData(3, ItemAction.Delete)]
    public void 旧アイテム動作の数値は同じ操作に対応する(int number, ItemAction expected)
    {
        DeserializeFromString<Config>($"<Config><ItemDoubleClick>{number}</ItemDoubleClick></Config>")
            .ItemDoubleClick.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, CloseButtonBehavior.Disabled)]
    [InlineData(1, CloseButtonBehavior.Hide)]
    [InlineData(2, CloseButtonBehavior.Exit)]
    public void 旧閉じる動作の数値は同じ操作に対応する(int number, CloseButtonBehavior expected)
    {
        DeserializeFromString<Config>($"<Config><CloseButton>{number}</CloseButton></Config>")
            .CloseButton.Should().Be(expected);
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
        config.ItemDoubleClick.Should().Be(ItemAction.EditCommand);
        config.RunAsAdminType.Should().Be(AdminElevation.VistaElevator);
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

    private static T DeserializeFromString<T>(string xml)
    {
        using var reader = new StringReader(xml);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(reader)!;
    }
}
