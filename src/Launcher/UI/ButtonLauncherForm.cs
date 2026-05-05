using Launcher.Core;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// ボタン型ランチャーフォーム
/// </summary>
public partial class ButtonLauncherForm : Form
{
    readonly ApplicationHostForm owner;
    readonly AsyncIconLoader iconLoader = new();
    readonly ContextMenuStrip buttonContextMenu;
    readonly ContextMenuStrip tabContextMenu;
    readonly ContextMenuStrip mainMenu;

    /// <summary>ShowLauncher 再入防止フラグ (DoEvents 経由の多重呼び出しを防ぐ)</summary>
    bool showLauncherInProgress;

    // ボタンサイズ
    const int ButtonWidth = 64;
    const int ButtonHeight = 64;

    // D&D用
    Button? dragSource;              // UI固有のためForm側に残す
    readonly DragDropState dragState = new();

    ButtonLauncherData Data => owner.ButtonLauncherData;

    public ButtonLauncherForm(ApplicationHostForm owner, ContextMenuStrip mainMenu)
    {
        InitializeComponent();
        this.owner = owner;
        this.mainMenu = mainMenu;

        Text = Infrastructure.AppVersion.Title;

        // ボタン右クリックメニュー
        buttonContextMenu = new ContextMenuStrip();
        buttonContextMenu.Items.Add("実行(&X)", null, ButtonMenu_Execute);
        buttonContextMenu.Items.Add("編集(&E)", null, ButtonMenu_Edit);
        buttonContextMenu.Items.Add("フォルダを開く(&O)", null, ButtonMenu_OpenFolder);
        buttonContextMenu.Items.Add(new ToolStripSeparator());
        buttonContextMenu.Items.Add("コマンドから割り当て(&A)", null, ButtonMenu_AssignFromCommand);
        buttonContextMenu.Items.Add(new ToolStripSeparator());
        buttonContextMenu.Items.Add("削除(&D)", null, ButtonMenu_Delete);
        buttonContextMenu.Opening += ButtonContextMenu_Opening;

        // ツールバー右クリック時にメインメニューを表示
        toolStrip1.ContextMenuStrip = mainMenu;

        // タブヘッダー右クリックメニュー
        tabContextMenu = new ContextMenuStrip();
        tabContextMenu.Items.Add("タブを追加(&A)", null, (s, ev) => AddTab());
        var renameItem = tabContextMenu.Items.Add("タブ名を変更(&R)", null, (s, ev) => RenameTab());
        var defaultItem = tabContextMenu.Items.Add("デフォルトタブに設定(&D)", null, (s, ev) => SetDefaultTab());
        tabContextMenu.Items.Add(new ToolStripSeparator());
        var moveLeftItem = tabContextMenu.Items.Add("左に移動(&L)", null, (s, ev) => MoveTab(tabControl1.SelectedIndex, tabControl1.SelectedIndex - 1));
        var moveRightItem = tabContextMenu.Items.Add("右に移動(&G)", null, (s, ev) => MoveTab(tabControl1.SelectedIndex, tabControl1.SelectedIndex + 1));
        tabContextMenu.Items.Add(new ToolStripSeparator());
        var deleteItem = tabContextMenu.Items.Add("タブを削除(&X)", null, (s, ev) => DeleteTab());
        tabContextMenu.Opening += (s, ev) =>
        {
            // カーソル直下のタブを検出し、見つかればSelectedIndexに反映する
            // (タブヘッダー空白部での右クリックでは見つからず、対象タブ依存の項目を無効化する)
            var pos = tabControl1.PointToClient(Cursor.Position);
            int hitIndex = -1;
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(pos))
                {
                    hitIndex = i;
                    break;
                }
            }
            if (hitIndex >= 0)
            {
                tabControl1.SelectedIndex = hitIndex;
            }

