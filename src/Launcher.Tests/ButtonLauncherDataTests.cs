using System.Drawing;
using System.Xml.Serialization;
using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// ButtonLauncherDataのシリアライズ・ロジックテスト
/// </summary>
public class ButtonLauncherDataTests
{
    [Fact]
    public void ButtonLauncherData_ラウンドトリップでデフォルト値が保持される()
    {
        var original = new ButtonLauncherData();
        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<ButtonLauncherData>(xml);

        deserialized.DefaultTabIndex.Should().Be(0);
        deserialized.Columns.Should().Be(7);
        deserialized.Rows.Should().Be(7);
        deserialized.IsLocked.Should().BeFalse();
        deserialized.Tabs.Should().BeEmpty();
    }

    [Fact]
    public void ButtonLauncherData_タブとボタンがラウンドトリップで保持される()
    {
        var original = new ButtonLauncherData
        {
            Columns = 5,
            Rows = 4,
            DefaultTabIndex = 1,
            IsLocked = true,
            WindowPos = new Point(100, 200),
            WindowSize = new Size(600, 400),
        };
        var tab = new ButtonTab { Name = "メイン" };
        tab.Buttons.Add(new ButtonEntry
        {
            Row = 0,
            Col = 2,
            Name = "notepad",
            FileName = @"C:\Windows\notepad.exe",
            Param = "/test",
            Show = WindowStyle.Maximized,
            Priority = ProcessPriorityLevel.High,
            RunAsAdmin = true,
        });
        tab.Buttons.Add(new ButtonEntry
        {
            Row = 3,
            Col = 1,
            Name = "calc",
            FileName = "calc.exe",
        });
        original.Tabs.Add(tab);
        original.Tabs.Add(new ButtonTab { Name = "サブ" });

        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<ButtonLauncherData>(xml);

        deserialized.Columns.Should().Be(5);
        deserialized.Rows.Should().Be(4);
        deserialized.DefaultTabIndex.Should().Be(1);
        deserialized.IsLocked.Should().BeTrue();
        deserialized.WindowPos.Should().Be(new Point(100, 200));
        deserialized.WindowSize.Should().Be(new Size(600, 400));
        deserialized.Tabs.Should().HaveCount(2);
        deserialized.Tabs[0].Name.Should().Be("メイン");
        deserialized.Tabs[0].Buttons.Should().HaveCount(2);

        var btn0 = deserialized.Tabs[0].Buttons[0];
        btn0.Row.Should().Be(0);
        btn0.Col.Should().Be(2);
        btn0.Name.Should().Be("notepad");
        btn0.FileName.Should().Be(@"C:\Windows\notepad.exe");
        btn0.Param.Should().Be("/test");
        btn0.Show.Should().Be(WindowStyle.Maximized);
        btn0.Priority.Should().Be(ProcessPriorityLevel.High);
        btn0.RunAsAdmin.Should().BeTrue();

        var btn1 = deserialized.Tabs[0].Buttons[1];
        btn1.Row.Should().Be(3);
        btn1.Col.Should().Be(1);
        btn1.Name.Should().Be("calc");

        deserialized.Tabs[1].Name.Should().Be("サブ");
        deserialized.Tabs[1].Buttons.Should().BeEmpty();
    }

    [Fact]
    public void ButtonEntry_IsEmptyはFileNameが空なら真()
    {
        new ButtonEntry { FileName = null! }.IsEmpty.Should().BeTrue();
        new ButtonEntry { FileName = "" }.IsEmpty.Should().BeTrue();
        new ButtonEntry { FileName = "test.exe" }.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ButtonEntry_FromCommandでプロパティがコピーされる()
    {
        var cmd = new Command
        {
            Name = "test",
            FileName = "test.exe",
            Param = "-v",
            WorkDir = @"C:\temp",
            Show = WindowStyle.Hidden,
            Priority = ProcessPriorityLevel.Idle,
            RunAsAdmin = true,
        };

        var entry = ButtonEntry.FromCommand(cmd, 2, 3);

        entry.Row.Should().Be(2);
        entry.Col.Should().Be(3);
        entry.Name.Should().Be("test");
        entry.FileName.Should().Be("test.exe");
        entry.Param.Should().Be("-v");
        entry.WorkDir.Should().Be(@"C:\temp");
        entry.Show.Should().Be(WindowStyle.Hidden);
        entry.Priority.Should().Be(ProcessPriorityLevel.Idle);
        entry.RunAsAdmin.Should().BeTrue();
    }

    [Fact]
    public void ButtonTab_GetSetButtonで位置管理できる()
    {
        var tab = new ButtonTab();

        tab.GetButton(0, 0).Should().BeNull();

        var entry = new ButtonEntry { Name = "test", FileName = "test.exe" };
        tab.SetButton(1, 2, entry);

        tab.GetButton(1, 2).Should().NotBeNull();
        tab.GetButton(1, 2)!.Name.Should().Be("test");
        tab.Buttons.Should().HaveCount(1);

        // 上書き
        var entry2 = new ButtonEntry { Name = "test2", FileName = "test2.exe" };
        tab.SetButton(1, 2, entry2);
        tab.GetButton(1, 2)!.Name.Should().Be("test2");
        tab.Buttons.Should().HaveCount(1);

        // 削除
        tab.SetButton(1, 2, null);
        tab.GetButton(1, 2).Should().BeNull();
        tab.Buttons.Should().BeEmpty();
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
