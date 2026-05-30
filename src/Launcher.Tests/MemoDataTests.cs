using System.Drawing;
using System.Xml.Serialization;
using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// MemoDataのシリアライズ・ロジックテスト
/// </summary>
public sealed class MemoDataTests
{
    [Fact]
    public void MemoData_ラウンドトリップでデフォルト値が保持される()
    {
        var original = new MemoData();
        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<MemoData>(xml);

        deserialized.FontName.Should().Be("Consolas");
        deserialized.FontSize.Should().Be(11f);
        deserialized.CurrentTabIndex.Should().Be(0);
        deserialized.ClosedTabsLimit.Should().Be(MemoData.DefaultClosedTabsLimit);
        deserialized.Tabs.Should().BeEmpty();
        deserialized.ClosedTabs.Should().BeEmpty();
        deserialized.WindowPos.Should().Be(Point.Empty);
        deserialized.WindowSize.Should().Be(Size.Empty);
    }

    [Fact]
    public void MemoData_全プロパティがラウンドトリップで保持される()
    {
        var original = new MemoData
        {
            CurrentTabIndex = 1,
            ClosedTabsLimit = 5,
            WindowPos = new Point(120, 240),
            WindowSize = new Size(640, 480),
            FontName = "Meiryo",
            FontSize = 14f,
        };
        // XMLの要素テキストは改行をLFへ正規化するため、複数行はLFで保持される
        original.Tabs.Add(new MemoTab { Name = "作業", Text = "本文1\n2行目" });
        original.Tabs.Add(new MemoTab { Name = "メモ", Text = "" });
        original.ClosedTabs.Add(new MemoTab { Name = "削除済み", Text = "ゴミ箱の内容" });

        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<MemoData>(xml);

        deserialized.CurrentTabIndex.Should().Be(1);
        deserialized.ClosedTabsLimit.Should().Be(5);
        deserialized.WindowPos.Should().Be(new Point(120, 240));
        deserialized.WindowSize.Should().Be(new Size(640, 480));
        deserialized.FontName.Should().Be("Meiryo");
        deserialized.FontSize.Should().Be(14f);

        deserialized.Tabs.Should().HaveCount(2);
        deserialized.Tabs[0].Name.Should().Be("作業");
        deserialized.Tabs[0].Text.Should().Be("本文1\n2行目");
        deserialized.Tabs[1].Name.Should().Be("メモ");
        deserialized.Tabs[1].Text.Should().BeEmpty();

        deserialized.ClosedTabs.Should().HaveCount(1);
        deserialized.ClosedTabs[0].Name.Should().Be("削除済み");
        deserialized.ClosedTabs[0].Text.Should().Be("ゴミ箱の内容");
    }

    [Fact]
    public void MemoData_要素を欠くXMLから既定値が補完される()
    {
        var deserialized = DeserializeFromString<MemoData>("<MemoData />");

        deserialized.FontName.Should().Be("Consolas");
        deserialized.FontSize.Should().Be(11f);
        deserialized.ClosedTabsLimit.Should().Be(MemoData.DefaultClosedTabsLimit);
        deserialized.Tabs.Should().BeEmpty();
        deserialized.ClosedTabs.Should().BeEmpty();
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
