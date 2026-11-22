using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

public partial class ButtonLauncherForm : Form
{
    readonly ApplicationHostForm owner;
    readonly AsyncIconLoader iconLoader = new();
    readonly ContextMenuStrip buttonContextMenu;
    readonly ContextMenuStrip tabContextMenu;
    readonly ContextMenuStrip mainMenu;

    const int ButtonWidth = 64;
    const int ButtonHeight = 64;

    Button? dragSource;
    readonly DragDropState dragState = new();

    readonly System.Windows.Forms.Timer longPressTimer;
    Button? longPressCandidate;
    ButtonEntry? longPressEntry;
    Point longPressStartPoint;
    readonly LongPressOperationState longPressState = new();
    long longPressOperationId;
    Button? longPressClickSuppressionTarget;

    const int LongPressMilliseconds = 500;

    ButtonLauncherData Data => owner.ButtonLauncherData;

    public ButtonLauncherForm(ApplicationHostForm owner, ContextMenuStrip mainMenu)
    {
        InitializeComponent();
        this.owner = owner;
        this.mainMenu = mainMenu;

        Text = Infrastructure.AppVersion.Title;

        buttonContextMenu = new ContextMenuStrip();
        buttonContextMenu.Items.Add("実行(&X)", null, ButtonMenu_Execute);
        buttonContextMenu.Items.Add("編集(&E)", null, ButtonMenu_Edit);
        buttonContextMenu.Items.Add("フォルダを開く(&O)", null, ButtonMenu_OpenFolder);
        buttonContextMenu.Items.Add(new ToolStripSeparator());
        buttonContextMenu.Items.Add("コマンドから割り当て(&A)", null, ButtonMenu_AssignFromCommand);
        buttonContextMenu.Items.Add(new ToolStripSeparator());
        buttonContextMenu.Items.Add("削除(&D)", null, ButtonMenu_Delete);
        buttonContextMenu.Opening += ButtonContextMenu_Opening;

        toolStrip1.ContextMenuStrip = mainMenu;

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
            var pos = tabControl1.PointToClient(Cursor.Position);
            int hitIndex = TabControlHelper.HitTest(tabControl1, pos);
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

        tabControl1.ContextMenuStrip = tabContextMenu;

        tabControl1.AllowDrop = true;
        tabControl1.DragOver += TabControl1_DragOver;
        tabControl1.DragDrop += TabControl1_DragDrop;

        iconLoader.IconLoaded += IconLoader_IconLoaded;

        longPressTimer = new System.Windows.Forms.Timer { Interval = LongPressMilliseconds };
        longPressTimer.Tick += LongPressTimer_Tick;

        lockButton.Checked = Data.IsLocked;
        ApplyLockState();

        // オーナー設定 (Show→Hideだと一瞬表示されるのでプロパティで設定)
        Owner = owner;
        // BuildTabs()内のiconLoader.Load()より先にハンドルを作成する
        // Handle未作成時にIconLoadedイベントが到着するとアイコンが破棄されるため
        _ = Handle;
        WindowHelper.DisableCloseButton(this);

        BuildTabs();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.FormOwnerClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    #region タブ・グリッド構築

    private void BuildTabs()
    {
        tabControl1.TabPages.Clear();
        iconLoader.Clear();

        if (Data.Tabs.Count == 0)
        {
            Data.Tabs.Add(new ButtonTab { Name = "Default" });
        }

        foreach (var tab in Data.Tabs)
        {
#pragma warning disable CA2000
            var tabPage = new TabPage(tab.Name) { Tag = tab };
#pragma warning restore CA2000
            BuildGrid(tabPage, tab);
            tabControl1.TabPages.Add(tabPage);
        }

        if (Data.DefaultTabIndex >= 0 && Data.DefaultTabIndex < tabControl1.TabPages.Count)
        {
            tabControl1.SelectedIndex = Data.DefaultTabIndex;
        }
    }

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

        panel.SuspendLayout();

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

    private void UpdateButton(TabPage? tabPage, ButtonTab tabData, int row, int col)
    {
        if (tabPage is null) return;

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

#pragma warning disable CA1852
    private record ButtonPosition(int Row, int Col);
#pragma warning restore CA1852

    #endregion

    #region 表示・非表示

    public void ShowLauncher()
    {
        try
        {
            ClientSize = ButtonLauncherPresenter.CalculateWindowSize(
                Data.Columns, Data.Rows,
                ButtonWidth, ButtonHeight,
                toolStrip1.Height, tabControl1.ItemSize.Height);

            FormsHelper.CenterOnCursor(this);

            if (Data.DefaultTabIndex >= 0 && Data.DefaultTabIndex < tabControl1.TabPages.Count)
            {
                tabControl1.SelectedIndex = Data.DefaultTabIndex;
            }

            Hide();
            WindowHelper.ActivateForce(this);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            ErrorReporter.Instance.OnException(ex);
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
        if (ReferenceEquals(sender, longPressClickSuppressionTarget) &&
            longPressState.ConsumeClickSuppression())
        {
            longPressClickSuppressionTarget = null;
            return;
        }

        var btn = (Button)sender!;
        var pos = (ButtonPosition)btn.Tag!;
        var tabData = GetCurrentTabData();
        if (tabData is null) return;

        var entry = tabData.GetButton(pos.Row, pos.Col);
        if (entry is null || entry.IsEmpty) return;

        Command command = entry.Clone();
        var config = owner.Config.Clone();
        IntPtr windowHandle = Handle;
        ShellLaunchService.Start(this,
            () => command.Execute("", config, windowHandle, NativeMethods.IsUserAnAdmin()),
            () =>
            {
                if (!Data.IsLocked) Hide();
            });
    }

    private void LongPressTimer_Tick(object? sender, EventArgs e)
    {
        longPressTimer.Stop();
        if (longPressCandidate is null || longPressEntry is null) return;

        if ((MouseButtons & MouseButtons.Left) != MouseButtons.Left) return;
        if (!longPressCandidate.ClientRectangle.Contains(
                longPressCandidate.PointToClient(Cursor.Position))) return;

        Button candidate = longPressCandidate;
        string folderPath = longPressEntry.FileName;
        long operationId = longPressOperationId;
        StaThreadRunner.Start(() =>
        {
            bool isDirectory = Directory.Exists(folderPath);
            UiThreadDispatcher.SafeBeginInvoke(this, () =>
            {
                if (!ReferenceEquals(longPressCandidate, candidate) || candidate.IsDisposed) return;
                if (!isDirectory) return;
                if ((MouseButtons & MouseButtons.Left) != MouseButtons.Left ||
                    !candidate.ClientRectangle.Contains(candidate.PointToClient(Cursor.Position)))
                {
                    return;
                }
                if (!longPressState.TryFire(operationId)) return;
                longPressClickSuppressionTarget = candidate;

#pragma warning disable CA2000
                var builder = new FolderPopupMenuBuilder(Handle, this);
                var menu = builder.Build(folderPath);
#pragma warning restore CA2000
                var pos = (ButtonPosition)candidate.Tag!;
                DiagnosticLog.Info("Button.FolderPopup", $"row={pos.Row} col={pos.Col}");
                menu.Show(candidate, candidate.PointToClient(Cursor.Position));
            });
        });
    }

    #endregion

    #region 右クリックメニュー

    private Button? contextMenuTarget;

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
        buttonContextMenu.Items[0].Enabled = hasCommand;
        buttonContextMenu.Items[1].Enabled = true;
        buttonContextMenu.Items[2].Enabled = hasCommand;
        buttonContextMenu.Items[6].Enabled = hasCommand;
    }

    private void GridButton_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var btn = (Button)sender!;
        var pos = (ButtonPosition)btn.Tag!;
        var tabData = GetCurrentTabData();
        var entry = tabData?.GetButton(pos.Row, pos.Col);
        if (entry is null || entry.IsEmpty) return;

        if (Data.IsLocked)
        {
            dragSource = btn;
            dragState.Start(entry, tabData!, e.Location);
        }
        else
        {
            longPressCandidate = btn;
            longPressEntry = entry;
            longPressStartPoint = e.Location;
            longPressOperationId = longPressState.Begin();
            longPressClickSuppressionTarget = null;
            longPressTimer.Start();
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
            entry = new ButtonEntry { Row = pos.Row, Col = pos.Col };
        }

        using var form = new EditCommandForm(entry!);
        if (form.ShowDialogOver(this) == DialogResult.OK)
        {
            if (isNew)
            {
                if (!entry!.IsEmpty)
                {
                    tabData.SetButton(pos.Row, pos.Col, entry);
                }
            }
            contextMenuTarget!.Text = entry!.Name ?? "";
            iconLoader.Load(entry.FileName, false, contextMenuTarget);
            DiagnosticLog.Info("Button.Edit", $"row={pos.Row} col={pos.Col} isNew={isNew}");
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

        ShellLaunchService.OpenDirectory(this, entry, owner.Config);
    }

    private void ButtonMenu_AssignFromCommand(object? sender, EventArgs e)
    {
        if (contextMenuTarget is null) return;
        var pos = (ButtonPosition)contextMenuTarget.Tag!;
        var tabData = GetCurrentTabData();
        if (tabData is null) return;

        using var dlg = new CommandSelectDialog(owner.CommandList);
        if (dlg.ShowDialogOver(this) == DialogResult.OK && dlg.SelectedCommand is not null)
        {
            var newEntry = ButtonEntry.FromCommand(dlg.SelectedCommand, pos.Row, pos.Col);
            tabData.SetButton(pos.Row, pos.Col, newEntry);
            contextMenuTarget.Text = newEntry.Name ?? "";
            iconLoader.Load(newEntry.FileName, false, contextMenuTarget);
            DiagnosticLog.Info("Button.AssignFromCommand", $"row={pos.Row} col={pos.Col}");
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
        DiagnosticLog.Info("Button.Delete", $"row={pos.Row} col={pos.Col}");
        SaveData();
    }

    #endregion

    #region ファイルD&D / ボタンD&D

    private void GridButton_MouseMove(object? sender, MouseEventArgs e)
    {
        if (longPressTimer.Enabled &&
            (Math.Abs(e.Location.X - longPressStartPoint.X) > SystemInformation.DragSize.Width / 2 ||
             Math.Abs(e.Location.Y - longPressStartPoint.Y) > SystemInformation.DragSize.Height / 2))
        {
            longPressTimer.Stop();
            longPressState.Cancel();
            longPressCandidate = null;
            longPressEntry = null;
        }

        if (dragSource is null || !dragState.IsActive) return;
        if (e.Button != MouseButtons.Left) return;

        if (dragState.ShouldBeginDrag(e.Location, SystemInformation.DragSize))
        {
            var btn = (Button)sender!;
            btn.DoDragDrop(dragState.DragEntry!, DragDropEffects.Move);
            dragSource = null;
            dragState.Reset();
        }
    }

    private void GridButton_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            longPressTimer.Stop();
            longPressState.Cancel();
            longPressCandidate = null;
            longPressEntry = null;
        }

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
            string[] files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            if (files.Length > 0)
            {
                var cmd = CommandFactory.FromFile(files[0]);
                var entry = ButtonEntry.FromCommand(cmd, pos.Row, pos.Col);
                destTabData.SetButton(pos.Row, pos.Col, entry);
                btn.Text = entry.Name ?? "";
                iconLoader.Load(entry.FileName, false, btn);
                SaveData();
            }
        }
        else if (dragSource is not null && dragState.IsActive && dragState.SourceTab is not null)
        {
            var srcPos = (ButtonPosition)dragSource.Tag!;
            var srcTabData = dragState.SourceTab;

            ButtonLauncherPresenter.SwapButtons(
                srcTabData, srcPos.Row, srcPos.Col,
                destTabData, pos.Row, pos.Col,
                dragState.DragEntry!);

            if (srcTabData != destTabData)
            {
                UpdateButton(FindTabPage(srcTabData), srcTabData, srcPos.Row, srcPos.Col);
            }
            UpdateButton(tabControl1.SelectedTab, destTabData, pos.Row, pos.Col);
            if (srcTabData == destTabData)
            {
                UpdateButton(tabControl1.SelectedTab, destTabData, srcPos.Row, srcPos.Col);
            }
            SaveData();
        }
    }

