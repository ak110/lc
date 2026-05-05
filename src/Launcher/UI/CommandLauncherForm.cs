using System.ComponentModel;
using System.Diagnostics;
using Launcher.Core;
using Launcher.Win32;

namespace Launcher.UI;

public partial class CommandLauncherForm : Form
{
    ApplicationHostForm ownerForm;
    CommandLauncherPresenter presenter;

    InputState state;
    int lastFocus;   // エディットボックスにフォーカスがある場合0、リストビューにある場合1

    bool recurseGuard; //再帰防止

    // ボタン型ランチャーのアイコン読み込みを優先するため低優先度で動作
    AsyncIconLoader iconLoader = new(threadPriority: ThreadPriority.BelowNormal);

    // LocationChanged/SizeChangedの高頻度保存を抑制するデバウンスタイマー
    System.Windows.Forms.Timer saveConfigTimer;

    public CommandLauncherForm(ApplicationHostForm applicationHostForm, ContextMenuStrip mainMenu)
    {
        InitializeComponent();
        ContextMenuStrip = mainMenu;
        Visible = false;

        Text = Infrastructure.AppVersion.Title;

        ownerForm = applicationHostForm;
        Owner = ownerForm;
        presenter = new CommandLauncherPresenter(
            () => ownerForm.CommandList,
            () => ownerForm.Config);

        iconLoader.IconLoaded += iconLoader_IconLoaded;

        // ListViewのちらつき・初回表示の遅延を軽減
        listView1.GetType().GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(listView1, true);

        // LocationChanged/SizeChangedのデバウンス保存タイマー
        saveConfigTimer = new System.Windows.Forms.Timer { Interval = 200 };
        saveConfigTimer.Tick += (s, e) =>
        {
            saveConfigTimer.Stop();
            ownerForm.Config.Serialize();
        };
        components ??= new Container();
        components.Add(saveConfigTimer);

        // ウィンドウの位置・サイズを設定
        Location = ownerForm.Config.WindowPos;
        Size = ownerForm.Config.WindowSize;
    }

    /// <summary>
    /// リストビューとアイコンの事前初期化 (初回Show前に呼ぶ)
    /// </summary>
    public void PreInitialize()
    {
        // ハンドル作成 (アイコン非同期読み込みのInvokeに必要)
        _ = Handle;
        // リストビューの構築とアイコン読み込みを事前実行
        textBox1_TextChanged(this, EventArgs.Empty);
        ApplyConfig();
        // 初回表示時にtextBox1へフォーカスを設定 (Alt+Spaceでシステムメニューが出る問題の対策)
        ActiveControl = textBox1;
        initialized = true;
    }

    private bool initialized;

    private void CommandLauncherForm_Load(object sender, EventArgs e)
    {
        if (initialized) return;
        // PreInitialize未実行時のフォールバック
        textBox1_TextChanged(this, EventArgs.Empty);
        ApplyConfig();
    }

