#nullable disable
using System.ComponentModel;
using Launcher.Core;
using Launcher.Win32;

namespace Launcher.UI;

public partial class MainForm : Form
{
    DummyForm ownerForm;

    int state; // 0:空っぽ  1:該当コマンド無し  2:部分一致のみ有り  3:前方一致有り
    int lastFocus;   // エディットボックスにフォーカスがあった場合0, リストな場合1

    bool recurseGuard = false; //再帰防止

    // ボタン型ランチャーのアイコン読み込みを優先するため低優先度で動作
    AsyncIconLoader iconLoader =
        new AsyncIconLoader() { ThreadPriority = ThreadPriority.BelowNormal };

    public MainForm(DummyForm dummyForm, ContextMenuStrip mainMenu)
    {
        InitializeComponent();
        ContextMenuStrip = mainMenu;
        Visible = false;

        Text = Infrastructure.AppVersion.Title;

        ownerForm = dummyForm;
        Owner = ownerForm;

        iconLoader.IconLoaded += new EventHandler<IconLoadedEventArgs>(iconLoader_IconLoaded);

        // ListViewのちらつき・初回表示の遅延を軽減
        listView1.GetType().GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(listView1, true);

        // ウィンドウスタイルとか
        Location = ownerForm.Config.WindowPos;
        Size = ownerForm.Config.WindowSize;
    }

    /// <summary>
    /// リストビューとアイコンの事前初期化（初回Show前に呼ぶ）
    /// </summary>
    public void PreInitialize()
    {
        // ハンドル作成（アイコン非同期読み込みのBeginInvokeに必要）
        _ = Handle;
        // リストビューの構築とアイコン読み込みを事前実行
        textBox1_TextChanged(this, null);
        ApplyConfig();
        initialized = true;
    }

    private bool initialized = false;

    private void MainForm_Load(object sender, EventArgs e)
    {
        if (initialized) return;
        // PreInitialize未実行時のフォールバック
        textBox1_TextChanged(this, null);
        ApplyConfig();
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
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

        iconLoader.Clear();
    }