    private void TabControl1_DragOver(object? sender, DragEventArgs e)
    {
        if (dragSource is null) { e.Effect = DragDropEffects.None; return; }

        var pt = tabControl1.PointToClient(new Point(e.X, e.Y));
        int hit = TabControlHelper.HitTest(tabControl1, pt);
        if (hit >= 0) tabControl1.SelectedIndex = hit;
        e.Effect = DragDropEffects.Move;
    }

    private void TabControl1_DragDrop(object? sender, DragEventArgs e)
    {
    }

    #endregion

    #region タブ管理

    private void AddTab()
    {
        string? name = InputDialog.Prompt(this, "タブ名を入力してください:", "タブの追加", $"Tab{Data.Tabs.Count + 1}");
        if (name is null) return;

        var tab = new ButtonTab { Name = name };
        Data.Tabs.Add(tab);
        int tabIndex = Data.Tabs.Count - 1;

        var tabPage = new TabPage(name) { Tag = tab };
        BuildGrid(tabPage, tab);
        tabControl1.TabPages.Add(tabPage);
        tabControl1.SelectedTab = tabPage;
        DiagnosticLog.Info("Tab.Add", $"index={tabIndex}");
        SaveData();
    }

    private void RenameTab()
    {
        var tabPage = tabControl1.SelectedTab;
        if (tabPage is null) return;

        var tabData = (ButtonTab)tabPage.Tag!;
        string? name = InputDialog.Prompt(this, "新しいタブ名:", "タブ名の変更", tabData.Name);
        if (name is null) return;

        tabData.Name = name;
        tabPage.Text = name;
        int tabIndex = tabControl1.SelectedIndex;
        DiagnosticLog.Info("Tab.Rename", $"index={tabIndex}");
        SaveData();
    }

