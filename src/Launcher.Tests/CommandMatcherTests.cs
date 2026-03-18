using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public class CommandMatcherTests
{
    private readonly Config _config = new Config { CommandIgnoreCase = true };
    private readonly Config _caseSensitiveConfig = new Config { CommandIgnoreCase = false };

    // --- GetMatchScore: 先頭一致 ---

    [Fact]
    public void GetMatchScore_完全一致は最高スコア()
    {
        int score = CommandMatcher.GetMatchScore("notepad", "notepad", _config);
        score.Should().BePositive();
    }

    [Fact]
    public void GetMatchScore_先頭一致はスコアが正()
    {
        int score = CommandMatcher.GetMatchScore("notepad", "note", _config);
        score.Should().BePositive();
    }

    [Fact]
    public void GetMatchScore_完全一致は先頭一致より高スコア()
    {
        int full = CommandMatcher.GetMatchScore("notepad", "notepad", _config);
        int partial = CommandMatcher.GetMatchScore("notepad", "note", _config);
        full.Should().BeGreaterThan(partial);
    }

    [Fact]
    public void GetMatchScore_不一致は0()
    {
        int score = CommandMatcher.GetMatchScore("notepad", "xyz", _config);
        score.Should().Be(0);
    }

    // --- GetMatchScore: 部分一致 ---

    [Fact]
    public void GetMatchScore_部分一致はスコアが正()
    {
        // "htdocs" に "docs" は部分一致
        int score = CommandMatcher.GetMatchScore("htdocs", "docs", _config);
        score.Should().BePositive();
    }

    [Fact]
    public void GetMatchScore_先頭一致は部分一致より高スコア()
    {
        int first = CommandMatcher.GetMatchScore("docs", "doc", _config);
        int mid = CommandMatcher.GetMatchScore("htdocs", "docs", _config);
        first.Should().BeGreaterThan(mid);
    }

    // --- GetMatchScore: 大文字小文字 ---

    [Fact]
    public void GetMatchScore_大文字小文字無視で一致()
    {
        int score = CommandMatcher.GetMatchScore("Notepad", "notepad", _config);
        score.Should().BePositive();
    }

    [Fact]
    public void GetMatchScore_大文字小文字区別で不一致()
    {
        int score = CommandMatcher.GetMatchScore("Notepad", "notepad", _caseSensitiveConfig);
        score.Should().Be(0);
    }

    [Fact]
    public void GetMatchScore_大文字小文字区別で一致()
    {
        int score = CommandMatcher.GetMatchScore("notepad", "notepad", _caseSensitiveConfig);
        score.Should().BePositive();
    }

    // --- GetMatchScore: 短いコマンド名が優先される ---

    [Fact]
    public void GetMatchScore_短い名前が長い名前より高スコア()
    {
        int shorter = CommandMatcher.GetMatchScore("cmd", "cmd", _config);
        int longer = CommandMatcher.GetMatchScore("command", "cmd", _config);
        shorter.Should().BeGreaterThan(longer);
    }

    // --- GetMatchScore: コマンド名より長い入力 ---

    [Fact]
    public void GetMatchScore_コマンド名より長い入力に引数あり()
    {
        // "notepad test.txt" → "notepad" と一致 + 引数 "test.txt"
        int score = CommandMatcher.GetMatchScore("notepad", "notepad test.txt", _config);
        score.Should().BePositive();
    }

    [Fact]
    public void GetMatchScore_コマンド名より長い入力でスペースなしは不一致()
    {
        // "notepad_x" → "notepad" に完全一致しない (余分文字がスペースでない)
        int score = CommandMatcher.GetMatchScore("notepad", "notepad_x", _config);
        score.Should().Be(0);
    }

    // --- ParseInput ---

    [Fact]
    public void ParseInput_完全一致でコマンド名と引数を分離()
    {
        bool result = CommandMatcher.ParseInput("notepad", "notepad test.txt", _config,
            out string commandName, out string? arguments);
        result.Should().BeTrue();
        commandName.Should().Be("notepad");
        arguments.Should().Be("test.txt");
    }

    [Fact]
    public void ParseInput_不一致時はfalse()
    {
        bool result = CommandMatcher.ParseInput("notepad", "note", _config,
            out string commandName, out string? arguments);
        result.Should().BeFalse();
        commandName.Should().Be("note");
        arguments.Should().BeNull();
    }

    [Fact]
    public void ParseInput_引数なしの完全一致()
    {
        bool result = CommandMatcher.ParseInput("notepad", "notepad", _config,
            out string commandName, out string? arguments);
        result.Should().BeTrue();
        commandName.Should().Be("notepad");
        arguments.Should().BeEmpty();
    }

    // --- GetMatchLength ---

    [Fact]
    public void GetMatchLength_完全一致は全長を返す()
    {
        int len = CommandMatcher.GetMatchLength("abc", "abc", _config);
        len.Should().Be(3);
    }

    [Fact]
    public void GetMatchLength_先頭一致は一致部分の長さ()
    {
        int len = CommandMatcher.GetMatchLength("abcdef", "abc", _config);
        len.Should().Be(3);
    }

    [Fact]
    public void GetMatchLength_不一致は0()
    {
        int len = CommandMatcher.GetMatchLength("abc", "xyz", _config);
        len.Should().Be(0);
    }

    // --- ParseInputNotMatch ---

    [Fact]
    public void ParseInputNotMatch_スペースで分離()
    {
        CommandMatcher.ParseInputNotMatch("cmd args here", out string commandName, out string? arguments);
        commandName.Should().Be("cmd");
        arguments.Should().Be("args here");
    }

    [Fact]
    public void ParseInputNotMatch_スペースなし()
    {
        CommandMatcher.ParseInputNotMatch("cmd", out string commandName, out string? arguments);
        commandName.Should().Be("cmd");
        arguments.Should().BeNull();
    }

    // --- 部分一致の回帰テスト（旧Command静的コンストラクタの検証を置換） ---

    [Fact]
    public void GetMatchScore_htdocsにdocsで部分一致する()
    {
        int score = CommandMatcher.GetMatchScore("htdocs", "docs", _config);
        score.Should().BePositive("'htdocs'に'docs'は部分一致するべき");
    }
}
