using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// MemoPresenterのロジックテスト
/// </summary>
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
    [InlineData(0, 2, 1)] // 現在タブより前を閉じると現在は1つ前へずれる
    [InlineData(2, 2, 1)] // 末尾の現在タブを閉じると新末尾を選ぶ
    [InlineData(1, 0, 0)] // 現在タブより後を閉じると現在は変わらない
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
    [InlineData(9, 9)]   // 上限未満は全保持
    [InlineData(10, 10)] // 上限ちょうどは全保持
    [InlineData(11, 10)] // 上限超過は最古が押し出される
    public void CloseTab_ゴミ箱の上限を超えると最古が押し出される(int closeCount, int expectedTrashCount)
    {
        var data = new MemoData(); // ClosedTabsLimit = 10 (既定)
        // closeCount 個閉じられるよう closeCount + 1 個のタブを用意する
        for (int i = 0; i <= closeCount; i++)
        {
            data.Tabs.Add(new MemoTab { Name = $"T{i}", Text = $"本文{i}" });
        }

        // 常に先頭タブを閉じる (T0, T1, ... の順で閉じる)
        for (int i = 0; i < closeCount; i++)
        {
            MemoPresenter.CloseTab(data, 0).Success.Should().BeTrue();
        }

        data.ClosedTabs.Should().HaveCount(expectedTrashCount);
        // 先頭は最後に閉じたタブ (最新)
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
    [InlineData(-1, 0)] // 範囲下限を下回ると0へ補正
    [InlineData(5, 2)]  // 範囲上限を上回ると末尾へ補正
    [InlineData(1, 1)]  // 範囲内はそのまま
    public void ClampCurrentIndex_有効範囲へ補正する(int currentIndex, int expected)
    {
        var data = new MemoData { CurrentTabIndex = currentIndex };
        data.Tabs.Add(new MemoTab { Name = "T0" });
        data.Tabs.Add(new MemoTab { Name = "T1" });
        data.Tabs.Add(new MemoTab { Name = "T2" });

        MemoPresenter.ClampCurrentIndex(data).Should().Be(expected);
    }
}
