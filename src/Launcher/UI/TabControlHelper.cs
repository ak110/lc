namespace Launcher.UI;

/// <summary>タブヘッダーの当たり判定とホイール選択を共通化する。</summary>
public static class TabControlHelper
{
    public static int HitTest(TabControl tabs, Point clientPosition)
    {
        for (int i = 0; i < tabs.TabCount; i++)
        {
            if (tabs.GetTabRect(i).Contains(clientPosition)) return i;
        }
        return -1;
    }

    public static void SelectByWheel(TabControl tabs, int delta)
    {
        int count = tabs.TabCount;
        if (count <= 1 || delta == 0) return;
        int direction = delta > 0 ? -1 : 1;
        tabs.SelectedIndex = (tabs.SelectedIndex + direction + count) % count;
    }
}
