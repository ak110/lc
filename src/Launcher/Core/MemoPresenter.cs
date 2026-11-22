namespace Launcher.Core;

public static class MemoPresenter
{
    public record CloseTabResult(bool Success, int NewCurrentIndex);

    public static CloseTabResult CloseTab(MemoData data, int tabIndex)
    {
        if (data.Tabs.Count <= 1)
        {
            return new CloseTabResult(false, -1);
        }

        var tab = data.Tabs[tabIndex];
        data.Tabs.RemoveAt(tabIndex);
        PushClosedTab(data, tab);

        int newIndex = data.CurrentTabIndex;
        if (newIndex >= tabIndex)
        {
            newIndex--;
        }
        newIndex = Math.Min(Math.Max(newIndex, 0), data.Tabs.Count - 1);
        data.CurrentTabIndex = newIndex;
        return new CloseTabResult(true, newIndex);
    }

    static void PushClosedTab(MemoData data, MemoTab tab)
    {
        data.ClosedTabs.Insert(0, tab);
        int limit = Math.Max(0, data.ClosedTabsLimit);
        while (data.ClosedTabs.Count > limit)
        {
            data.ClosedTabs.RemoveAt(data.ClosedTabs.Count - 1);
        }
    }

    public static int RestoreTab(MemoData data, int closedIndex)
    {
        var tab = data.ClosedTabs[closedIndex];
        data.ClosedTabs.RemoveAt(closedIndex);
        data.Tabs.Add(tab);
        data.CurrentTabIndex = data.Tabs.Count - 1;
        return data.CurrentTabIndex;
    }

    public static int ClampCurrentIndex(MemoData data)
    {
        if (data.Tabs.Count == 0)
        {
            return 0;
        }
        return Math.Min(Math.Max(data.CurrentTabIndex, 0), data.Tabs.Count - 1);
    }
}
