using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class CommandListTests
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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FindMatch_空入力とnull入力で全コマンドが返る(string? input)
    {
        var list = CreateSampleList();
        var expected = list.Commands.ToList();

        var result = list.FindMatch(input!, _config).ToList();

        result.Should().BeEquivalentTo(expected);
        list.Commands.Should().Equal(expected);
    }

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

        result.Select(c => c.Name).Should().Equal("notepad", "notepadpp");
    }

    [Fact]
    public void FindMatch_部分一致でヒットする()
    {
        var list = CreateSampleList();

        var result = list.FindMatch("docs", _config).ToList();

        result.Should().ContainSingle();
        result[0].Name.Should().Be("htdocs");
    }

    [Fact]
    public void FindMatch_マッチなしで空結果()
    {
        var list = CreateSampleList();

        var result = list.FindMatch("xyz", _config).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindMatch_スコア順にソートされる()
    {
        var list = new CommandList();
        list.Commands.Add(new Command { Name = "abcdef" });
        list.Commands.Add(new Command { Name = "abc" });

        var result = list.FindMatch("abc", _config).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("abc");
        result[1].Name.Should().Be("abcdef");
    }

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
