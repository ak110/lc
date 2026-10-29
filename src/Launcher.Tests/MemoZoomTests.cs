using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class MemoZoomTests
{
    [Theory]
    [InlineData(1f, true, 1.1f)]
    [InlineData(1f, false, 0.9f)]
    [InlineData(1.26f, true, 1.3f)]
    [InlineData(1.26f, false, 1.2f)]
    [InlineData(1.1f, true, 1.2f)]
    [InlineData(0.2f, false, 0.1f)]
    [InlineData(4.9f, true, 5f)]
    [InlineData(6f, false, 5f)]
    [InlineData(0.05f, true, 0.1f)]
    public void 倍率は操作の方向にある次の10パーセント単位へ進め範囲内に収める(float current, bool zoomIn, float expected)
    {
        MemoZoom.Step(current, zoomIn).Should().BeApproximately(expected, 0.0001f);
    }

    [Theory]
    [InlineData(0.1f, false)]
    [InlineData(5f, true)]
    [InlineData(6f, true)]
    [InlineData(0.05f, false)]
    public void 範囲の端と範囲外から遠ざかる操作は倍率を変えない(float current, bool zoomIn)
    {
        MemoZoom.Step(current, zoomIn).Should().BeNull();
    }
}
