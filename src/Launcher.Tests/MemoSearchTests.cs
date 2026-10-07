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
}