            bool onTab = hitIndex >= 0;
            renameItem.Enabled = onTab;
            defaultItem.Enabled = onTab;
            deleteItem.Enabled = onTab && tabControl1.TabCount > 1;
            moveLeftItem.Enabled = onTab && hitIndex > 0;
            moveRightItem.Enabled = onTab && hitIndex < tabControl1.TabCount - 1;
        };

        // タブヘッダーの右クリックに標準でメニューを割り当て
        // (タブ余白部でも開くが、対象タブ依存項目はOpeningで無効化済み)
        tabControl1.ContextMenuStrip = tabContextMenu;

        // タブ間D&D対応
        tabControl1.AllowDrop = true;
        tabControl1.DragOver += TabControl1_DragOver;
        tabControl1.DragDrop += TabControl1_DragDrop;

        iconLoader.IconLoaded += IconLoader_IconLoaded;

        // ロック状態を復元
        lockButton.Checked = Data.IsLocked;
        ApplyLockState();

        // オーナー設定 (Show→Hideだと一瞬表示されるのでプロパティで設定)
        Owner = owner;
        // BuildTabs()内のiconLoader.Load()より先にハンドルを作成する
        // Handle未作成時にIconLoadedイベントが到着するとアイコンが破棄されるため
        _ = Handle;
        WindowHelper.DisableCloseButton(this);

        // タブを構築 (アイコン非同期読み込みを開始するためHandle作成後に実行)
        BuildTabs();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // オーナー(ApplicationHostForm)からの閉じる操作以外は非表示にするだけ
        if (e.CloseReason != CloseReason.FormOwnerClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    #region タブ・グリッド構築

    /// <summary>
    /// 全タブを構築
    /// </summary>
    private void BuildTabs()
    {
        tabControl1.TabPages.Clear();
        iconLoader.Clear();

        // タブがなければデフォルト1つ生成する
        if (Data.Tabs.Count == 0)
        {
            Data.Tabs.Add(new ButtonTab { Name = "Default" });
        }

        foreach (var tab in Data.Tabs)
        {
#pragma warning disable CA2000 // TabControlがTabPageのライフサイクルを管理
            var tabPage = new TabPage(tab.Name) { Tag = tab };
#pragma warning restore CA2000
            BuildGrid(tabPage, tab);
            tabControl1.TabPages.Add(tabPage);
        }

        // デフォルトタブ
        if (Data.DefaultTabIndex >= 0 && Data.DefaultTabIndex < tabControl1.TabPages.Count)
        {
            tabControl1.SelectedIndex = Data.DefaultTabIndex;
        }
    }

    /// <summary>
    /// タブ内にグリッドボタンを配置
    /// </summary>
    private void BuildGrid(TabPage tabPage, ButtonTab tabData)
    {
        tabPage.Controls.Clear();
        tabPage.ContextMenuStrip = mainMenu;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = Data.Columns,
            RowCount = Data.Rows,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            AutoScroll = false,
        };

        // レイアウト計算を一時停止して一括追加
        panel.SuspendLayout();

        // 均等配分
        panel.ColumnStyles.Clear();
        for (int c = 0; c < Data.Columns; c++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Data.Columns));
        }
        panel.RowStyles.Clear();
        for (int r = 0; r < Data.Rows; r++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / Data.Rows));
        }

        for (int r = 0; r < Data.Rows; r++)
        {
            for (int c = 0; c < Data.Columns; c++)
            {
                var entry = tabData.GetButton(r, c);
                var btn = CreateGridButton(entry, r, c);
                panel.Controls.Add(btn, c, r);
            }
        }

        panel.ResumeLayout(true);
        tabPage.Controls.Add(panel);
    }

    /// <summary>
    /// グリッド上のボタンを1つ作成
    /// </summary>
    private Button CreateGridButton(ButtonEntry? entry, int row, int col)
    {
        var btn = new Button
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.BottomCenter,
            ImageAlign = ContentAlignment.TopCenter,
            AllowDrop = true,
            ContextMenuStrip = buttonContextMenu,
            Tag = new ButtonPosition(row, col),
        };
        btn.FlatAppearance.BorderSize = 0;

        if (entry is not null && !entry.IsEmpty)
        {
            btn.Text = entry.Name ?? "";
            // アイコンの非同期読み込み
            iconLoader.Load(entry.FileName, false, btn);
        }
        else
        {
            btn.Text = "";
        }

        btn.Click += GridButton_Click;
        btn.MouseDown += GridButton_MouseDown;
        btn.MouseMove += GridButton_MouseMove;
        btn.MouseUp += GridButton_MouseUp;
        btn.DragEnter += GridButton_DragEnter;
        btn.DragDrop += GridButton_DragDrop;

        return btn;
    }

    /// <summary>
    /// 指定位置のボタンの表示を更新 (D&Dスワップ時の部分更新用)
    /// </summary>
    private void UpdateButton(TabPage? tabPage, ButtonTab tabData, int row, int col)
    {
        if (tabPage is null) return;

        // TableLayoutPanelからボタンを探す
        var panel = tabPage.Controls.Count > 0 ? tabPage.Controls[0] as TableLayoutPanel : null;
        if (panel is null) return;

        var btn = panel.GetControlFromPosition(col, row) as Button;
        if (btn is null) return;

        var entry = tabData.GetButton(row, col);
        if (entry is not null && !entry.IsEmpty)
        {
            btn.Text = entry.Name ?? "";
            btn.Image = null;
            iconLoader.Load(entry.FileName, false, btn);
        }
        else
        {
            btn.Text = "";
            btn.Image = null;
        }
    }

    /// <summary>
    /// ボタン位置情報
    /// </summary>
