namespace Launcher.Core;

/// <summary>
/// MemoFormのビジネスロジックを担当するPresenter。
/// タブを閉じる際の現在タブ補正、閉じたタブのゴミ箱管理、復元を集約する。
/// </summary>
public static class MemoPresenter
{
    /// <summary>
    /// タブを閉じた結果
    /// </summary>
    /// <param name="Success">閉じる操作が成功したか</param>
    /// <param name="NewCurrentIndex">閉じた後に選択すべきタブインデックス (失敗時は-1)</param>
    public record CloseTabResult(bool Success, int NewCurrentIndex);

    /// <summary>
    /// タブを閉じてゴミ箱へ送る。最後の1タブは残すため閉じない。
    /// CurrentTabIndexの補正も行う。
    /// </summary>
    /// <returns>閉じた結果</returns>
    public static CloseTabResult CloseTab(MemoData data, int tabIndex)
    {
        if (data.Tabs.Count <= 1)
        {
            return new CloseTabResult(false, -1);
        }

        var tab = data.Tabs[tabIndex];
        data.Tabs.RemoveAt(tabIndex);
        PushClosedTab(data, tab);

        // 削除位置と同じか後ろのタブを選択していた場合は1つ前へずれる
        int newIndex = data.CurrentTabIndex;
        if (newIndex >= tabIndex)
        {
            newIndex--;
        }
        newIndex = Math.Min(Math.Max(newIndex, 0), data.Tabs.Count - 1);
        data.CurrentTabIndex = newIndex;
        return new CloseTabResult(true, newIndex);
    }

    /// <summary>
    /// 閉じたタブをゴミ箱の先頭へ積み、上限を超えた古いタブを捨てる。
    /// </summary>
    static void PushClosedTab(MemoData data, MemoTab tab)
    {
        data.ClosedTabs.Insert(0, tab);
        int limit = Math.Max(0, data.ClosedTabsLimit);
        while (data.ClosedTabs.Count > limit)
        {
            data.ClosedTabs.RemoveAt(data.ClosedTabs.Count - 1);
        }
    }

    /// <summary>
    /// ゴミ箱の指定タブを末尾へ復元し、現在タブにする。
    /// </summary>
    /// <returns>復元したタブの新しいインデックス</returns>
    public static int RestoreTab(MemoData data, int closedIndex)
    {
        var tab = data.ClosedTabs[closedIndex];
        data.ClosedTabs.RemoveAt(closedIndex);
        data.Tabs.Add(tab);
        data.CurrentTabIndex = data.Tabs.Count - 1;
        return data.CurrentTabIndex;
    }

    /// <summary>
    /// CurrentTabIndexをタブ数に応じた有効範囲へ補正して返す。
    /// </summary>
    public static int ClampCurrentIndex(MemoData data)
    {
        if (data.Tabs.Count == 0)
        {
            return 0;
        }
        return Math.Min(Math.Max(data.CurrentTabIndex, 0), data.Tabs.Count - 1);
    }
}
