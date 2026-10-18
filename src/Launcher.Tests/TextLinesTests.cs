using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class TextLinesTests
{
    [Theory]
    [InlineData("ab\ncd\nef", 1, 3, 0, 5)]
    [InlineData("ab\ncd\nef", 0, 6, 0, 6)]
    [InlineData("ab\ncd\nef", 4, 0, 3, 2)]
    [InlineData("ab\ncd", 3, 3, 3, 3)]
    public void 行範囲は行頭から行末まで広げ次行の行頭で終わる選択はその行を含めない(
        string text, int selStart, int selLength, int expectedStart, int expectedLength)
    {
        TextLines.ExpandToLines(text, selStart, selLength).Should().Be((expectedStart, expectedLength));
    }

    [Fact]
    public void 行のソートは選択を含む行を序数順に並べ替え範囲外の行を保つ()
    {
        string text = "head\nc\nB\na\ntail";
        string replaced = TextLines.ComputeSortReplacement(text, 6, 4, out int start, out int length);
        (start, length).Should().Be((5, 5));
        replaced.Should().Be("B\na\nc");
        (text[..start] + replaced + text[(start + length)..]).Should().Be("head\nB\na\nc\ntail");
    }

    [Fact]
    public void 行のソートは次行の行頭で終わる選択の改行を末尾に保つ()
    {
        string replaced = TextLines.ComputeSortReplacement("b\na\nb\nz", 0, 6, out int start, out int length);
        (start, length).Should().Be((0, 6));
        replaced.Should().Be("a\nb\nb\n");
    }

    [Fact]
    public void 行のソートは改行の位置と種類を保ち行の内容だけを並べ替える()
    {
        string replaced = TextLines.ComputeSortReplacement("b\r\na\nc", 0, 6, out _, out _);
        replaced.Should().Be("a\r\nb\nc");
    }
}
