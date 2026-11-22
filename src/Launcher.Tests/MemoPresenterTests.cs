using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class MemoPresenterTests
{
    [Fact]
    public void CloseTab_最後の1タブは閉じられない()
    {
        var data = new MemoData();
        data.Tabs.Add(new MemoTab { Name = "唯一", Text = "本文" });

        var result = MemoPresenter.CloseTab(data, 0);

        result.Success.Should().BeFalse();
        result.NewCurrentIndex.Should().Be(-1);
        data.Tabs.Should().HaveCount(1);
        data.ClosedTabs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 2, 1)]
    [InlineData(2, 2, 1)]
    [InlineData(1, 0, 0)]
    public void CloseTab_現在タブインデックスが補正される(int closeIndex, int currentIndex, int expected)
    {
        var data = new MemoData { CurrentTabIndex = currentIndex };
        data.Tabs.Add(new MemoTab { Name = "T0" });
        data.Tabs.Add(new MemoTab { Name = "T1" });
        data.Tabs.Add(new MemoTab { Name = "T2" });

        var result = MemoPresenter.CloseTab(data, closeIndex);

        result.Success.Should().BeTrue();
        result.NewCurrentIndex.Should().Be(expected);
        data.CurrentTabIndex.Should().Be(expected);
        data.Tabs.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(9, 9)]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    public void CloseTab_ゴミ箱の上限を超えると最古が削除される(int closeCount, int expectedTrashCount)
    {
        var data = new MemoData();
        for (int i = 0; i <= closeCount; i++)
        {
            data.Tabs.Add(new MemoTab { Name = $"T{i}", Text = $"本文{i}" });
        }

        for (int i = 0; i < closeCount; i++)
        {
            MemoPresenter.CloseTab(data, 0).Success.Should().BeTrue();
        }

        data.ClosedTabs.Should().HaveCount(expectedTrashCount);
        data.ClosedTabs[0].Name.Should().Be($"T{closeCount - 1}");
    }

    [Fact]
    public void RestoreTab_ゴミ箱から末尾へ復元し現在タブにする()
    {
        var data = new MemoData();
        data.Tabs.Add(new MemoTab { Name = "残り" });
        data.Tabs.Add(new MemoTab { Name = "閉じる対象", Text = "復元したい内容" });

        MemoPresenter.CloseTab(data, 1);
        data.Tabs.Should().HaveCount(1);
        data.ClosedTabs.Should().HaveCount(1);

        int newIndex = MemoPresenter.RestoreTab(data, 0);

        newIndex.Should().Be(1);
        data.CurrentTabIndex.Should().Be(1);
        data.Tabs.Should().HaveCount(2);
        data.Tabs[1].Name.Should().Be("閉じる対象");
        data.Tabs[1].Text.Should().Be("復元したい内容");
        data.ClosedTabs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(5, 2)]
    [InlineData(1, 1)]
    public void ClampCurrentIndex_有効範囲へ補正する(int currentIndex, int expected)
    {
        var data = new MemoData { CurrentTabIndex = currentIndex };
        data.Tabs.Add(new MemoTab { Name = "T0" });
        data.Tabs.Add(new MemoTab { Name = "T1" });
        data.Tabs.Add(new MemoTab { Name = "T2" });

        MemoPresenter.ClampCurrentIndex(data).Should().Be(expected);
    }
}
