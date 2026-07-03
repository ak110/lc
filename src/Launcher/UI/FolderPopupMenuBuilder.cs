using System.ComponentModel;
using System.Runtime.InteropServices;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// フォルダ内容を階層メニュー (ContextMenuStrip) として組み立てるビルダー。
/// AsyncIconLoader パターンで非同期にアイコンを取得する。
/// サブフォルダは DropDownOpening で遅延展開する。
/// 大量項目時の応答性確保のため 1 階層あたりの表示件数上限を設ける。
/// </summary>
public sealed class FolderPopupMenuBuilder : IDisposable
{
    const string LoadingLabel = "(読み込み中)";
    const int MaxItemsPerLevel = 50;
    // グリッド全体用の8本に対しper-menu用途は4本に限定する。
    // 表示件数上限50件に対して4本で応答性を確保でき、
    // per-menu生成のたびに8本を立てるとリソース消費が過大になるため。
    // 詳細は .claude/rules/threading.md「アイコンローダーの並行度」節を参照。
    const int IconWorkerCount = 4;

    readonly IntPtr ownerHwnd;
    readonly Control ownerControl;
    readonly AsyncIconLoader iconLoader;

    public FolderPopupMenuBuilder(IntPtr ownerHwnd, Control ownerControl)
    {
        this.ownerHwnd = ownerHwnd;
        this.ownerControl = ownerControl;
        iconLoader = new AsyncIconLoader(workerCount: IconWorkerCount);
        iconLoader.IconLoaded += IconLoader_IconLoaded;
    }

    /// <summary>
    /// AsyncIconLoader を破棄する。Build 経由の Closed イベントでも呼ばれ得るが、
    /// AsyncIconLoader.Dispose は冪等のため二重呼び出しでも問題ない。
    /// </summary>
    public void Dispose()
    {
        iconLoader.IconLoaded -= IconLoader_IconLoaded;
        iconLoader.Dispose();
    }

    /// <summary>
    /// folderPath 直下の内容を表すメニューを構築する。
    /// 返した ContextMenuStrip は Closed イベントで自身と iconLoader を Dispose する。
    /// </summary>
    public ContextMenuStrip Build(string folderPath)
    {
        var menu = new ContextMenuStrip();
        // Closed 内での同期 Dispose は WinForms 内部の後始末処理と衝突するため
        // BeginInvoke で次のメッセージループへ遅延する。
        // ガード発火（ownerControl 破棄）時は同期 menu.Dispose を対象外とし、
        // ビルダー側の解放のみ onSkipped で保証する（menu と ContextMenuStrip の
        // ライフサイクルは ownerControl 破棄時に併せて GC 対象化される想定）。
        // ApplicationHostForm.ApplyConfig 経由でボタンランチャーが無効化される場合も同経路で解放する。
        // 詳細は .claude/rules/win32-interop.md「ContextMenuStripのClosedイベントでのDispose遅延」節
        // および .claude/rules/threading.md「UIスレッドBeginInvoke内例外の回送」節を参照。
        menu.Closed += (_, _) => UiThreadDispatcher.SafeBeginInvoke(ownerControl, () =>
        {
            menu.Dispose();
            Dispose();
        }, onSkipped: Dispose);
        AttachKeyDownHandler(menu, menu);
        PopulateItems(menu, menu.Items, folderPath, topMenu: menu);
        return menu;
    }

#pragma warning disable CA2000 // ContextMenuStrip が配下 ToolStripItem のライフサイクルを管理
    void PopulateItems(
        ToolStrip owner, ToolStripItemCollection items, string folderPath,
        ContextMenuStrip topMenu)
    {
        items.Clear();

        var entries = FolderEntryEnumerator.Enumerate(folderPath);
        if (entries.Count == 0)
        {
            items.Add(new ToolStripMenuItem("(空)") { Enabled = false });
            return;
        }

        int totalCount = entries.Count;
        int visibleCount = Math.Min(totalCount, MaxItemsPerLevel);
        int omittedCount = totalCount - visibleCount;

        // ToolStrip.Items.Add は項目ごとに Layout 計算が発生するため、
        // SuspendLayout/ResumeLayout でまとめて計算する
        owner.SuspendLayout();
        try
        {
            for (int i = 0; i < visibleCount; i++)
            {
                var entry = entries[i];
                var item = new ToolStripMenuItem(entry.DisplayName) { Tag = entry };
                if (entry.IsDirectory)
                {
                    item.DropDownItems.Add(new ToolStripMenuItem(LoadingLabel) { Enabled = false });
                    item.DropDownOpening += (_, _) =>
                    {
                        if (item.DropDownItems.Count == 1 &&
                            item.DropDownItems[0].Text == LoadingLabel)
                        {
                            PopulateItems(
                                item.DropDown, item.DropDownItems, entry.FullPath,
                                topMenu: topMenu);
                            AttachKeyDownHandler(item.DropDown, topMenu);
                        }
                    };
                }
                AttachShellMouseHandler(item, entry.FullPath, topMenu);
                items.Add(item);

                // 非同期アイコン取得: arg に item を渡し IconLoaded で差し替える
                iconLoader.Load(entry.FullPath, small: true, arg: item);
            }
            if (omittedCount > 0)
            {
                items.Add(new ToolStripMenuItem($"(以下 {omittedCount} 件を省略)") { Enabled = false });
            }
        }
        finally
        {
            owner.ResumeLayout(true);
        }
    }
#pragma warning restore CA2000