    private void SetDefaultTab()
    {
        ButtonLauncherPresenter.SetDefaultTab(Data, tabControl1.SelectedIndex);
        int tabIndex = tabControl1.SelectedIndex;
        DiagnosticLog.Info("Tab.SetDefault", $"index={tabIndex}");
        SaveData();
    }

    private void DeleteTab()
    {
        if (tabControl1.TabPages.Count <= 1)
        {
            MessageBox.Show(this, "最後のタブは削除できません。\n削除するには、タブ見出しの右クリックの「タブを追加」で別のタブを追加してから削除してください。",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        tabControl1.TabPages.Remove(tabPage);

        DiagnosticLog.Info("Tab.Delete", $"index={tabIndex}");
        SaveData();
    }

    private void MoveTab(int fromIndex, int toIndex)
    {
        ButtonLauncherPresenter.MoveTab(Data, fromIndex, toIndex);

        var tabPage = tabControl1.TabPages[fromIndex];
        tabControl1.TabPages.Remove(tabPage);
        tabControl1.TabPages.Insert(toIndex, tabPage);

        tabControl1.SelectedIndex = toIndex;
        DiagnosticLog.Info("Tab.Move", $"from={fromIndex} to={toIndex}");
        SaveData();
    }

    private void tabControl1_MouseWheel(object? sender, MouseEventArgs e)
    {
        TabControlHelper.SelectByWheel(tabControl1, e.Delta);
    }

    #endregion

    #region アイコン読み込み

    private void IconLoader_IconLoaded(object? sender, IconLoadedEventArgs e)
    {
        IconReceiver.Receive(this, iconLoader, e, icon =>
        {
            var btn = e.Arg as Button;
            if (btn is null || btn.IsDisposed) return;
            var previous = btn.Image;
            btn.Image = icon.ToBitmap();
            previous?.Dispose();
            btn.Parent?.Invalidate(true);
        });
    }

    #endregion

    #region ヘルパー

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
        Data.Save(owner.ReportSaveFailure, owner.ConfigurationBaseName);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            iconLoader.Dispose();
            longPressTimer?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
        if (disposing)
        {
            // ボタンとタブコントロールがContextMenuStripとして参照するため、
            // base.Disposeが子の破棄を終えた後に解放する。
            buttonContextMenu?.Dispose();
            tabContextMenu?.Dispose();
        }
    }

    #endregion
}