    private void CommandLauncherForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.FormOwnerClosing)
        {
            if (ownerForm.Config.CloseButton == CloseButtonBehavior.Close)
            {
                HideWindow();
                e.Cancel = true;
                return;
            }
            else
            {
                ownerForm.Close();
            }
        }

        // 遅延保存が残っていたら即フラッシュ
        FlushPendingSave();

        iconLoader.IconLoaded -= iconLoader_IconLoaded;
        iconLoader.Dispose();
    }

    /// <summary>
    /// デバウンスタイマーによる遅延保存が残っていたら即座に保存する。
    /// </summary>
    private void FlushPendingSave()
    {
        if (saveConfigTimer.Enabled)
        {
            saveConfigTimer.Stop();
            try
            {
                ownerForm.Config.Serialize();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"Config保存失敗 (終了時): {ex.Message}");
            }
        }
    }

    /// <summary>
    /// コンフィグの反映
    /// </summary>
    public void ApplyConfig()
    {
        Size s = ownerForm.Config.WindowSize;
        FormBorderStyle = ownerForm.Config.WindowNoResize ?
            FormBorderStyle.FixedDialog : FormBorderStyle.Sizable;
        Size = ownerForm.Config.WindowSize = s; // FormBorderStyle変更後にサイズがずれるため再設定する
        TopMost = ownerForm.Config.WindowTopMost;
        if (ownerForm.Config.CloseButton == CloseButtonBehavior.Disabled)
        {
            WindowHelper.DisableCloseButton(this);
        }
        else
        {
            WindowHelper.EnableCloseButton(this);
        }

        // アイコンの再読込
        ReloadIcons();

        // ReplaceEnv
        Thread thread = new Thread(() =>
        {
            new ReplaceEnvList(ownerForm.Config.ReplaceEnv).Replace(ownerForm.CommandList);
        });
        thread.IsBackground = true;
        thread.Priority = ThreadPriority.Lowest;
        thread.Start();
    }

    /// <summary>
    /// コマンド一覧の表示だけを更新 (アイコン再読込なし)
    /// </summary>
    public void RefreshCommandList()
    {
        textBox1_TextChanged(this, EventArgs.Empty);
    }

    private void ReloadIcons()
    {
        iconLoader.Clear();
        foreach (ListViewItem item in listView1.Items)
        {
            item.ImageIndex = -1;
        }
        imageList1.Images.Clear();
        bool largeIcon = ownerForm.Config.LargeIcon;
        imageList1.ImageSize = largeIcon ? new Size(32, 32) : new Size(16, 16);
        foreach (Command command in ownerForm.CommandList.Commands)
        {
            iconLoader.Load(command.FileName, !largeIcon, command);
        }
    }

    #region show/hide

    public void ShowWindow()
    {
        Location = ownerForm.Config.WindowPos;
        WindowHelper.ActivateForce(this);
    }

    public void HideWindow()
    {
        Hide();
        textBox1.Clear();
    }

    #endregion

    #region CommandLauncherForm

    /// <summary>
    /// Locationが変わった
    /// </summary>
    private void CommandLauncherForm_LocationChanged(object sender, EventArgs e)
    {
        if (Visible && WindowState == FormWindowState.Normal)
        {
            ownerForm.Config.WindowPos = Location;
            saveConfigTimer.Stop();
            saveConfigTimer.Start();
        }
    }

    /// <summary>
    /// Sizeが変わった
    /// </summary>
    private void CommandLauncherForm_SizeChanged(object sender, EventArgs e)
    {
        if (Visible && WindowState == FormWindowState.Normal)
        {
            ownerForm.Config.WindowSize = Size;
            saveConfigTimer.Stop();
            saveConfigTimer.Start();
        }
    }

    /// <summary>
    /// アクティブになった
    /// </summary>
    private void CommandLauncherForm_Activated(object sender, EventArgs e)
    {
        BringToFront();
        Activate();
        textBox1.Focus();
    }

    /// <summary>
    /// 非アクティブになった
    /// </summary>
    private void CommandLauncherForm_Deactivate(object sender, EventArgs e)
    {
        if (ownerForm.Config.WindowHideNoActive)
        {
            // 子フォームも追跡中の非同期通知も無い場合のみ隠す。
            // 通知表示中に隠すと、ShowHideで表示→通知をActivate→Deactivateで即再度隠れる問題を防ぐ。
            if (OwnedForms.Length <= 0 && !ownerForm.HasActiveNotifications)
            {
                HideWindow();
            }
        }
    }

    /// <summary>
    /// 非アクティブになった
    /// </summary>
    private void CommandLauncherForm_Leave(object sender, EventArgs e)
    {
        if (ownerForm.Config.WindowHideNoActive)
        {
            if (OwnedForms.Length <= 0 && !ownerForm.HasActiveNotifications)
            {
                HideWindow();
            }
        }
    }

    #endregion

    #region textBox1

    /// <summary>
    /// エディットボックスがフォーカスを受け取った
    /// </summary>
    private void textBox1_Enter(object sender, EventArgs e)
    {
        lastFocus = 0;
    }

    /// <summary>
    /// テキストが入力された
    /// </summary>
    private void textBox1_TextChanged(object sender, EventArgs e)
    {
        if (recurseGuard) return;
        recurseGuard = true;
        SuspendLayout();
        listView1.BeginUpdate();
        try
        {
            InnerTextChanged();
        }
        finally
        {
            listView1.EndUpdate();
            ResumeLayout();
            recurseGuard = false;
        }
    }

    /// <summary>
    /// キー押された
    /// </summary>
    private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == '\b')
        { // バックスペース押された
            int n1 = textBox1.SelectionStart;
            int n2 = n1 + textBox1.SelectionLength;
            if (textBox1.Text.Length == n2)
            {
                // 選択部分（補完候補）を削除し、残りの処理はデフォルト動作に委ねる
                System.Diagnostics.Debug.Assert(!recurseGuard);
                recurseGuard = true;
                try
                {
                    textBox1.SelectedText = "";
                }
                finally
                {
                    recurseGuard = false;
                }
            }
        }
    }

    /// <summary>
    /// キー押し下げられた
    /// </summary>
    private void textBox1_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up)
        { // ↑キー
            if (0 < listView1.Items.Count)
            {
                SetListViewSelection(listView1.Items.Count - 1);
                listView1.Select();
                e.Handled = true; // 処理した
            }
        }
        else if (e.KeyCode == Keys.Down)
        { // ↓キー
            if (0 < listView1.Items.Count)
            {
                SetListViewSelection(0);
                listView1.Select();
                e.Handled = true; // 処理した
            }
        }
        else if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.Control)
        {
            // Ctrl+EnterはKeyPressが改行入力として割り込むためbutton1クリックにならない。KeyDownで処理する
            e.Handled = true;
            button1_Click(this, EventArgs.Empty);
        }
        else
        {
            UpdateButtonText();
        }
    }

    /// <summary>
    /// キー離された
    /// </summary>
    private void textBox1_KeyUp(object sender, KeyEventArgs e)
    {
        UpdateButtonText();
    }

    /// <summary>
    /// 選択部分(補完部分)を除いたテキストの取得。
    /// </summary>
    string GetInputText()
    {
        int n1 = textBox1.SelectionStart;
        int n2 = n1 + textBox1.SelectionLength;
        string text = textBox1.Text;
        if (n1 != n2 && n2 == text.Length)
        {
            return text.Substring(0, n1);
        }
        return text;
    }

    #endregion

    /// <summary>
    /// ListViewを一番上選択状態にしてテキストボックスにフォーカスを設定
    /// </summary>
    private void ActivateTextBox()
    {
        SetListViewSelection(0);
        textBox1.Select();
        textBox1.Focus();
    }

    #region listView1

    /// <summary>
    /// リストビューがフォーカスを受け取った
    /// </summary>
    private void listView1_Enter(object sender, EventArgs e)
    {
        lastFocus = 1;
    }

    /// <summary>
    /// ダブルクリック
    /// </summary>
    private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        switch (ownerForm.Config.ItemDoubleClick)
        {
            case ItemAction.Execute: 実行RToolStripMenuItem_Click(this, EventArgs.Empty); break;
            case ItemAction.EditConfig: フォルダを開くFToolStripMenuItem_Click(this, EventArgs.Empty); break;
            case ItemAction.OpenDirectory: 設定CToolStripMenuItem1_Click(this, EventArgs.Empty); break;
            case ItemAction.None: 削除DToolStripMenuItem_Click(this, EventArgs.Empty); break;
        }
    }

    /// <summary>
    /// キー
    /// </summary>
    private void listView1_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up)
        { // ↑
            if (listView1.SelectedIndices.Count == 1 &&
                listView1.SelectedIndices[0] == 0)
            {
                // リストビュー一番上
                ActivateTextBox();
                e.Handled = true;
            }
            UpdateButtonText();
        }
        else if (e.KeyCode == Keys.Down)
        { // ↓
            if (listView1.SelectedIndices.Count == 1 &&
                listView1.SelectedIndices[0] == listView1.Items.Count - 1)
            {
                // リストビュー一番下
                listView1.EnsureVisible(0);
                ActivateTextBox();
                e.Handled = true;
            }
            UpdateButtonText();
        }
        else if (e.KeyCode == Keys.Home)
        { // Home
            ActivateTextBox();
            e.Handled = true;
            UpdateButtonText();
        }
        else if (e.KeyCode == Keys.Apps)
        { // メニュー
            listView1.ContextMenuStrip!.Show(listView1, new Point());
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.Control)
        {
            // Ctrl+EnterはKeyPressが改行入力として割り込むためbutton1クリックにならない。KeyDownで処理する
            e.Handled = true;
            button1_Click(this, EventArgs.Empty);
        }
        else
        {
            UpdateButtonText();
        }
    }

    /// <summary>
    /// キー離された
    /// </summary>
    private void listView1_KeyUp(object sender, KeyEventArgs e)
    {
        UpdateButtonText();
    }

    /// <summary>
    /// リストビューの指定した項目を選択してスクロール位置を合わせる。
    /// </summary>
    private void SetListViewSelection(int index)
    {
        if (0 <= index && index < listView1.Items.Count)
        {
            listView1.SelectedIndices.Clear();
            listView1.SelectedIndices.Add(index);
            listView1.Items[index].Focused = true;
            listView1.EnsureVisible(index);
        }
    }

    #endregion

    #region リストビューの右クリックメニュー

    private void 実行RToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            Command command = (Command)listView1.SelectedItems[0].Tag!;
            ActivateTextBox();
            ExecuteCommand(command, "");
        }
    }

    private void フォルダを開くFToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            Command command = (Command)listView1.SelectedItems[0].Tag!;
            ActivateTextBox();
            OpenDirectory(command);
        }
    }

    private void 設定CToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            Command command = (Command)listView1.SelectedItems[0].Tag!;
            ActivateTextBox();
            using var form = new EditCommandForm(command);
            if (form.ShowDialogOver(this) == DialogResult.OK)
            {
                ownerForm.CommandList.Serialize(".cmd.cfg");
                ApplyConfig();
                textBox1.Clear(); // 入力欄をクリアする。
            }
        }
    }

    private void 削除DToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            var removeItem = listView1.SelectedItems[0];
            var command = (Command)removeItem.Tag!;
            ActivateTextBox();
            if (MessageBox.Show(this, "コマンド " + command.Name + " を削除します。", "確認",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                ownerForm.CommandList.Commands.Remove(command);
                ownerForm.CommandList.Serialize(".cmd.cfg");
                listView1.Items.Remove(removeItem);
                textBox1.Clear();
            }
        }
    }

    private void 複製の作成LToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            Command command = ((Command)listView1.SelectedItems[0].Tag!).Clone();
            ActivateTextBox();
            using var form = new EditCommandForm(command);
            form.Text += " (複製の作成)";
            if (form.ShowDialogOver(this) == DialogResult.OK)
            {
                new ReplaceEnvList(ownerForm.Config.ReplaceEnv).Replace(command);
                ownerForm.CommandList.Add(command);
                ownerForm.CommandList.Serialize(".cmd.cfg");
                ApplyConfig();
                textBox1_TextChanged(this, EventArgs.Empty);
            }
        }
    }

    #endregion

    #region iconLoader

    /// <summary>
    /// アイコン読み込まれたイベント。
    /// </summary>
    void iconLoader_IconLoaded(object? sender, IconLoadedEventArgs e)
    {
        // Invoke()にはハンドルが必要 (CreatedはShow()まで立たないのでIsHandleCreatedで判定)
        if (!IsHandleCreated || IsDisposed)
        {
            e.Icon?.Dispose();
            return;
        }
        try
        {
            Command command = (Command)e.Arg!;
            Invoke(new MethodInvoker(delegate ()
            {
                try
                {
                    // 世代が古い結果は破棄 (Clear()後の古いリクエスト結果を無視)
                    if (e.Generation != iconLoader.Generation) return;

                    if (e.Icon is not null)
                    {
                        imageList1.Images.Add(command.FileName, (System.Drawing.Icon)e.Icon.Clone()!);
                    }
                    command.IconIndex = imageList1.Images.IndexOfKey(command.FileName);
                    // リストビューに存在する場合はアイコンを設定する
                    foreach (ListViewItem item in listView1.Items)
                    {
                        if (command.Equals(item.Tag))
                        {
                            item.ImageIndex = command.IconIndex;
                            listView1.RedrawItems(item.Index, item.Index, true);
                            break;
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    e.Icon?.Dispose();
                }
            }));
        }
        catch (InvalidOperationException ex)
        {
            // Invoke失敗 (フォームが破棄済み等。ObjectDisposedExceptionも含む)
            e.Icon?.Dispose();
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    #endregion

    /// <summary>
    /// textBox1_TextChanged
    /// </summary>
    private void InnerTextChanged()
    {
        var result = presenter.ProcessTextChange(GetInputText());
        state = result.State;

        // 補完処理 (テキストボックスへの反映はUI操作のためForm側で行う)
        if (result.CompletionText is not null)
        {
            textBox1.Text = result.CompletionText;
            textBox1.Select(result.SelectionStart, result.SelectionLength);
        }

        // コマンドをリストビューへ (AddRangeでまとめて追加して描画コストを削減)
        listView1.Items.Clear();
        var items = new ListViewItem[result.MatchedCommands.Count];
        int idx = 0;
        foreach (var command in result.MatchedCommands)
        {
            ListViewItem item = new ListViewItem(command.Name);
            item.SubItems.Add(command.FileName + " " + command.Param);
            if (0 <= command.IconIndex)
            {
                item.ImageIndex = command.IconIndex;
            }
            item.Tag = command;
            items[idx++] = item;
        }
        listView1.Items.AddRange(items);

        UpdateButtonText();
    }

    /// <summary>
    /// ボタンのテキストの更新
    /// </summary>
    private void UpdateButtonText()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(state, lastFocus, ModifierKeys);
        button1.Text = texts.Button1Text;
        button2.Text = texts.Button2Text;
    }

    /// <summary>
    /// 実行・設定ボタン
    /// </summary>
    private void button1_Click(object sender, EventArgs e)
    {
        // フォーカス位置に応じたコマンド取得
        Command? firstCommand = listView1.Items.Count > 0
            ? (Command)listView1.Items[0].Tag! : null;
        Command? selectedCommand = listView1.SelectedItems.Count > 0
            ? (Command)listView1.SelectedItems[0].Tag! : null;

        var result = CommandLauncherPresenter.DetermineAction(
            state, lastFocus, firstCommand, selectedCommand, ModifierKeys);

        switch (result.Action)
        {
            case MainAction.ShowConfig:
                ownerForm.ShowConfigDialog();
                break;

            case MainAction.AddCommand:
                {
                    var command = new Command();
                    command.Name = GetInputText();
                    using EditCommandForm form = new EditCommandForm(command);
                    if (form.ShowDialogOver(this) == DialogResult.OK)
                    {
                        new ReplaceEnvList(ownerForm.Config.ReplaceEnv).Replace(command);
                        ownerForm.CommandList.Add(command);
                        ownerForm.CommandList.Serialize(".cmd.cfg");
                        ApplyConfig();
                        textBox1.Clear();
                    }
                }
                break;

            case MainAction.EditCommand:
                {
                    using EditCommandForm form = new EditCommandForm(result.TargetCommand!);
                    if (form.ShowDialogOver(this) == DialogResult.OK)
                    {
                        new ReplaceEnvList(ownerForm.Config.ReplaceEnv).Replace(result.TargetCommand!);
                        ownerForm.CommandList.Serialize(".cmd.cfg");
                        ApplyConfig();
                        textBox1.Clear();
                    }
                }
                break;

            case MainAction.OpenDirectory:
                OpenDirectory(result.TargetCommand!);
                if (ownerForm.Config.HideOnRun)
                {
                    HideWindow();
                }
                break;

            case MainAction.Execute:
                ExecuteCommand(result.TargetCommand!, textBox1.Text);
                if (ownerForm.Config.HideOnRun)
                {
                    HideWindow();
                }
                break;
        }
    }

    /// <summary>
    /// Command.OpenDirectory
    /// </summary>
    private void OpenDirectory(Command command)
    {
        Thread thread = new Thread(OpenDirectoryThread);
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(command);
    }
    private void OpenDirectoryThread(object? arg)
    {
        Command cmd = (Command)arg!;
        cmd.OpenDirectory(ownerForm.Config);
    }

    sealed class ExecuteParams
    {
        public Command Command;
        public string Input;
        public IntPtr Handle;
        public ExecuteParams(Command command, string text, IntPtr handle)
        {
            Command = command;
            Input = text;
            Handle = handle;
        }
    }
    /// <summary>
    /// コマンドを実行する。
    /// </summary>
    private void ExecuteCommand(Command command, string input)
    {
#if DEBUG
        ExecuteThread(new ExecuteParams(command, input, Handle));
#else
        Thread thread = new Thread(ExecuteThread);
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(new ExecuteParams(command, input, Handle));
#endif
    }

    /// <summary>
    /// ThreadPool上で実行される処理。
    /// </summary>
    private void ExecuteThread(object? args)
    {
        try
        {
            ExecuteParams ep = (ExecuteParams)args!;
            ep.Command.Execute(ep.Input,
                ownerForm.Config, ep.Handle);
        }
        catch (Win32Exception e) when (e.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED: ユーザーが UAC ダイアログ等をキャンセルした場合は無視する
        }
        catch (Win32Exception e)
        {
            ErrorMessageBox(e);
        }
        catch (IOException e)
        {
            ErrorMessageBox(e);
        }
        catch (InvalidOperationException e)
        {
            ErrorMessageBox(e);
        }
    }

    /// <summary>
    /// エラーメッセージボックスの表示
    /// </summary>
    private void ErrorMessageBox(Exception e)
    {
        try
        {
            if (IsDisposed) return;
            Invoke(new MethodInvoker(delegate ()
            {
                try
                {
                    string msg = ownerForm.Config.Debug ? e.ToString() : e.Message;
                    MessageBox.Show(this, msg, "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (InvalidOperationException)
                {
                    // フォーム破棄済み等で表示不可 (ObjectDisposedExceptionも含む)
                }
            }));
        }
        catch (InvalidOperationException)
        {
            // Invoke失敗 (ObjectDisposedExceptionも含む)
        }
    }

    /// <summary>
    /// キャンセルボタン
    /// </summary>
    private void button2_Click(object sender, EventArgs e)
    {
        if (state == InputState.Empty)
        {
            HideWindow();
        }
        else
        {
            textBox1.Clear();
        }
    }
}
