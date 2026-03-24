using System.Drawing;
using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class ButtonLauncherPresenterTests
{
    #region CalculateWindowSize

    [Fact]
    public void CalculateWindowSize_基本的な計算()
    {
        // 7列7行、ボタン64x64、ツールバー25px、タブヘッダー21px
        var result = ButtonLauncherPresenter.CalculateWindowSize(7, 7, 64, 64, 25, 21);

        result.Width.Should().Be(7 * 64);   // 448
        result.Height.Should().Be(7 * 64 + 25 + 21); // 494
    }

    [Fact]
    public void CalculateWindowSize_1x1の最小構成()
    {
        var result = ButtonLauncherPresenter.CalculateWindowSize(1, 1, 64, 64, 25, 21);

        result.Width.Should().Be(64);
        result.Height.Should().Be(64 + 25 + 21); // 110
    }

    #endregion

    #region CalculateGridSize

    [Fact]
    public void CalculateGridSize_基本的な計算()
    {
        // 640x480のクライアント領域、ツールバー25px、タブヘッダー21px、ボタン64x64
        var result = ButtonLauncherPresenter.CalculateGridSize(640, 480, 25, 21, 64, 64);

        result.Columns.Should().Be(10); // 640 / 64
        result.Rows.Should().Be(6);     // (480 - 25 - 21) / 64 = 434 / 64 = 6
    }

    [Fact]
    public void CalculateGridSize_最小値は1()
    {
        var result = ButtonLauncherPresenter.CalculateGridSize(30, 50, 25, 21, 64, 64);

        result.Columns.Should().Be(1);
        result.Rows.Should().Be(1);
    }

    [Fact]
    public void CalculateGridSize_端数切り捨て()
    {
        // 130 / 64 = 2.03 → 2
        var result = ButtonLauncherPresenter.CalculateGridSize(130, 200, 10, 10, 64, 64);

        result.Columns.Should().Be(2);
        result.Rows.Should().Be(2); // (200 - 10 - 10) / 64 = 180 / 64 = 2
    }

    [Fact]
    public void CalculateGridSize_高さからツールバーとタブを差し引く()
    {
        // ツールバー40px、タブ30px → 有効高さ = 500 - 40 - 30 = 430
        var result = ButtonLauncherPresenter.CalculateGridSize(640, 500, 40, 30, 64, 64);

        result.Rows.Should().Be(6); // 430 / 64 = 6
    }

    #endregion

    #region DragDropState

    [Fact]
    public void DragDropState_初期状態はInactive()
    {
        var state = new DragDropState();

        state.IsActive.Should().BeFalse();
        state.DragEntry.Should().BeNull();
        state.SourceTab.Should().BeNull();
    }

    [Fact]
    public void DragDropState_Start後はActive()
    {
        var state = new DragDropState();
        var entry = new ButtonEntry { Name = "test", FileName = "test.exe", Row = 0, Col = 0 };
        var tab = new ButtonTab { Name = "Tab1" };

        state.Start(entry, tab, new Point(100, 200));

        state.IsActive.Should().BeTrue();
        state.DragEntry.Should().Be(entry);
        state.SourceTab.Should().Be(tab);
        state.DragStartPoint.Should().Be(new Point(100, 200));
    }

    [Fact]
    public void DragDropState_Reset後はInactive()
    {
        var state = new DragDropState();
        var entry = new ButtonEntry { Name = "test", FileName = "test.exe", Row = 0, Col = 0 };
        var tab = new ButtonTab { Name = "Tab1" };
        state.Start(entry, tab, new Point(100, 200));

        state.Reset();

        state.IsActive.Should().BeFalse();
        state.DragEntry.Should().BeNull();
        state.SourceTab.Should().BeNull();
    }

    [Fact]
    public void DragDropState_ShouldBeginDrag_閾値未満はfalse()
    {
        var state = new DragDropState();
        var entry = new ButtonEntry { Name = "test", FileName = "test.exe", Row = 0, Col = 0 };
        state.Start(entry, new ButtonTab { Name = "Tab1" }, new Point(100, 100));

        // ドラッグサイズ4x4の場合、2px以内の移動は閾値未満
        state.ShouldBeginDrag(new Point(101, 101), new Size(4, 4)).Should().BeFalse();
    }

    [Fact]
    public void DragDropState_ShouldBeginDrag_閾値超過はtrue()
    {
        var state = new DragDropState();
        var entry = new ButtonEntry { Name = "test", FileName = "test.exe", Row = 0, Col = 0 };
        state.Start(entry, new ButtonTab { Name = "Tab1" }, new Point(100, 100));

        // 10px移動はドラッグサイズ4x4の閾値を超える
        state.ShouldBeginDrag(new Point(110, 100), new Size(4, 4)).Should().BeTrue();
    }

    #endregion

    #region SwapButtons

    [Fact]
    public void SwapButtons_空セルへ移動()
    {
        var srcTab = new ButtonTab { Name = "Src" };
        var destTab = new ButtonTab { Name = "Dest" };
        var entry = new ButtonEntry { Name = "app", FileName = "app.exe", Row = 0, Col = 0 };
        srcTab.SetButton(0, 0, entry);

        ButtonLauncherPresenter.SwapButtons(srcTab, 0, 0, destTab, 1, 2, entry);

        // ドロップ先にエントリが配置される
        var dest = destTab.GetButton(1, 2);
        dest.Should().NotBeNull();
        dest!.Name.Should().Be("app");
        dest.Row.Should().Be(1);
        dest.Col.Should().Be(2);

        // ドラッグ元は空になる
        srcTab.GetButton(0, 0).Should().BeNull();
    }

    [Fact]
    public void SwapButtons_既存ボタンとスワップ()
    {
        var tab = new ButtonTab { Name = "Tab1" };
        var entryA = new ButtonEntry { Name = "A", FileName = "a.exe", Row = 0, Col = 0 };
        var entryB = new ButtonEntry { Name = "B", FileName = "b.exe", Row = 1, Col = 1 };
        tab.SetButton(0, 0, entryA);
        tab.SetButton(1, 1, entryB);

        ButtonLauncherPresenter.SwapButtons(tab, 0, 0, tab, 1, 1, entryA);

        // Aがドロップ先に
        var dest = tab.GetButton(1, 1);
        dest.Should().NotBeNull();
        dest!.Name.Should().Be("A");

        // Bがドラッグ元に
        var src = tab.GetButton(0, 0);
        src.Should().NotBeNull();
        src!.Name.Should().Be("B");
    }

    [Fact]
    public void SwapButtons_クロスタブスワップ()
    {
        var tab1 = new ButtonTab { Name = "Tab1" };
        var tab2 = new ButtonTab { Name = "Tab2" };
        var entryA = new ButtonEntry { Name = "A", FileName = "a.exe", Row = 0, Col = 0 };
        var entryB = new ButtonEntry { Name = "B", FileName = "b.exe", Row = 2, Col = 3 };
        tab1.SetButton(0, 0, entryA);
        tab2.SetButton(2, 3, entryB);

        ButtonLauncherPresenter.SwapButtons(tab1, 0, 0, tab2, 2, 3, entryA);

        // Tab2(2,3)にAが配置される
        var dest = tab2.GetButton(2, 3);
        dest.Should().NotBeNull();
        dest!.Name.Should().Be("A");
        dest.Row.Should().Be(2);
        dest.Col.Should().Be(3);

        // Tab1(0,0)にBが配置される
        var src = tab1.GetButton(0, 0);
        src.Should().NotBeNull();
        src!.Name.Should().Be("B");
        src.Row.Should().Be(0);
        src.Col.Should().Be(0);
    }

    #endregion

    #region SetDefaultTab

    [Fact]
    public void SetDefaultTab_指定インデックスが設定される()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });

        ButtonLauncherPresenter.SetDefaultTab(data, 2);

        data.DefaultTabIndex.Should().Be(2);
    }

    [Fact]
    public void SetDefaultTab_0に変更できる()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.DefaultTabIndex = 1;

        ButtonLauncherPresenter.SetDefaultTab(data, 0);

        data.DefaultTabIndex.Should().Be(0);
    }

    #endregion

    #region DeleteTab

    [Fact]
    public void DeleteTab_最後の1タブは削除不可()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });

        var result = ButtonLauncherPresenter.DeleteTab(data, 0);

        result.Success.Should().BeFalse();
        result.NewSelectedIndex.Should().Be(-1);
        data.Tabs.Should().HaveCount(1);
    }

    [Fact]
    public void DeleteTab_中間タブを削除()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });

        var result = ButtonLauncherPresenter.DeleteTab(data, 1);

        result.Success.Should().BeTrue();
        result.NewSelectedIndex.Should().Be(1);
        data.Tabs.Should().HaveCount(2);
        data.Tabs[0].Name.Should().Be("Tab1");
        data.Tabs[1].Name.Should().Be("Tab3");
    }

    [Fact]
    public void DeleteTab_末尾タブを削除すると選択インデックスが1つ前になる()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });

        var result = ButtonLauncherPresenter.DeleteTab(data, 2);

        result.Success.Should().BeTrue();
        result.NewSelectedIndex.Should().Be(1);
        data.Tabs.Should().HaveCount(2);
    }

    [Fact]
    public void DeleteTab_デフォルトタブを削除するとDefaultTabIndexが0にリセットされる()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });
        data.DefaultTabIndex = 1;

        ButtonLauncherPresenter.DeleteTab(data, 1);

        data.DefaultTabIndex.Should().Be(0);
    }

    [Fact]
    public void DeleteTab_デフォルトタブより前のタブを削除するとDefaultTabIndexがデクリメントされる()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });
        data.DefaultTabIndex = 2;

        ButtonLauncherPresenter.DeleteTab(data, 0);

        data.DefaultTabIndex.Should().Be(1);
    }

    [Fact]
    public void DeleteTab_デフォルトタブより後のタブを削除してもDefaultTabIndexは変わらない()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });
        data.DefaultTabIndex = 0;

        ButtonLauncherPresenter.DeleteTab(data, 2);

        data.DefaultTabIndex.Should().Be(0);
    }

    #endregion

    #region MoveTab

    [Fact]
    public void MoveTab_前方に移動()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });

        ButtonLauncherPresenter.MoveTab(data, 2, 1);

        data.Tabs[0].Name.Should().Be("Tab1");
        data.Tabs[1].Name.Should().Be("Tab3");
        data.Tabs[2].Name.Should().Be("Tab2");
    }

    [Fact]
    public void MoveTab_後方に移動()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });

        ButtonLauncherPresenter.MoveTab(data, 0, 1);

        data.Tabs[0].Name.Should().Be("Tab2");
        data.Tabs[1].Name.Should().Be("Tab1");
        data.Tabs[2].Name.Should().Be("Tab3");
    }

    [Fact]
    public void MoveTab_移動元がデフォルトタブの場合DefaultTabIndexが追従する()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });
        data.DefaultTabIndex = 0;

        ButtonLauncherPresenter.MoveTab(data, 0, 1);

        data.DefaultTabIndex.Should().Be(1);
    }

    [Fact]
    public void MoveTab_移動先がデフォルトタブの場合DefaultTabIndexがスワップされる()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });
        data.DefaultTabIndex = 1;

        ButtonLauncherPresenter.MoveTab(data, 0, 1);

        data.DefaultTabIndex.Should().Be(0);
    }

    [Fact]
    public void MoveTab_無関係のデフォルトタブは変わらない()
    {
        var data = new ButtonLauncherData();
        data.Tabs.Add(new ButtonTab { Name = "Tab1" });
        data.Tabs.Add(new ButtonTab { Name = "Tab2" });
        data.Tabs.Add(new ButtonTab { Name = "Tab3" });
        data.DefaultTabIndex = 2;

        ButtonLauncherPresenter.MoveTab(data, 0, 1);

        data.DefaultTabIndex.Should().Be(2);
    }

    #endregion
}
