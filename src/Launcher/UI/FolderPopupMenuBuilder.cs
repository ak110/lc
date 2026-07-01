using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// フォルダ内容を階層メニュー (ContextMenuStrip) として組み立てるビルダー。
/// サブフォルダは DropDownOpening で遅延展開する。
/// </summary>
public sealed class FolderPopupMenuBuilder
{
    // 遅延展開判定に使うプレースホルダー文字列
    const string LoadingLabel = "(読み込み中)";

    readonly IntPtr ownerHwnd;

    public FolderPopupMenuBuilder(IntPtr ownerHwnd)
    {
        this.ownerHwnd = ownerHwnd;
    }

    /// <summary>
    /// folderPath 直下の内容を表すメニューを構築する。
    /// 返した ContextMenuStrip は Closed イベントで自身を Dispose する。
    /// </summary>
    public ContextMenuStrip Build(string folderPath)
    {
        var menu = new ContextMenuStrip();
        // メニュー破棄時は自身をDisposeし、配下のToolStripMenuItem・Imageも連鎖破棄させる
        menu.Closed += (_, _) => menu.Dispose();
        PopulateItems(menu.Items, folderPath);
        return menu;
    }

#pragma warning disable CA2000 // ContextMenuStrip が配下 ToolStripItem のライフサイクルを管理
    void PopulateItems(ToolStripItemCollection items, string folderPath)
    {
        items.Clear();
        var entries = FolderEntryEnumerator.Enumerate(folderPath);
        if (entries.Count == 0)
        {
            items.Add(new ToolStripMenuItem("(空)") { Enabled = false });
            return;
        }

        foreach (var entry in entries)
        {
            var item = new ToolStripMenuItem(entry.DisplayName)
            {
                Image = LoadIconOrNull(entry.FullPath),
                Tag = entry,
            };
            if (entry.IsDirectory)
            {
                // 遅延展開: 開くまでダミー項目を1件だけ入れておく
                item.DropDownItems.Add(new ToolStripMenuItem(LoadingLabel) { Enabled = false });
                item.DropDownOpening += (_, _) =>
                {
                    if (item.DropDownItems.Count == 1 &&
                        item.DropDownItems[0].Text == LoadingLabel)
                    {
                        PopulateItems(item.DropDownItems, entry.FullPath);
                    }
                };
            }
            else
            {
                item.Click += (_, _) =>
                    ProcessLauncher.Start(new ShellProcessStartInfo(entry.FullPath));
            }
            item.MouseUp += (_, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                ShellContextMenuInvoker.Show(entry.FullPath, ownerHwnd, Cursor.Position);
            };
            items.Add(item);
        }
    }
#pragma warning restore CA2000

    static Bitmap? LoadIconOrNull(string path)
    {
        try
        {
            // Icon.ToBitmap() は新規Bitmapを返す。元のIconはusingで破棄。
            // 返したBitmapはToolStripMenuItem.Imageに設定され、
            // ContextMenuStrip.Dispose時に連鎖破棄される。
            using var icon = IconExtractor.ExtractAssociatedIcon(path, true);
            return icon.ToBitmap();
        }
        catch (FileLoadException)
        {
            return null;
        }
    }
}
