using System.ComponentModel;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

public partial class MemoForm : Form
{
    readonly ApplicationHostForm owner;
    readonly ContextMenuStrip tabContextMenu;
    readonly System.Windows.Forms.Timer saveTimer;
    readonly Dictionary<MemoTab, PlainRichTextBox> editedTabs = [];
    readonly ToolStripStatusLabel saveStatus;
    readonly ToolStripButton retrySaveButton;
    readonly ToolStripMenuItem saveMenuItem = new("保存(&S)")
    {
        Name = "saveMenuItem",
        ShortcutKeys = Keys.Control | Keys.S,
    };
    // 編集操作の3項目は選択が無いときハンドラが何もしないため、ショートカットを常に処理できるよう有効のまま保つ。
    // メニューのショートカットは本文のRichEditより先に処理され、RichEditの既定のCtrl+U・Ctrl+Lの書式操作より優先される。
    readonly ToolStripMenuItem sortLinesMenuItem = new("行のソート(&S)")
    {
        Name = "sortLinesMenuItem",
        ShortcutKeys = Keys.F9,
    };
    readonly ToolStripMenuItem upperCaseMenuItem = new("大文字に変換(&G)")
    {
        Name = "upperCaseMenuItem",
        ShortcutKeys = Keys.Control | Keys.U,
    };
    readonly ToolStripMenuItem lowerCaseMenuItem = new("小文字に変換(&L)")
    {
        Name = "lowerCaseMenuItem",
        ShortcutKeys = Keys.Control | Keys.L,
    };
    FindReplaceDialog? findReplaceDialog;
    bool savePending;
    bool saveFailed;
    ConfigSaveFailureKind? saveFailureKind;

    bool loading;

    Font? memoFont;

    MemoData Data => owner.MemoData;