#pragma warning disable CA1852 // recordは暗黙的にsealed
    private record ButtonPosition(int Row, int Col);
#pragma warning restore CA1852

    #endregion

    #region 表示・非表示

    /// <summary>
    /// ランチャーを表示 (マウスカーソル中心に配置)
    /// </summary>
    public void ShowLauncher()
    {
        if (showLauncherInProgress) return;
        showLauncherInProgress = true;
        try
        {
            // Columns/Rowsからウィンドウサイズを計算
            ClientSize = ButtonLauncherPresenter.CalculateWindowSize(
                Data.Columns, Data.Rows,
                ButtonWidth, ButtonHeight,
                toolStrip1.Height, tabControl1.ItemSize.Height);

            // マウスカーソル中心に配置
            var cursor = Cursor.Position;
            int x = cursor.X - Width / 2;
            int y = cursor.Y - Height / 2;

            // 画面端クランプ
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(cursor))
                {
                    var area = screen.WorkingArea;
                    x = Math.Max(area.Left, Math.Min(x, area.Right - Width));
                    y = Math.Max(area.Top, Math.Min(y, area.Bottom - Height));
                    break;
                }
            }

            Location = new Point(x, y);

            // デフォルトタブに切り替え
            if (Data.DefaultTabIndex >= 0 && Data.DefaultTabIndex < tabControl1.TabPages.Count)
            {
                tabControl1.SelectedIndex = Data.DefaultTabIndex;
            }

            // Hide→Show→ActivateForceの順で確実にアクティブ化
            Hide();
            Show();
            WindowHelper.ActivateForce(this);
        }
        catch (InvalidOperationException ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowLauncher失敗: {ex}");
            MessageBox.Show($"ボタンランチャーの表示に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowLauncher失敗: {ex}");
            MessageBox.Show($"ボタンランチャーの表示に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            showLauncherInProgress = false;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Hide();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        // ロック解除時は非表示
        if (!Data.IsLocked)
        {
            Hide();
        }
    }

    #endregion

    #region ロック機能

    private void lockButton_Click(object? sender, EventArgs e)
    {
        Data.IsLocked = lockButton.Checked;
        ApplyLockState();
        SaveData();
    }

    private void ApplyLockState()
    {
        lockButton.Text = Data.IsLocked ? "Locked" : "Lock";
        TopMost = true;
    }

    #endregion

    #region ボタンクリック・コマンド実行

    private void GridButton_Click(object? sender, EventArgs e)
    {
        var btn = (Button)sender!;
        var pos = (ButtonPosition)btn.Tag!;
        var tabData = GetCurrentTabData();
        if (tabData is null) return;

        var entry = tabData.GetButton(pos.Row, pos.Col);
        if (entry is null || entry.IsEmpty) return;

        // 実行
        try
        {
            // ShellExecuteExのhwndに自身のハンドルを渡し、現在のモニターでアプリを起動させる
            entry.Execute("", owner.Config, Handle);
            if (!Data.IsLocked)
            {
                Hide();
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            MessageBox.Show(this, $"実行に失敗しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, $"実行に失敗しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, $"実行に失敗しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region 右クリックメニュー

    private Button? contextMenuTarget;

    /// <summary>
    /// ボタン右クリックメニューのOpening時にメニュー項目の有効/無効を設定
    /// </summary>
    private void ButtonContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var menu = (ContextMenuStrip)sender!;
        contextMenuTarget = menu.SourceControl as Button;
        if (contextMenuTarget is null)
        {
            e.Cancel = true;
            return;
        }

        var pos = (ButtonPosition)contextMenuTarget.Tag!;
        var tabData = GetCurrentTabData();
        var entry = tabData?.GetButton(pos.Row, pos.Col);

        bool hasCommand = entry is not null && !entry.IsEmpty;
        buttonContextMenu.Items[0].Enabled = hasCommand; // 実行
        buttonContextMenu.Items[1].Enabled = true; // 編集 (未割当でも可)
        buttonContextMenu.Items[2].Enabled = hasCommand; // フォルダを開く
        buttonContextMenu.Items[6].Enabled = hasCommand; // 削除
    }

    private void GridButton_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Data.IsLocked)
        {
            // ロック時のD&D開始準備 (実際のDoDragDropはMouseMoveで閾値超過時に呼ぶ)
            var btn = (Button)sender!;
            var pos = (ButtonPosition)btn.Tag!;
            var tabData = GetCurrentTabData();
            var entry = tabData?.GetButton(pos.Row, pos.Col);
            if (entry is not null && !entry.IsEmpty)
            {
                dragSource = btn;
                dragState.Start(entry, tabData!, e.Location);
            }
        }
    }

    private void ButtonMenu_Execute(object? sender, EventArgs e)
    {
        if (contextMenuTarget is null) return;
        GridButton_Click(contextMenuTarget, EventArgs.Empty);
    }

    private void ButtonMenu_Edit(object? sender, EventArgs e)
    {
        if (contextMenuTarget is null) return;
        var pos = (ButtonPosition)contextMenuTarget.Tag!;
        var tabData = GetCurrentTabData();
        if (tabData is null) return;

        var entry = tabData.GetButton(pos.Row, pos.Col);
        bool isNew = entry is null || entry.IsEmpty;

        if (isNew)
        {
            // 未割当ボタン: 新規エントリを作成して編集
            entry = new ButtonEntry { Row = pos.Row, Col = pos.Col };
        }

        using var form = new EditCommandForm(entry!);
        if (form.ShowDialogOver(this) == DialogResult.OK)
        {
            if (isNew)
            {
                // 新規: FileNameが設定されていれば保存
                if (!entry!.IsEmpty)
                {
                    tabData.SetButton(pos.Row, pos.Col, entry);
                }
            }
            contextMenuTarget!.Text = entry!.Name ?? "";
            iconLoader.Load(entry.FileName, false, contextMenuTarget);
            SaveData();
        }
    }

    private void ButtonMenu_OpenFolder(object? sender, EventArgs e)
    {
        if (contextMenuTarget is null) return;
        var pos = (ButtonPosition)contextMenuTarget.Tag!;
        var tabData = GetCurrentTabData();
        var entry = tabData?.GetButton(pos.Row, pos.Col);
        if (entry is null || entry.IsEmpty) return;

        entry.OpenDirectory(owner.Config);
    }

    private void ButtonMenu_AssignFromCommand(object? sender, EventArgs e)
    {
        if (contextMenuTarget is null) return;
        var pos = (ButtonPosition)contextMenuTarget.Tag!;
        var tabData = GetCurrentTabData();
        if (tabData is null) return;

        // コマンド選択ダイアログ
        using var dlg = new CommandSelectDialog(owner.CommandList);
        if (dlg.ShowDialogOver(this) == DialogResult.OK && dlg.SelectedCommand is not null)
        {
            var newEntry = ButtonEntry.FromCommand(dlg.SelectedCommand, pos.Row, pos.Col);
            tabData.SetButton(pos.Row, pos.Col, newEntry);
            contextMenuTarget.Text = newEntry.Name ?? "";
            iconLoader.Load(newEntry.FileName, false, contextMenuTarget);
            SaveData();
        }
    }

    private void ButtonMenu_Delete(object? sender, EventArgs e)
    {
        if (contextMenuTarget is null) return;
        var pos = (ButtonPosition)contextMenuTarget.Tag!;
        var tabData = GetCurrentTabData();
        if (tabData is null) return;

        tabData.SetButton(pos.Row, pos.Col, null);
        contextMenuTarget.Text = "";
        contextMenuTarget.Image = null;
        SaveData();
    }

    #endregion

    #region ファイルD&D / ボタンD&D

    private void GridButton_MouseMove(object? sender, MouseEventArgs e)
    {
        if (dragSource is null || !dragState.IsActive) return;
        if (e.Button != MouseButtons.Left) return;

        // ドラッグ閾値を超えたらDoDragDropを開始
        if (dragState.ShouldBeginDrag(e.Location, SystemInformation.DragSize))
        {
            var btn = (Button)sender!;
            btn.DoDragDrop(dragState.DragEntry!, DragDropEffects.Move);
            // DoDragDropはブロッキング。戻り後にフィールドをクリア
            dragSource = null;
            dragState.Reset();
        }
    }

    private void GridButton_MouseUp(object? sender, MouseEventArgs e)
    {
        // ドラッグ閾値未到達でリリースした場合のクリア (通常クリック動作を壊さない)
        if (e.Button == MouseButtons.Left)
        {
            dragSource = null;
            dragState.Reset();
        }
    }

    private void GridButton_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data!.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.Link;
        }
        else if (dragSource is not null)
        {
            e.Effect = DragDropEffects.Move;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void GridButton_DragDrop(object? sender, DragEventArgs e)
    {
        var btn = (Button)sender!;
        var pos = (ButtonPosition)btn.Tag!;
        var destTabData = GetCurrentTabData();
        if (destTabData is null) return;

        if (e.Data!.GetDataPresent(DataFormats.FileDrop))
        {
            // ファイルD&D → コマンド登録
            string[] files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            if (files.Length > 0)
            {
                var cmd = Command.FromFile(files[0]);
                var entry = ButtonEntry.FromCommand(cmd, pos.Row, pos.Col);
                destTabData.SetButton(pos.Row, pos.Col, entry);
                btn.Text = entry.Name ?? "";
                iconLoader.Load(entry.FileName, false, btn);
                SaveData();
            }
        }
        else if (dragSource is not null && dragState.IsActive && dragState.SourceTab is not null)
        {
            // ボタン間D&D (クロスタブ対応)
            var srcPos = (ButtonPosition)dragSource.Tag!;
            var srcTabData = dragState.SourceTab;

            ButtonLauncherPresenter.SwapButtons(
                srcTabData, srcPos.Row, srcPos.Col,
                destTabData, pos.Row, pos.Col,
                dragState.DragEntry!);

            if (srcTabData != destTabData)
            {
                // クロスタブ: ソースタブのボタンを部分更新
                UpdateButton(FindTabPage(srcTabData), srcTabData, srcPos.Row, srcPos.Col);
            }
            // デスティネーション側の2ボタンを部分更新
            UpdateButton(tabControl1.SelectedTab, destTabData, pos.Row, pos.Col);
            if (srcTabData == destTabData)
            {
                UpdateButton(tabControl1.SelectedTab, destTabData, srcPos.Row, srcPos.Col);
            }
            SaveData();
        }
    }

    /// <summary>
    /// タブヘッダー上でのドラッグオーバー: マウス位置のタブに切り替え
    /// </summary>
    private void TabControl1_DragOver(object? sender, DragEventArgs e)
    {
        if (dragSource is null) { e.Effect = DragDropEffects.None; return; }

        var pt = tabControl1.PointToClient(new Point(e.X, e.Y));
        for (int i = 0; i < tabControl1.TabCount; i++)
        {
            if (tabControl1.GetTabRect(i).Contains(pt))
            {
                if (tabControl1.SelectedIndex != i)
                {
                    tabControl1.SelectedIndex = i;
                }
                break;
            }
        }
        e.Effect = DragDropEffects.Move;
    }

    /// <summary>
    /// タブヘッダー上にドロップされた場合のフォールバック
    /// </summary>
    private void TabControl1_DragDrop(object? sender, DragEventArgs e)
    {
        // タブヘッダー上にドロップされた場合は何もしない (ボタン上へのドロップで処理される)
    }

    #endregion

    #region タブ管理

    private void AddTab()
    {
        string? name = ShowInputDialog("タブ名を入力してください:", "タブの追加", $"Tab{Data.Tabs.Count + 1}");
        if (name is null) return;

        var tab = new ButtonTab { Name = name };
        Data.Tabs.Add(tab);

        var tabPage = new TabPage(name) { Tag = tab };
        BuildGrid(tabPage, tab);
        tabControl1.TabPages.Add(tabPage);
        tabControl1.SelectedTab = tabPage;
        SaveData();
    }

    private void RenameTab()
    {
        var tabPage = tabControl1.SelectedTab;
        if (tabPage is null) return;

        var tabData = (ButtonTab)tabPage.Tag!;
        string? name = ShowInputDialog("新しいタブ名:", "タブ名の変更", tabData.Name);
        if (name is null) return;

        tabData.Name = name;
        tabPage.Text = name;
        SaveData();
    }

    private void SetDefaultTab()
    {
        ButtonLauncherPresenter.SetDefaultTab(Data, tabControl1.SelectedIndex);
        SaveData();
    }

    private void DeleteTab()
    {
        if (tabControl1.TabPages.Count <= 1)
        {
            MessageBox.Show(this, "最後のタブは削除できません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var tabPage = tabControl1.SelectedTab;
        if (tabPage is null) return;

        var tabData = (ButtonTab)tabPage.Tag!;
        if (MessageBox.Show(this, $"タブ「{tabData.Name}」を削除しますか？", "確認",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        int tabIndex = Data.Tabs.IndexOf(tabData);
        var result = ButtonLauncherPresenter.DeleteTab(Data, tabIndex);
        if (!result.Success) return;

        // UI更新: TabPageを削除
        tabControl1.TabPages.Remove(tabPage);

        SaveData();
    }

    private void MoveTab(int fromIndex, int toIndex)
    {
        // データモデルの順序を入れ替え + DefaultTabIndex調整
        ButtonLauncherPresenter.MoveTab(Data, fromIndex, toIndex);

        // TabControlの順序を入れ替え
        var tabPage = tabControl1.TabPages[fromIndex];
        tabControl1.TabPages.Remove(tabPage);
        tabControl1.TabPages.Insert(toIndex, tabPage);

        tabControl1.SelectedIndex = toIndex;
        SaveData();
    }

    private void tabControl1_MouseWheel(object? sender, MouseEventArgs e)
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

    #region アイコン読み込み

    private void IconLoader_IconLoaded(object? sender, IconLoadedEventArgs e)
    {
        if (e.Generation != iconLoader.Generation)
        {
            e.Icon?.Dispose();
            return;
        }
        if (!IsHandleCreated)
        {
            e.Icon?.Dispose();
            return;
        }

        BeginInvoke(() =>
        {
            try
            {
                if (IsDisposed) return;
                if (e.Icon is null) return;

                var btn = e.Arg as Button;
                if (btn is null || btn.IsDisposed) return;

                btn.Image = e.Icon.ToBitmap();
                // 非選択タブのボタンはInvalidate()では再描画されないため親パネル全体を対象にする
                btn.Parent?.Invalidate(true);
            }
            finally
            {
                e.Icon?.Dispose();
            }
        });
    }

    #endregion

    #region ヘルパー

    /// <summary>
    /// グリッドサイズ変更を適用 (ウィンドウサイズ再計算 + タブ再構築)
    /// </summary>
    public void ApplyGridSize()
    {
        ClientSize = ButtonLauncherPresenter.CalculateWindowSize(
            Data.Columns, Data.Rows,
            ButtonWidth, ButtonHeight,
            toolStrip1.Height, tabControl1.ItemSize.Height);
        BuildTabs();
    }

    private ButtonTab? GetCurrentTabData()
    {
        return tabControl1.SelectedTab?.Tag as ButtonTab;
    }

    private void RebuildCurrentTab()
    {
        RebuildTab(tabControl1.SelectedTab);
    }

    /// <summary>
    /// 指定タブページのグリッドを再構築
    /// </summary>
    private void RebuildTab(TabPage? tabPage)
    {
        if (tabPage is null) return;
        iconLoader.Clear();
        var tabData = (ButtonTab)tabPage.Tag!;
        BuildGrid(tabPage, tabData);
    }

    /// <summary>
    /// ButtonTabに対応するTabPageを検索
    /// </summary>
    private TabPage? FindTabPage(ButtonTab tabData)
    {
        foreach (TabPage page in tabControl1.TabPages)
        {
            if (page.Tag == tabData) return page;
        }
        return null;
    }

    private void SaveData()
    {
        Data.Serialize();
    }

    /// <summary>
    /// 簡易入力ダイアログ
    /// </summary>
    private string? ShowInputDialog(string prompt, string title, string defaultValue)
    {
        using var form = new Form();
        form.Text = title;
        form.ClientSize = new Size(300, 100);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterParent;
        form.MaximizeBox = false;
        form.MinimizeBox = false;

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
            iconLoader.Dispose();
            buttonContextMenu?.Dispose();
            tabContextMenu?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
