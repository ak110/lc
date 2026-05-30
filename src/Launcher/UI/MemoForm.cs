using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Launcher.Core;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// メモパッドフォーム。タブで複数のプレーンテキストメモを管理する。
/// 常に通常ウィンドウとし、最前面固定は設けない。
/// </summary>
public partial class MemoForm : Form
{
    readonly ApplicationHostForm owner;
    readonly ContextMenuStrip tabContextMenu;
    readonly System.Windows.Forms.Timer saveTimer;

    /// <summary>初期化・タブ再構築・ウィンドウ復元中の保存抑制ガード</summary>
    bool loading;

    /// <summary>全タブへ一括適用する現在のフォント</summary>
    Font? memoFont;

    MemoData Data => owner.MemoData;

    public MemoForm(ApplicationHostForm owner)
    {
        this.owner = owner;
        InitializeComponent();
        Text = $"{Infrastructure.AppVersion.Title} : メモパッド";

        // テキスト変更・位置/サイズ変更・タブ切替のデバウンス保存タイマー
        saveTimer = new System.Windows.Forms.Timer { Interval = 200 };
        saveTimer.Tick += (s, e) =>
        {
            saveTimer.Stop();
            SaveData();
        };
        components ??= new Container();
        components.Add(saveTimer);

        // タブヘッダー右クリックメニュー (内容はOpeningで動的構築)
        tabContextMenu = new ContextMenuStrip();
        tabContextMenu.Opening += TabContextMenu_Opening;
        tabControl1.ContextMenuStrip = tabContextMenu;
        tabControl1.MouseWheel += TabControl1_MouseWheel;
        tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

        LocationChanged += MemoForm_LocationChanged;
        SizeChanged += MemoForm_SizeChanged;

        // オーナー設定 (Show→Hideだと一瞬表示されるのでプロパティで設定)
        Owner = owner;
        _ = Handle;

        RestoreWindowBounds();
        BuildTabs();
    }

    #region 表示・非表示

    /// <summary>
    /// 表示中なら隠し、非表示なら表示してアクティブ化する。
    /// </summary>
    public void ToggleVisible()
    {
        if (Visible)
        {
            Hide();
        }
        else
        {
            ShowMemo();
        }
    }

