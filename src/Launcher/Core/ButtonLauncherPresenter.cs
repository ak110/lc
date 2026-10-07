namespace Launcher.Core;

/// <summary>
/// ButtonLauncherFormのビジネスロジックを担当するPresenter。
/// ウィンドウサイズ計算、D&amp;D状態管理、ボタンスワップロジックを集約する。
/// </summary>
public static class ButtonLauncherPresenter
{
    /// <summary>
    /// 列数・行数からウィンドウのクライアントサイズを計算する。
    /// </summary>
    public static Size CalculateWindowSize(
        int columns, int rows,
        int buttonWidth, int buttonHeight,
        int toolStripHeight, int tabHeaderHeight)
    {
        int width = columns * buttonWidth;
        int height = rows * buttonHeight + toolStripHeight + tabHeaderHeight;
        return new Size(width, height);
    }

    /// <summary>
    /// デフォルトタブを設定する。
    /// </summary>
    public static void SetDefaultTab(ButtonLauncherData data, int tabIndex)
    {
        data.DefaultTabIndex = tabIndex;
    }

    /// <summary>
    /// タブ削除の結果
    /// </summary>
    /// <param name="Success">削除が成功したか</param>
    /// <param name="NewSelectedIndex">削除後に選択すべきタブインデックス (失敗時は-1)</param>
    public record DeleteTabResult(bool Success, int NewSelectedIndex);

    /// <summary>
    /// タブを削除する。最後の1タブは削除不可。
    /// DefaultTabIndexの調整も行う。
    /// </summary>
    /// <returns>削除結果</returns>
    public static DeleteTabResult DeleteTab(ButtonLauncherData data, int tabIndex)
    {
        if (data.Tabs.Count <= 1)
        {
            return new DeleteTabResult(false, -1);
        }

        data.Tabs.RemoveAt(tabIndex);

        // DefaultTabIndexの調整
        if (data.DefaultTabIndex == tabIndex)
            data.DefaultTabIndex = 0;
        else if (data.DefaultTabIndex > tabIndex)
            data.DefaultTabIndex--;

        // 削除後の選択タブ: 削除位置が末尾だった場合は1つ前、それ以外は同じ位置
        int newIndex = Math.Min(tabIndex, data.Tabs.Count - 1);
        return new DeleteTabResult(true, newIndex);
    }

    /// <summary>
    /// タブを移動する (隣接スワップ)。DefaultTabIndexも追従する。
    /// </summary>
    public static void MoveTab(ButtonLauncherData data, int fromIndex, int toIndex)
    {
        var tab = data.Tabs[fromIndex];
        data.Tabs.RemoveAt(fromIndex);
        data.Tabs.Insert(toIndex, tab);

        // DefaultTabIndexの調整 (隣接スワップ)
        if (data.DefaultTabIndex == fromIndex)
            data.DefaultTabIndex = toIndex;
        else if (data.DefaultTabIndex == toIndex)
            data.DefaultTabIndex = fromIndex;
    }

    /// <summary>
    /// D&amp;Dでボタンをスワップする。
    /// ドラッグ元にはドロップ先の既存エントリを配置し、ドロップ先にはドラッグ中のエントリを配置する。
    /// ドロップ先が空の場合、ドラッグ元は空になる。
    /// </summary>
    /// <param name="sourceTab">ドラッグ元のタブ</param>
    /// <param name="srcRow">ドラッグ元の行</param>
    /// <param name="srcCol">ドラッグ元の列</param>
    /// <param name="destTab">ドロップ先のタブ</param>
    /// <param name="destRow">ドロップ先の行</param>
    /// <param name="destCol">ドロップ先の列</param>
    /// <param name="dragEntry">ドラッグ中のエントリ</param>
    public static void SwapButtons(
        ButtonTab sourceTab, int srcRow, int srcCol,
        ButtonTab destTab, int destRow, int destCol,
        ButtonEntry dragEntry)
    {
        // 移動先の既存エントリ
        var destEntry = destTab.GetButton(destRow, destCol);

        // ドロップ先にドラッグ中のエントリを配置
        destTab.SetButton(destRow, destCol, ButtonEntry.FromCommand(dragEntry, destRow, destCol));

        // ドラッグ元にドロップ先の既存エントリを配置 (空なら削除)
        if (destEntry is not null && !destEntry.IsEmpty)
        {
            sourceTab.SetButton(srcRow, srcCol, ButtonEntry.FromCommand(destEntry, srcRow, srcCol));
        }
        else
        {
            sourceTab.SetButton(srcRow, srcCol, null);
        }
    }
}
