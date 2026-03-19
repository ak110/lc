using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// CommandList.FindMatchのテスト
/// </summary>
public class CommandListTests
{
    private readonly Config _config = new Config { CommandIgnoreCase = true };

    private static CommandList CreateSampleList()
    {
        var list = new CommandList();
        list.Commands.Add(new Command { Name = "notepad" });
        list.Commands.Add(new Command { Name = "calc" });
        list.Commands.Add(new Command { Name = "htdocs" });
        list.Commands.Add(new Command { Name = "cmd" });
        return list;
    }

    // --- 空入力 ---

    [Fact]
    public void FindMatch_空入力で全コマンドが返る()
    {
        var list = CreateSampleList();

        var result = list.FindMatch("", _config).ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void FindMatch_null入力で全コマンドが返る()
    {
        var list = CreateSampleList();

        var result = list.FindMatch(null!, _config).ToList();

        result.Should().HaveCount(4);
    }

    // --- 前方一致 ---

    [Fact]
    public void FindMatch_前方一致でヒットする()
    {
        var list = CreateSampleList();

        var result = list.FindMatch("note", _config).ToList();

        result.Should().ContainSingle();
        result[0].Name.Should().Be("notepad");
    }

    [Fact]
    public void FindMatch_前方一致で複数ヒット()
    {
        var list = new CommandList();
        list.Commands.Add(new Command { Name = "notepad" });
        list.Commands.Add(new Command { Name = "notepadpp" });

        var result = list.FindMatch("note", _config).ToList();

        // 両方とも "note" で前方一致
        result.Should().HaveCount(2);
        // 短い名前のnotepadがスコアが高いので先に来る
        result[0].Name.Should().Be("notepad");
    }

    // --- 部分一致 ---

    [Fact]
    public void FindMatch_部分一致でヒットする()
    {
        var list = CreateSampleList();

        var result = list.FindMatch("docs", _config).ToList();

        result.Should().ContainSingle();
        result[0].Name.Should().Be("htdocs");
    }

    // --- マッチなし ---

    [Fact]
    public void FindMatch_マッチなしで空結果()
    {
        var list = CreateSampleList();

        var result = list.FindMatch("xyz", _config).ToList();

        result.Should().BeEmpty();
    }

    // --- スコア順 ---

    [Fact]
    public void FindMatch_スコア順にソートされる()
    {
        var list = new CommandList();
        list.Commands.Add(new Command { Name = "abcdef" });
        list.Commands.Add(new Command { Name = "abc" });

        var result = list.FindMatch("abc", _config).ToList();

        result.Should().HaveCount(2);
        // 短い名前（完全一致に近い）のabcが先に来る
        result[0].Name.Should().Be("abc");
        result[1].Name.Should().Be("abcdef");
    }

    // --- 空リスト ---

    [Fact]
    public void FindMatch_空リストで空入力は空結果()
    {
        var list = new CommandList();

        var result = list.FindMatch("", _config).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindMatch_空リストで検索しても空結果()
    {
        var list = new CommandList();

        var result = list.FindMatch("test", _config).ToList();

        result.Should().BeEmpty();
    }
}