    /// <summary>
    /// ContextMenuStrip項目にマウス操作対応のみを登録する。
    /// `MouseUp`単独で左右分岐し、`Click`ハンドラは登録しない。
    /// .NET 10 WinFormsの`ToolStripMenuItem`はマウス右クリック時にも`Click`が発火する経路があり、
    /// `Click`ハンドラ併用は右クリック時の意図しないShell実行を招くため使わない。
    /// キーボードEnter対応は`ContextMenuStrip.KeyDown`で別途担う（`AttachKeyDownHandler`）。
    /// 詳細は.claude/rules/win32-interop.md
    /// 「ContextMenuStrip項目のマウスボタン別ハンドラ設計」節を参照。
    /// </summary>
    void AttachShellMouseHandler(
        ToolStripMenuItem item, string path, ContextMenuStrip topMenu)
    {
        item.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                topMenu.Close(ToolStripDropDownCloseReason.ItemClicked);
                InvokeShellExecuteDeferred(path);
            }
            else if (e.Button == MouseButtons.Right)
            {
                topMenu.Close(ToolStripDropDownCloseReason.ItemClicked);
                InvokeContextMenuDeferred(path);
            }
            // 中ボタンなど左右以外のボタン押下時は何もしない
        };
    }

    /// <summary>
    /// <paramref name="menu"/>にキーボードEnter対応ハンドラを登録する。
    /// `ToolStripDropDown.KeyDown`でEnterキーを検知し、
    /// 選択中の項目に対して左クリック相当のShell実行を発火する。
    /// 発火後は<paramref name="topMenu"/>を`ToolStripDropDownCloseReason.Keyboard`で閉じる。
    /// 項目の`Tag`に`FolderEntry`を格納した項目のみ対象とする。
    /// トップメニューとサブメニューの両方で共通に利用する。
    /// </summary>
    void AttachKeyDownHandler(ToolStripDropDown menu, ContextMenuStrip topMenu)
    {
        menu.KeyDown += (sender, e) =>
        {
            // Keys.Return と Keys.Enter は同一列挙値のため片方のみ比較で足りる。
            if (e.KeyCode != Keys.Enter) return;
            var strip = (ToolStrip)sender!;
            // 選択中の項目は公開プロパティ`ToolStripItem.Selected`で判定する。
            foreach (ToolStripItem item in strip.Items)
            {
                if (item is not ToolStripMenuItem menuItem) continue;
                if (!menuItem.Selected) continue;
                if (menuItem.Tag is not FolderEntry entry) return;
                topMenu.Close(ToolStripDropDownCloseReason.Keyboard);
                InvokeShellExecuteDeferred(entry.FullPath);
                e.Handled = true;
                return;
            }
        };
    }

    void IconLoader_IconLoaded(object? sender, IconLoadedEventArgs e)
    {
        if (e.Generation != iconLoader.Generation) { e.Icon?.Dispose(); return; }
        // ownerControl のガードは SafeBeginInvoke 内部で行い、
        // ガード発火時の Icon 解放は onSkipped に委ねる。
        UiThreadDispatcher.SafeBeginInvoke(ownerControl, () =>
        {
            try
            {
                if (ownerControl.IsDisposed) return;
                if (e.Icon is null) return;
                var item = e.Arg as ToolStripMenuItem;
                if (item is null || item.IsDisposed) return;
                item.Image = e.Icon.ToBitmap();
            }
            finally
            {
                e.Icon?.Dispose();
            }
        }, onSkipped: () => e.Icon?.Dispose());
    }

    void InvokeShellExecuteDeferred(string path)
    {
        // 親メニューが完全に閉じた後に Shell 呼び出しを実行する
        UiThreadDispatcher.SafeBeginInvoke(ownerControl, () =>
        {
            try
            {
                ProcessLauncher.Start(new ShellProcessStartInfo(path));
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(ownerControl,
                    $"開けませんでした: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });
    }

    void InvokeContextMenuDeferred(string path)
    {
        UiThreadDispatcher.SafeBeginInvoke(ownerControl, () =>
        {
            DiagnosticLog.Trace("Popup.InvokeContextMenu",
                $"before Show path={path}");
            try
            {
                ShellContextMenuInvoker.Show(path, ownerHwnd, Cursor.Position);
                DiagnosticLog.Trace("Popup.InvokeContextMenu", "after Show OK");
            }
            // Win32Exception も COMException も ExternalException のサブクラスであるため
            // ExternalException で両方をカバーできる。
            catch (ExternalException ex)
            {
                DiagnosticLog.TraceException("Popup.InvokeContextMenu", ex);
                MessageBox.Show(ownerControl,
                    $"シェルメニューの表示に失敗しました: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });
    }
}