    /// <summary>
    /// メモパッドを表示してアクティブ化する。
    /// </summary>
    public void ShowMemo()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }
        Show();
        WindowHelper.ActivateForce(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // オーナー(ApplicationHostForm)からの閉じる操作以外は非表示にするだけ
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

    #region タブ構築

    /// <summary>
    /// データから全タブを構築する。
    /// </summary>
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

    /// <summary>
    /// MemoTabに対応するタブページを生成する。
    /// </summary>
    TabPage CreateTabPage(MemoTab tab)
    {
#pragma warning disable CA2000 // TabControlがTabPageのライフサイクルを管理
        var page = new TabPage(tab.Name) { Tag = tab };
#pragma warning restore CA2000
        var textBox = new PlainRichTextBox { Text = tab.Text };
        if (memoFont is not null)
        {
            textBox.ApplyFont(memoFont);
        }
        // Textは初期化子で設定済みのため、ここで購読すれば初期化時のTextChangedは発生しない
        textBox.TextChanged += (s, e) =>
        {
            if (loading) return;
            tab.Text = textBox.Text;
            ScheduleSave();
        };
        page.Controls.Add(textBox);
        return page;
    }

    #endregion

    #region タブ操作

    void TabContextMenu_Opening(object? sender, CancelEventArgs e)
    {
        // カーソル直下のタブを検出し、見つかればSelectedIndexへ反映する
        var pos = tabControl1.PointToClient(Cursor.Position);
        int hit = -1;
        for (int i = 0; i < tabControl1.TabCount; i++)
        {
            if (tabControl1.GetTabRect(i).Contains(pos))
            {
                hit = i;
                break;
            }
        }
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

    /// <summary>
    /// 閉じたタブの復元メニュー用ラベルを作る。タブ名と本文先頭を併記する。
    /// </summary>
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
        string? name = ShowInputDialog("タブ名を入力してください:", "新しいタブ", $"メモ{Data.Tabs.Count + 1}");
        if (name is null) return;

        var tab = new MemoTab { Name = name };
        Data.Tabs.Add(tab);
        var page = CreateTabPage(tab);
        tabControl1.TabPages.Add(page);
        tabControl1.SelectedTab = page;
        Data.CurrentTabIndex = tabControl1.SelectedIndex;
        SaveData(); // タブ増減は即時保存
    }

    void RenameTab()
    {
        var page = tabControl1.SelectedTab;
        if (page is null) return;

        var tab = (MemoTab)page.Tag!;
        string? name = ShowInputDialog("新しいタブ名:", "タブ名の変更", tab.Name);
        if (name is null) return;

        tab.Name = name;
        page.Text = name;
        SaveData(); // 改名は即時保存
    }

    void CloseCurrentTab()
    {
        // tabControl1.TabPagesとData.TabsはBuildTabs以降つねに同順・同数で対応する。
        // SelectedIndexはData.Tabsのインデックスとしてそのまま使える。
        int index = tabControl1.SelectedIndex;
        if (index < 0) return;

        if (tabControl1.TabPages.Count <= 1)
        {
            MessageBox.Show(this, "最後のタブは閉じられません。", "確認",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var tab = Data.Tabs[index];
        // 内容のあるタブを閉じる際は確認する
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
        SaveData(); // タブ増減は即時保存
    }

    void RestoreClosedTab(int closedIndex)
    {
        if (closedIndex < 0 || closedIndex >= Data.ClosedTabs.Count) return;

        int newIndex = MemoPresenter.RestoreTab(Data, closedIndex);
        var tab = Data.Tabs[newIndex];
        var page = CreateTabPage(tab);
        tabControl1.TabPages.Add(page);
        tabControl1.SelectedTab = page;
        SaveData(); // タブ増減は即時保存
    }

    void TabControl1_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (loading) return;
        Data.CurrentTabIndex = tabControl1.SelectedIndex;
        ScheduleSave(); // タブ切替はデバウンス保存
    }

    void TabControl1_MouseWheel(object? sender, MouseEventArgs e)
    {
        int count = tabControl1.TabPages.Count;
        if (count <= 1) return;

        int index = tabControl1.SelectedIndex;
        if (e.Delta > 0)
        {
            tabControl1.SelectedIndex = (index - 1 + count) % count;
        }
        else if (e.Delta < 0)
        {
            tabControl1.SelectedIndex = (index + 1) % count;
        }
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

    /// <summary>
    /// データのフォント設定を全タブへ適用する。
    /// </summary>
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
            ScheduleSave(); // 位置変更はデバウンス保存
        }
    }

    void MemoForm_SizeChanged(object? sender, EventArgs e)
    {
        if (loading) return;
        if (Visible && WindowState == FormWindowState.Normal)
        {
            Data.WindowSize = Size;
            ScheduleSave(); // サイズ変更はデバウンス保存
        }
    }

    #endregion

    #region 保存

    void ScheduleSave()
    {
        saveTimer.Stop();
        saveTimer.Start();
    }

    /// <summary>
    /// デバウンス保存が残っていたら即座に保存する。
    /// </summary>
    public void FlushPendingSave()
    {
        if (saveTimer.Enabled)
        {
            saveTimer.Stop();
            SaveData();
        }
    }

    void SaveData()
    {
        try
        {
            Data.Serialize();
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Debug.WriteLine($"メモ保存失敗: {ex.Message}");
        }
    }

    #endregion

    #region ヘルパー

    /// <summary>
    /// 簡易入力ダイアログ。タブ名入力に使う。
    /// </summary>
    string? ShowInputDialog(string prompt, string title, string defaultValue)
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(300, 100),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
        };

        var label = new Label { Text = prompt, Left = 8, Top = 8, Width = 280 };
        var textBox = new TextBox { Text = defaultValue, Left = 8, Top = 32, Width = 280 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 120, Top = 64, Width = 75 };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Left = 200, Top = 64, Width = 75 };

        form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialogOver(this) == DialogResult.OK ? textBox.Text : null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            memoFont?.Dispose();
            tabContextMenu?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
