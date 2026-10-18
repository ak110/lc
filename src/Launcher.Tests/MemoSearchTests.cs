using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class MemoSearchTests
{
    [Theory]
    [InlineData(false, false, 0, 0)]
    [InlineData(false, true, 0, 8)]
    [InlineData(true, false, 11, 8)]
    [InlineData(true, true, 8, -1)]
    public void 文字列検索は方向と大文字小文字と開始位置を守る(bool reverse, bool matchCase, int start, int expected)
    {
        var result = MemoSearch.FindMatch("Abc def abc", "abc", start, reverse, matchCase, false);
        result.start.Should().Be(expected);
        result.length.Should().Be(expected < 0 ? 0 : 3);
    }

    [Theory]
    [InlineData(false, 0, 1)]
    [InlineData(true, 7, 5)]
    public void 正規表現検索は一致位置と長さを返す(bool reverse, int start, int expected)
    {
        MemoSearch.FindMatch("a12 b34", @"\d+", start, reverse, false, true)
            .Should().Be((expected, 2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[")]
    public void 無効な検索条件は一致しない(string pattern)
    {
        MemoSearch.FindMatch("text", pattern, 0, false, false, true).start.Should().Be(-1);
    }

    [Fact]
    public void 選択文字列全体が一致するときだけ置換対象にする()
    {
        MemoSearch.IsMatch("Abc", "abc", false, false).Should().BeTrue();
        MemoSearch.IsMatch("Abc", "abc", true, false).Should().BeFalse();
        MemoSearch.IsMatch("123", @"\d+", true, true).Should().BeTrue();
        MemoSearch.IsMatch("x123", @"\d+", true, true).Should().BeFalse();
        MemoSearch.IsMatch("123", "[", true, true).Should().BeFalse();
    }

    [Fact]
    public void 正規表現置換はキャプチャを展開し文字列置換はそのまま使う()
    {
        MemoSearch.ComputeReplacement("a12", @"a(\d+)", "$1!", true, true).Should().Be("12!");
        MemoSearch.ComputeReplacement("a12", "a12", "$1!", true, false).Should().Be("$1!");
    }

    [Theory]
    [InlineData(false, "abc ABC", "abc", "x", "x x", 2)]
    [InlineData(true, "a12 a34", @"a(\d+)", "$1", "12 34", 2)]
    [InlineData(false, "abc", "none", "x", "abc", 0)]
    public void 全置換は文字列と置換件数を返す(bool regex, string text, string pattern, string replacement, string expected, int expectedCount)
    {
        MemoSearch.ReplaceAllInText(text, pattern, replacement, false, regex, out int count).Should().Be(expected);
        count.Should().Be(expectedCount);
    }

    [Theory]
    [InlineData(@"a(\d)", @"$1\n", "a1 a2", "1\n 2\n")]
    [InlineData("a", @"\t\r\a\f\v\e\0", "a", "\t\r\a\f\v\u001B\0")]
    [InlineData("a", @"\x41\u3042", "a", "Aあ")]
    [InlineData("a", @"\\n", "a", @"\n")]
    [InlineData("a", @"\x24\u00241", "a", "$$1")]
    [InlineData("a", @"\q\x4\u30", "a", @"\q\x4\u30")]
    [InlineData("a", "x\\", "a", "x\\")]
    public void 正規表現の置換後文字列は文字エスケープを解釈し後方参照も働く(string pattern, string replacement, string text, string expected)
    {
        MemoSearch.ReplaceAllInText(text, pattern, replacement, true, true, out _).Should().Be(expected);
    }

    [Fact]
    public void 正規表現の単一置換も置換後文字列のエスケープを解釈し文字列置換は文字どおりに使う()
    {
        MemoSearch.ComputeReplacement("a1", @"a(\d)", @"[$1]\t\\", true, true).Should().Be("[1]\t\\");
        MemoSearch.ComputeReplacement("a1", "a1", @"\t", true, false).Should().Be(@"\t");
        MemoSearch.ReplaceAllInText("a1", "a1", @"\n", true, false, out _).Should().Be(@"\n");
    }
}