    /// <summary>
    /// コンフィグの反映
    /// </summary>
    public void ApplyConfig()
    {
        Size s = ownerForm.Config.WindowSize;
        FormBorderStyle = ownerForm.Config.WindowNoResize ?
            FormBorderStyle.FixedDialog : FormBorderStyle.Sizable;
        Size = ownerForm.Config.WindowSize = s; // ずれるので再設定
        TopMost = ownerForm.Config.WindowTopMost;
        if (ownerForm.Config.CloseButton == CloseButtonBehavior.Disabled)
        {
            FormsHelper.DisableCloseButton(this);
        }
        else
        {
            FormsHelper.EnableCloseButton(this);
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
        FormsHelper.ActivateForce(this);
    }

    public void HideWindow()
    {
        Hide();
        textBox1.Clear();
    }

    #endregion

    #region MainForm

    /// <summary>
    /// Locationが変わった
    /// </summary>
    private void MainForm_LocationChanged(object sender, EventArgs e)
    {
        if (Visible && WindowState == FormWindowState.Normal)
        {
            ownerForm.Config.WindowPos = Location;
            ownerForm.Config.Serialize();
        }
    }

    /// <summary>
    /// Sizeが変わった
    /// </summary>
    private void MainForm_SizeChanged(object sender, EventArgs e)
    {
        if (Visible && WindowState == FormWindowState.Normal)
        {
            ownerForm.Config.WindowSize = Size;
            ownerForm.Config.Serialize();
        }
    }

    /// <summary>
    /// アクティブになった
    /// </summary>
    private void MainForm_Activated(object sender, EventArgs e)
    {
        BringToFront();
        Activate();
        textBox1.Focus();
    }

    /// <summary>
    /// 非アクティブになった
    /// </summary>
    private void MainForm_Deactivate(object sender, EventArgs e)
    {
        if (ownerForm.Config.WindowHideNoActive)
        {
            if (OwnedForms.Length <= 0)
            { // 子も無い場合。
                HideWindow();
            }
        }
    }

    /// <summary>
    /// 非アクティブになった
    /// </summary>
    private void MainForm_Leave(object sender, EventArgs e)
    {
        if (ownerForm.Config.WindowHideNoActive)
        {
            if (OwnedForms.Length <= 0)
            { // 子も無い場合。
                HideWindow();
            }
        }
    }

    #endregion

    #region textBox1

    /// <summary>
    /// エディットボックスにフォーカス来た
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
                // 選択されてるとこを削除して、あとはデフォルトの処理に任せる
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
            // Ctrl+Enterは何故か(改行入力がらみ？)button1にならないので。。
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
    /// リストビューにフォーカス来た
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
            case ItemAction.Execute: 実行RToolStripMenuItem_Click(this, null); break;
            case ItemAction.EditConfig: フォルダを開くFToolStripMenuItem_Click(this, null); break;
            case ItemAction.OpenDirectory: 設定CToolStripMenuItem1_Click(this, null); break;
            case ItemAction.None: 削除DToolStripMenuItem_Click(this, null); break;
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
            listView1.ContextMenuStrip.Show(listView1, new Point());
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.Control)
        {
            // Ctrl+Enterは何故か(改行入力がらみ？)button1にならないので。。
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
    /// リストビューの1つの項目を選択状態にして、ついでにスクロールする。
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
            Command command = (Command)listView1.SelectedItems[0].Tag;
            ActivateTextBox();
            ExecuteCommand(command, "");
        }
    }

    private void フォルダを開くFToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            Command command = (Command)listView1.SelectedItems[0].Tag;
            ActivateTextBox();
            OpenDirectory(command);
        }
    }

    private void 設定CToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            Command command = (Command)listView1.SelectedItems[0].Tag;
            ActivateTextBox();
            using (EditCommandForm form = new EditCommandForm(command))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    ownerForm.CommandList.Serialize(".cmd.cfg");
                    ApplyConfig();
                    textBox1.Clear(); // 消しちゃう
                }
            }
        }
    }

    private void 削除DToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            var removeItem = listView1.SelectedItems[0];
            var command = (Command)removeItem.Tag;
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
            Command command = ((Command)listView1.SelectedItems[0].Tag).Clone();
            ActivateTextBox();
            using (EditCommandForm form = new EditCommandForm(command))
            {
                form.Text += " (複製の作成)";
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    new ReplaceEnvList(ownerForm.Config.ReplaceEnv).Replace(command);
                    ownerForm.CommandList.Add(command);
                    ownerForm.CommandList.Serialize(".cmd.cfg");
                    ApplyConfig();
                    textBox1_TextChanged(this, EventArgs.Empty);
                }
            }
        }
    }

    #endregion

    #region iconLoader

    /// <summary>
    /// アイコン読み込まれたイベント。
    /// </summary>
    void iconLoader_IconLoaded(object sender, IconLoadedEventArgs e)
    {
        if (!Created || IsDisposed) return;
        try
        {
            Command command = (Command)e.Arg;
            Invoke(new MethodInvoker(delegate ()
            {
                try
                {
                    // 世代が古い結果は破棄（Clear()後の古いリクエスト結果を無視）
                    if (e.Generation != iconLoader.Generation) return;

                    if (e.Icon != null)
                    {
                        imageList1.Images.Add(command.FileName, (System.Drawing.Icon)e.Icon.Clone());
                    }
                    command.IconIndex = imageList1.Images.IndexOfKey(command.FileName);
                    // リストビューに存在してたらセットしとく
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    #endregion

    /// <summary>
    /// textBox1_TextChanged
    /// </summary>
    private void InnerTextChanged()
    {
        string input = GetInputText();
        var commands = ownerForm.CommandList.FindMatch(input, ownerForm.Config);

        if (commands.Any())
        {
            var firstCommand = commands.First();
            // 該当するコマンドが1個以上あった
            if (string.IsNullOrEmpty(input))
            {
                state = 0; // state:入力空
            }
            else if (input.Length <= firstCommand.Name.Length &&
                string.Compare(input, 0, firstCommand.Name, 0,
                input.Length, ownerForm.Config.CommandIgnoreCase) == 0)
            {
                // 補完処理。
                if (input.Length < firstCommand.Name.Length)
                {
                    textBox1.Text = string.Concat(input, firstCommand.Name.AsSpan(input.Length));
                    textBox1.Select(input.Length, textBox1.Text.Length - input.Length);
                }
                state = 3; // state:前方一致アリ
            }
            else
            {
                state = 2; // state:部分一致のみ？
            }
        }
        else
        {
            // 該当するコマンドが1個も無い
            state = 1; // state:該当コマンド無し
        }

        // コマンドをリストビューへ（AddRangeでまとめて追加して描画コストを削減）
        listView1.Items.Clear();
        var items = new ListViewItem[commands.Count()];
        int idx = 0;
        foreach (var command in commands)
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
        int s = state;
        if (lastFocus == 1)
        {
            s = -1;
        }
        // OKボタン
        switch (s)
        {
            case 0: button1.Text = "設定"; break;
            case 1: button1.Text = "追加"; break;
            default:
                switch (ModifierKeys)
                {
                    case Keys.Control: button1.Text = "ｺﾏﾝﾄﾞ"; break;
                    case Keys.Shift: button1.Text = "ﾌｫﾙﾀﾞ"; break;
                    default: button1.Text = "実行"; break;
                }
                break;
        }
        // キャンセルボタン
        if (state == 0)
        { // テキストが空
            button2.Text = "隠す";
        }
        else
        { // それ以外
            button2.Text = "消す";
        }
    }

    /// <summary>
    /// 実行・設定ボタン
    /// </summary>
    private void button1_Click(object sender, EventArgs e)
    {
        Command command = null;
        if (lastFocus == 0)
        {
            // 通常のエディットボックスからの実行
            if (0 < listView1.Items.Count)
            {
                command = (Command)listView1.Items[0].Tag;
            }
        }
        else
        { // if (lastFocus == 1)
            // リストビューからの実行
            if (0 < listView1.SelectedItems.Count)
            {
                command = (Command)listView1.SelectedItems[0].Tag;
            }
        }
        if (state == 0 && lastFocus == 0)
        {
            // 設定ダイアログ
            ownerForm.ShowConfigDialog();
        }
        else if (command == null)
        {
            System.Diagnostics.Debug.Assert(state == 1);
            // 追加
            command = new Command();
            command.Name = GetInputText();
            using (EditCommandForm form = new EditCommandForm(command))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    new ReplaceEnvList(ownerForm.Config.ReplaceEnv).Replace(command);
                    ownerForm.CommandList.Add(command);
                    ownerForm.CommandList.Serialize(".cmd.cfg");
                    ApplyConfig();
                    textBox1.Clear(); // 消しちゃう
                }
            }
        }
        else
        {
            // 実行・設定・フォルダ開く
            switch (ModifierKeys)
            {
                case Keys.Control:
                    // 設定
                    using (EditCommandForm form = new EditCommandForm(command))
                    {
                        if (form.ShowDialog(this) == DialogResult.OK)
                        {
                            new ReplaceEnvList(ownerForm.Config.ReplaceEnv).Replace(command);
                            ownerForm.CommandList.Serialize(".cmd.cfg");
                            ApplyConfig();
                            textBox1.Clear(); // 消しちゃう
                        }
                    }
                    break;

                case Keys.Shift:
                    // フォルダ開く

                    OpenDirectory(command);
                    if (ownerForm.Config.HideOnRun)
                    {
                        HideWindow();
                    }
                    break;

                default:
                    // 実行
                    ExecuteCommand(command, textBox1.Text);
                    if (ownerForm.Config.HideOnRun)
                    {
                        HideWindow();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Command.OpenDirectory
    /// </summary>
    private void OpenDirectory(Command command)
    {
        Thread thread = new Thread(
            new ParameterizedThreadStart(OpenDirectoryThread));
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(command);
    }
    private void OpenDirectoryThread(object arg)
    {
        Command cmd = (Command)arg;
        cmd.OpenDirectory(ownerForm.Config);
    }

    class ExecuteParams
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
    /// コマンドの実行を行う
    /// </summary>
    private void ExecuteCommand(Command command, string input)
    {
#if DEBUG
        ExecuteThread(new ExecuteParams(command, input, Handle));
#else
        Thread thread = new Thread(
            new ParameterizedThreadStart(ExecuteThread));
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(new ExecuteParams(command, input, Handle));
#endif
    }

    /// <summary>
    /// ThreadPool上で実行される処理。
    /// </summary>
    private void ExecuteThread(object args)
    {
        try
        {
            ExecuteParams ep = (ExecuteParams)args;
            ep.Command.Execute(ep.Input,
                ownerForm.Config, ep.Handle);
        }
        catch (Win32Exception e)
        {
            System.Diagnostics.Debug.Fail(e.ToString()); // 邪魔なので黙殺。オプションで普通にMessageBoxの方がいいかも？
        }
        catch (Exception e)
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
                catch
                {
                    // 無視。
                }
            }));
        }
        catch
        {
            // 無視。
        }
    }

    /// <summary>
    /// キャンセルボタン
    /// </summary>
    private void button2_Click(object sender, EventArgs e)
    {
        if (state == 0)
        {
            HideWindow();
        }
        else
        {
            textBox1.Clear();
        }
    }
}