    public MemoForm(ApplicationHostForm owner)
    {
        this.owner = owner;
        Owner = owner;
        InitializeComponent();
        Text = $"{Infrastructure.AppVersion.Title} : メモパッド";

        var statusStrip = new StatusStrip { Name = "saveStatusStrip", SizingGrip = false, ShowItemToolTips = true };
        saveStatus = new ToolStripStatusLabel("保存済み")
        {
            Name = "saveStatus",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        retrySaveButton = new ToolStripButton("再保存") { Name = "retrySaveButton", Enabled = false };
        retrySaveButton.Click += (s, e) => SaveData();
        statusStrip.Items.AddRange([saveStatus, retrySaveButton]);
        Controls.Add(statusStrip);
        Controls.SetChildIndex(statusStrip, 1);

        saveTimer = new System.Windows.Forms.Timer { Interval = 200 };
        saveTimer.Tick += (s, e) =>
        {
            saveTimer.Stop();
            SaveData();
        };
        components ??= new Container();
        components.Add(saveTimer);

        tabContextMenu = new ContextMenuStrip();
        tabContextMenu.Opening += TabContextMenu_Opening;
        tabControl1.ContextMenuStrip = tabContextMenu;
        tabControl1.MouseWheel += TabControl1_MouseWheel;
        tabControl1.MouseUp += TabControl1_MouseUp;
        tabControl1.MouseDoubleClick += TabControl1_MouseDoubleClick;
        tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

        LocationChanged += MemoForm_LocationChanged;
        SizeChanged += MemoForm_SizeChanged;

        WireMenu();

        _ = Handle;

        RestoreWindowBounds();
        BuildTabs();
    }

    #region 表示・非表示

    public void ToggleVisible()
    {
        if (Visible && WindowState != FormWindowState.Minimized)
        {
            Hide();
        }
        else
        {
            ShowMemo();
        }
    }

    public void HideIfVisible()
    {
        if (Visible)
        {
            FlushPendingSave();
            Hide();
        }
    }

    public void ShowMemo()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }
        Show();
        WindowHelper.ActivateForce(this);
        FocusCurrentTextBox();
    }

    void FocusCurrentTextBox()
    {
        var textBox = GetCurrentTextBox();
        textBox?.Focus();
    }

    PlainRichTextBox? GetCurrentTextBox()
    {
        var page = tabControl1.SelectedTab;
        if (page is null || page.Controls.Count == 0) return null;
        return page.Controls[0] as PlainRichTextBox;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.FormOwnerClosing)
        {
            e.Cancel = true;
            FlushPendingSave();
            Hide();
            return;
        }
        FlushPendingSave();
        base.OnFormClosing(e);
    }

    #endregion

    #region メニュー

    void WireMenu()
    {
        newTabMenuItem.Click += (s, e) => AddTab();
        saveMenuItem.Click += (s, e) => SaveData();
        fileMenu.DropDownItems.Insert(1, saveMenuItem);
        renameTabMenuItem.Click += (s, e) => RenameTab();
        closeTabMenuItem.Click += (s, e) => CloseCurrentTab();
        closeWindowMenuItem.Click += (s, e) => Close();

        fileMenu.DropDownOpening += (s, e) => UpdateFileMenu();

        undoMenuItem.Click += (s, e) => GetCurrentTextBox()?.Undo();
        redoMenuItem.Click += (s, e) => GetCurrentTextBox()?.Redo();
        cutMenuItem.Click += (s, e) => GetCurrentTextBox()?.Cut();
        copyMenuItem.Click += (s, e) => GetCurrentTextBox()?.Copy();
        pasteMenuItem.Click += (s, e) => GetCurrentTextBox()?.Paste();
        selectAllMenuItem.Click += (s, e) => GetCurrentTextBox()?.SelectAll();
        findMenuItem.Click += (s, e) => OpenFindDialog(replaceMode: false);
        replaceMenuItem.Click += (s, e) => OpenFindDialog(replaceMode: true);
        findNextMenuItem.Click += (s, e) => FindNextFromDialogOrOpen(reverse: false);
        findPrevMenuItem.Click += (s, e) => FindNextFromDialogOrOpen(reverse: true);
        sortLinesMenuItem.Click += (s, e) => SortSelectedLines();
        upperCaseMenuItem.Click += (s, e) => ConvertSelectedCase(upper: true);
        lowerCaseMenuItem.Click += (s, e) => ConvertSelectedCase(upper: false);
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.AddRange([sortLinesMenuItem, upperCaseMenuItem, lowerCaseMenuItem]);

        editMenu.DropDownOpening += (s, e) => UpdateEditMenu();

        fontMenuItem.Click += (s, e) => ChangeFont();

        zoomInMenuItem.Click += (s, e) => ZoomCurrentText(zoomIn: true);
        zoomOutMenuItem.Click += (s, e) => ZoomCurrentText(zoomIn: false);
        zoomResetMenuItem.Click += (s, e) => ResetCurrentZoom();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Shift | Keys.Oemplus:
            case Keys.Control | Keys.Add:
                ZoomCurrentText(zoomIn: true);
                return true;
            case Keys.Control | Keys.Subtract:
                ZoomCurrentText(zoomIn: false);
                return true;
            case Keys.Control | Keys.NumPad0:
                ResetCurrentZoom();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    void UpdateFileMenu()
    {
        renameTabMenuItem.Enabled = tabControl1.SelectedTab is not null;
        closeTabMenuItem.Enabled = tabControl1.TabPages.Count > 1;

        restoreClosedTabMenuItem.DropDownItems.Clear();
        if (Data.ClosedTabs.Count == 0)
        {
            restoreClosedTabMenuItem.Enabled = false;
        }
        else
        {
            restoreClosedTabMenuItem.Enabled = true;
            for (int i = 0; i < Data.ClosedTabs.Count; i++)
            {
                int index = i;
                restoreClosedTabMenuItem.DropDownItems.Add(
                    BuildRestoreLabel(Data.ClosedTabs[i]), null, (s, ev) => RestoreClosedTab(index));
            }
        }
    }

    void UpdateEditMenu()
    {
        var textBox = GetCurrentTextBox();
        bool hasBox = textBox is not null;
        bool hasSelection = hasBox && textBox!.SelectionLength > 0;
        undoMenuItem.Enabled = hasBox && textBox!.CanUndo;
        redoMenuItem.Enabled = hasBox && textBox!.CanRedo;
        cutMenuItem.Enabled = hasSelection;
        copyMenuItem.Enabled = hasSelection;
        pasteMenuItem.Enabled = hasBox && Clipboard.ContainsText();
        selectAllMenuItem.Enabled = hasBox && textBox!.TextLength > 0;
        findMenuItem.Enabled = hasBox;
        replaceMenuItem.Enabled = hasBox;
        findNextMenuItem.Enabled = hasBox;
        findPrevMenuItem.Enabled = hasBox;
    }

    #endregion

    #region 行・文字の変換

    void SortSelectedLines()
    {
        var textBox = GetCurrentTextBox();
        if (textBox is null || textBox.SelectionLength == 0) return;

        string replaced = TextLines.ComputeSortReplacement(
            textBox.Text, textBox.SelectionStart, textBox.SelectionLength, out int regionStart, out int regionLength);
        textBox.Select(regionStart, regionLength);
        textBox.SelectedText = replaced;
        textBox.Select(regionStart, replaced.Length);
    }

    void ConvertSelectedCase(bool upper)
    {
        var textBox = GetCurrentTextBox();
        if (textBox is null || textBox.SelectionLength == 0) return;

        int start = textBox.SelectionStart;
        string selected = textBox.SelectedText;
        string converted = upper ? selected.ToUpperInvariant() : selected.ToLowerInvariant();
        textBox.SelectedText = converted;
        textBox.Select(start, converted.Length);
    }

    #endregion

    #region 検索・置換

    void OpenFindDialog(bool replaceMode)
    {
        findReplaceDialog ??= CreateFindReplaceDialog();

        string? initial = null;
        var textBox = GetCurrentTextBox();
        if (textBox is not null && textBox.SelectionLength > 0 && !textBox.SelectedText.Contains('\n'))
        {
            initial = textBox.SelectedText;
        }
        findReplaceDialog.ShowOrActivate(this, replaceMode, initial);
    }

    FindReplaceDialog CreateFindReplaceDialog()
    {
        var dialog = new FindReplaceDialog();
        dialog.FindNext += (s, e) => ExecuteFind(e, reverse: false);
        dialog.FindPrev += (s, e) => ExecuteFind(e, reverse: true);
        dialog.Replace += (s, e) => ExecuteReplace(e);
        dialog.ReplaceAll += (s, e) => ExecuteReplaceAll(e);
        return dialog;
    }

    void FindNextFromDialogOrOpen(bool reverse)
    {
        findReplaceDialog ??= CreateFindReplaceDialog();
        if (!findReplaceDialog.TryGetFindCondition(out var args))
        {
            OpenFindDialog(replaceMode: false);
            return;
        }
        if (!ExecuteFind(args, reverse) && !findReplaceDialog.Visible)
        {
            string status = "見つかりませんでした。";
            OpenFindDialog(replaceMode: false);
            findReplaceDialog.SetStatus(status);
        }
    }

    bool ExecuteFind(FindEventArgs e, bool reverse)
    {
        var textBox = GetCurrentTextBox();
        if (textBox is null) return false;

        string text = textBox.Text;
        int startIndex;
        if (reverse)
        {
            startIndex = textBox.SelectionStart;
        }
        else
        {
            startIndex = textBox.SelectionStart + textBox.SelectionLength;
        }

        var (matchStart, matchLength) = MemoSearch.FindMatch(text, e.Pattern, startIndex, reverse, e.MatchCase, e.UseRegex);
        if (matchStart < 0)
        {
            int wrapStart = reverse ? text.Length : 0;
            (matchStart, matchLength) = MemoSearch.FindMatch(text, e.Pattern, wrapStart, reverse, e.MatchCase, e.UseRegex);
            if (matchStart < 0)
            {
                findReplaceDialog?.SetStatus("見つかりませんでした。");
                return false;
            }
            findReplaceDialog?.SetStatus("先頭/末尾まで達したため折り返しました。");
        }
        else
        {
            findReplaceDialog?.SetStatus(string.Empty);
        }

        textBox.Select(matchStart, matchLength);
        textBox.ScrollToCaret();
        return true;
    }

    void ExecuteReplace(ReplaceEventArgs e)
    {
        var textBox = GetCurrentTextBox();
        if (textBox is null) return;

        if (textBox.SelectionLength > 0 && MemoSearch.IsMatch(textBox.SelectedText, e.Pattern, e.MatchCase, e.UseRegex))
        {
            string replacement = MemoSearch.ComputeReplacement(textBox.SelectedText, e.Pattern, e.Replacement, e.MatchCase, e.UseRegex);
            textBox.SelectedText = replacement;
        }
        ExecuteFind(e, reverse: false);
    }

    void ExecuteReplaceAll(ReplaceEventArgs e)
    {
        var textBox = GetCurrentTextBox();
        if (textBox is null) return;

        string original = textBox.Text;
        int count;
        string replaced;
        try
        {
            replaced = MemoSearch.ReplaceAllInText(original, e.Pattern, e.Replacement, e.MatchCase, e.UseRegex, out count);
        }
        catch (ArgumentException ex)
        {
            findReplaceDialog?.SetStatus($"置換エラー: {ex.Message} "
                + "置換後の文字列は$1などの置換パターンと\\nなどのエスケープで書くか、「正規表現」のチェックを外して文字どおりに置換してください。");
            return;
        }

        if (count == 0)
        {
            findReplaceDialog?.SetStatus("見つかりませんでした。");
            return;
        }

        // 全選択→SelectedTextで置換することで取り消し履歴を1操作にまとめる
        int caret = textBox.SelectionStart;
        textBox.SelectAll();
        textBox.SelectedText = replaced;
        textBox.SelectionStart = Math.Min(caret, replaced.Length);
        textBox.SelectionLength = 0;
        findReplaceDialog?.SetStatus($"{count}件置換しました。");
    }

    #endregion

    #region タブ構築

    void BuildTabs()
    {
        loading = true;
        try
        {
            tabControl1.TabPages.Clear();
            if (Data.Tabs.Count == 0)
            {
                Data.Tabs.Add(new MemoTab { Name = "メモ1" });
            }
            memoFont ??= new Font(Data.FontName, Data.FontSize);

            foreach (var tab in Data.Tabs)
            {
                tabControl1.TabPages.Add(CreateTabPage(tab));
            }

            int sel = MemoPresenter.ClampCurrentIndex(Data);
            if (sel >= 0 && sel < tabControl1.TabPages.Count)
            {
                tabControl1.SelectedIndex = sel;
            }
            Data.CurrentTabIndex = sel;
        }
        finally
        {
            loading = false;
        }
    }

    TabPage CreateTabPage(MemoTab tab)
    {
#pragma warning disable CA2000
        var page = new TabPage(tab.Name) { Tag = tab };
#pragma warning restore CA2000
        var textBox = new PlainRichTextBox { Text = tab.Text };
        if (memoFont is not null)
        {
            textBox.ApplyFont(memoFont);
        }
        textBox.TextChanged += (s, e) =>
        {
            if (loading) return;
            editedTabs[tab] = textBox;
            ScheduleSave();
        };
        page.Controls.Add(textBox);
        return page;
    }

    #endregion

    #region タブ操作

    void TabContextMenu_Opening(object? sender, CancelEventArgs e)
    {
        var pos = tabControl1.PointToClient(Cursor.Position);
        int hit = TabControlHelper.HitTest(tabControl1, pos);
        if (hit >= 0)
        {
            tabControl1.SelectedIndex = hit;
        }

        tabContextMenu.Items.Clear();
        tabContextMenu.Items.Add("新しいタブ(&N)", null, (s, ev) => AddTab());
        var rename = tabContextMenu.Items.Add("タブ名の変更(&R)", null, (s, ev) => RenameTab());
        var close = tabContextMenu.Items.Add("タブを閉じる(&C)", null, (s, ev) => CloseCurrentTab());
        rename.Enabled = hit >= 0;
        close.Enabled = hit >= 0 && tabControl1.TabCount > 1;

        tabContextMenu.Items.Add(new ToolStripSeparator());
        var restore = new ToolStripMenuItem("閉じたタブを戻す(&U)");
        if (Data.ClosedTabs.Count == 0)
        {
            restore.Enabled = false;
        }
        else
        {
            for (int i = 0; i < Data.ClosedTabs.Count; i++)
            {
                int index = i;
                restore.DropDownItems.Add(BuildRestoreLabel(Data.ClosedTabs[i]), null, (s, ev) => RestoreClosedTab(index));
            }
        }
        tabContextMenu.Items.Add(restore);

        tabContextMenu.Items.Add(new ToolStripSeparator());
        tabContextMenu.Items.Add("フォント(&F)...", null, (s, ev) => ChangeFont());
    }

    static string BuildRestoreLabel(MemoTab tab)
    {
        string preview = tab.Text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (preview.Length > 20)
        {
            preview = preview[..20] + "…";
        }
        string label = preview.Length > 0 ? $"{tab.Name} : {preview}" : tab.Name;
        // メニュー項目の'&'はニーモニックと解釈されるためエスケープする
        return label.Replace("&", "&&");
    }

    void AddTab()
    {
        string? name = InputDialog.Prompt(this, "タブ名を入力してください:", "新しいタブ", $"メモ{Data.Tabs.Count + 1}");
        if (name is null) return;

        AddTabWithName(name);
    }

    void AddTabWithDefaultName() => AddTabWithName($"メモ{Data.Tabs.Count + 1}");

    void AddTabWithName(string name)
    {
        var tab = new MemoTab { Name = name };
        Data.Tabs.Add(tab);
        var page = CreateTabPage(tab);
        tabControl1.TabPages.Add(page);
        tabControl1.SelectedTab = page;
        Data.CurrentTabIndex = tabControl1.SelectedIndex;
        SaveData();
        FocusCurrentTextBox();
    }

    void RenameTab()
    {
        var page = tabControl1.SelectedTab;
        if (page is null) return;

        var tab = (MemoTab)page.Tag!;
        string? name = InputDialog.Prompt(this, "新しいタブ名:", "タブ名の変更", tab.Name);
        if (name is null) return;

        tab.Name = name;
        page.Text = name;
        SaveData();
    }

    void CloseCurrentTab()
    {
        int index = tabControl1.SelectedIndex;
        if (index < 0) return;

        if (tabControl1.TabPages.Count <= 1)
        {
            MessageBox.Show(this, "最後のタブは閉じられません。\n閉じるには、新しいタブ（Ctrl+T）を追加してから閉じてください。", "確認",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var tab = Data.Tabs[index];
        SyncEditedText();
        if (!string.IsNullOrEmpty(tab.Text))
        {
            if (MessageBox.Show(this, $"タブ「{tab.Name}」を閉じますか？\n閉じたタブは元に戻せます。", "確認",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
        }

        var result = MemoPresenter.CloseTab(Data, index);
        if (!result.Success) return;

        loading = true;
        try
        {
            tabControl1.TabPages.RemoveAt(index);
            if (result.NewCurrentIndex >= 0 && result.NewCurrentIndex < tabControl1.TabPages.Count)
            {
                tabControl1.SelectedIndex = result.NewCurrentIndex;
            }
        }
        finally
        {
            loading = false;
        }
        SaveData();
        FocusCurrentTextBox();
    }

    void RestoreClosedTab(int closedIndex)
    {
        if (closedIndex < 0 || closedIndex >= Data.ClosedTabs.Count) return;

        int newIndex = MemoPresenter.RestoreTab(Data, closedIndex);
        var tab = Data.Tabs[newIndex];
        var page = CreateTabPage(tab);
        tabControl1.TabPages.Add(page);
        tabControl1.SelectedTab = page;
        SaveData();
        FocusCurrentTextBox();
    }

    void TabControl1_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (loading) return;
        Data.CurrentTabIndex = tabControl1.SelectedIndex;
        ScheduleSave();
        FocusCurrentTextBox();
    }

    void TabControl1_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (TabControlHelper.HitTest(tabControl1, e.Location) >= 0) return;
        AddTabWithDefaultName();
    }

    void TabControl1_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Middle) return;
        int hit = TabControlHelper.HitTest(tabControl1, e.Location);
        if (hit < 0) return;
        tabControl1.SelectedIndex = hit;
        CloseCurrentTab();
    }

    void TabControl1_MouseWheel(object? sender, MouseEventArgs e)
    {
        TabControlHelper.SelectByWheel(tabControl1, e.Delta);
    }

    #endregion

    #region フォント

    void ChangeFont()
    {
        using var dlg = new FontDialog
        {
            Font = memoFont ?? Font,
            ShowEffects = false,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        Data.FontName = dlg.Font.Name;
        Data.FontSize = dlg.Font.Size;
        ApplyFontFromData();
        SaveData();
    }

    void ApplyFontFromData()
    {
        var old = memoFont;
        memoFont = new Font(Data.FontName, Data.FontSize);
        foreach (TabPage page in tabControl1.TabPages)
        {
            if (page.Controls.Count > 0 && page.Controls[0] is PlainRichTextBox textBox)
            {
                textBox.ApplyFont(memoFont);
            }
        }
        old?.Dispose();
    }

    #endregion

    #region 表示倍率

    void ZoomCurrentText(bool zoomIn)
    {
        var textBox = GetCurrentTextBox();
        if (textBox is null) return;
        if (MemoZoom.Step(textBox.ZoomFactor, zoomIn) is float next)
        {
            textBox.ZoomFactor = next;
        }
    }

    void ResetCurrentZoom()
    {
        var textBox = GetCurrentTextBox();
        if (textBox is null) return;
        // ZoomFactorの設定はキャッシュ値と同じなら何もしない。Ctrl+ホイールの変更はキャッシュへ反映されないため、
        // 取得でRichEditの現在値をキャッシュへ読み込んでから設定する。
        _ = textBox.ZoomFactor;
        textBox.ZoomFactor = MemoZoom.Default;
    }

    #endregion

    #region ウィンドウ位置・サイズ

    void RestoreWindowBounds()
    {
        loading = true;
        try
        {
            if (Data.WindowSize != Size.Empty)
            {
                Size = Data.WindowSize;
            }
            if (Data.WindowPos != Point.Empty)
            {
                FormsHelper.SetLocationWithClip(this, Data.WindowPos);
            }
        }
        finally
        {
            loading = false;
        }
    }

    void MemoForm_LocationChanged(object? sender, EventArgs e)
    {
        if (loading) return;
        if (Visible && WindowState == FormWindowState.Normal)
        {
            Data.WindowPos = Location;
            ScheduleSave();
        }
    }

    void MemoForm_SizeChanged(object? sender, EventArgs e)
    {
        if (loading) return;
        if (Visible && WindowState == FormWindowState.Normal)
        {
            Data.WindowSize = Size;
            ScheduleSave();
        }
    }

    #endregion

    #region 保存

    void ScheduleSave()
    {
        savePending = true;
        if (!saveFailed) saveStatus.Text = "未保存";
        saveTimer.Stop();
        saveTimer.Start();
    }

    public bool FlushPendingSave()
    {
        if (savePending)
        {
            SaveData();
        }
        return !savePending;
    }

    void SyncEditedText()
    {
        foreach (var (tab, editor) in editedTabs)
        {
            tab.Text = editor.Text;
        }
        editedTabs.Clear();
    }

    void SaveData()
    {
        savePending = true;
        saveTimer.Stop();
        SyncEditedText();
        bool saved = Data.Save(failure =>
        {
            saveFailureKind = failure.Kind;
            owner.ReportSaveFailure(failure);
        }, owner.ConfigurationBaseName);
        if (saved)
        {
            savePending = false;
            saveFailed = false;
            saveFailureKind = null;
            saveStatus.Text = "保存済み";
            saveStatus.ToolTipText = string.Empty;
            retrySaveButton.Enabled = false;
            saveMenuItem.Enabled = true;
        }
        else
        {
            saveFailed = true;
            bool blocked = saveFailureKind == ConfigSaveFailureKind.Blocked;
            saveStatus.Text = blocked
                ? "未保存: 内容をコピーしてから復元してください"
                : "未保存: 書き込みを確認して再保存してください";
            saveStatus.ToolTipText = blocked
                ? "読込失敗で保存が停止しています。編集中の本文を別の場所へコピーしてから、起動時の復元案内に従ってください。"
                : "メモを保存できませんでした。フォルダーの書き込み権限やファイルの使用状態を確認し、再保存してください。";
            retrySaveButton.Enabled = !blocked;
            saveMenuItem.Enabled = retrySaveButton.Enabled;
        }
    }

    #endregion

    #region ヘルパー

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            findReplaceDialog?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
        if (disposing)
        {
            // 子コントロールが参照するリソースは、base.Disposeが子の破棄を終えた後に解放する。
            // 破棄済みのフォントを本文編集コントロールが参照すると、
            // 破棄中に発生するサイズ変更の処理でGDI+が例外を返す。
            memoFont?.Dispose();
            tabContextMenu?.Dispose();
        }
    }

    #endregion
}
