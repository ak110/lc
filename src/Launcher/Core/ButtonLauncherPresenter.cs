namespace Launcher.Core;

public static class ButtonLauncherPresenter
{
    public static Size CalculateWindowSize(
        int columns, int rows,
        int buttonWidth, int buttonHeight,
        int toolStripHeight, int tabHeaderHeight)
    {
        int width = columns * buttonWidth;
        int height = rows * buttonHeight + toolStripHeight + tabHeaderHeight;
        return new Size(width, height);
    }

    public static void SetDefaultTab(ButtonLauncherData data, int tabIndex)
    {
        data.DefaultTabIndex = tabIndex;
    }

    public record DeleteTabResult(bool Success, int NewSelectedIndex);

    public static DeleteTabResult DeleteTab(ButtonLauncherData data, int tabIndex)
    {
        if (data.Tabs.Count <= 1)
        {
            return new DeleteTabResult(false, -1);
        }

        data.Tabs.RemoveAt(tabIndex);

        if (data.DefaultTabIndex == tabIndex)
            data.DefaultTabIndex = 0;
        else if (data.DefaultTabIndex > tabIndex)
            data.DefaultTabIndex--;

        int newIndex = Math.Min(tabIndex, data.Tabs.Count - 1);
        return new DeleteTabResult(true, newIndex);
    }

    public static void MoveTab(ButtonLauncherData data, int fromIndex, int toIndex)
    {
        var tab = data.Tabs[fromIndex];
        data.Tabs.RemoveAt(fromIndex);
        data.Tabs.Insert(toIndex, tab);

        if (data.DefaultTabIndex == fromIndex)
            data.DefaultTabIndex = toIndex;
        else if (data.DefaultTabIndex == toIndex)
            data.DefaultTabIndex = fromIndex;
    }

    public static void SwapButtons(
        ButtonTab sourceTab, int srcRow, int srcCol,
        ButtonTab destTab, int destRow, int destCol,
        ButtonEntry dragEntry)
    {
        var destEntry = destTab.GetButton(destRow, destCol);

        destTab.SetButton(destRow, destCol, ButtonEntry.FromCommand(dragEntry, destRow, destCol));

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
