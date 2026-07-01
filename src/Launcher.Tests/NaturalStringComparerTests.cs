using FluentAssertions;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// NaturalStringComparerのテスト
/// </summary>
public sealed class NaturalStringComparerTests
{
    [Fact]
    public void Compare_数字部分を数値として比較する()
    {
        var input = new[] { "file10.txt", "file2.txt", "file1.txt", "file20.txt" };
        Array.Sort(input, NaturalStringComparer.Instance);
        input.Should().Equal("file1.txt", "file2.txt", "file10.txt", "file20.txt");
    }

    [Fact]
    public void Compare_括弧付き番号も数値として比較する()
    {
        var input = new[] { "file (10).txt", "file (2).txt", "file (1).txt" };
        Array.Sort(input, NaturalStringComparer.Instance);
        input.Should().Equal("file (1).txt", "file (2).txt", "file (10).txt");
    }

    [Fact]
    public void Compare_数字を含まない文字列は文字列順で比較する()
    {
        var input = new[] { "charlie", "alpha", "bravo" };
        Array.Sort(input, NaturalStringComparer.Instance);
        input.Should().Equal("alpha", "bravo", "charlie");
    }

    [Fact]
    public void Compare_両方nullは0を返す()
    {
        NaturalStringComparer.Instance.Compare(null, null).Should().Be(0);
    }

    [Fact]
    public void Compare_左辺のみnullは負値を返す()
    {
        NaturalStringComparer.Instance.Compare(null, "x").Should().BeNegative();
    }

    [Fact]
    public void Compare_右辺のみnullは正値を返す()
    {
        NaturalStringComparer.Instance.Compare("x", null).Should().BePositive();
    }

    [Fact]
    public void Compare_同じ文字列は0を返す()
    {
        NaturalStringComparer.Instance.Compare("file1.txt", "file1.txt").Should().Be(0);
    }
}
