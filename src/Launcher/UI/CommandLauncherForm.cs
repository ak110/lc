using System.ComponentModel;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

public partial class CommandLauncherForm : Form
{
    ApplicationHostForm ownerForm;
    CommandLauncherPresenter presenter;

    InputState state;
    int lastFocus;

    bool recurseGuard;

    AsyncIconLoader iconLoader = new(threadPriority: ThreadPriority.BelowNormal);

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

        listView1.GetType().GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(listView1, true);

        saveConfigTimer = new System.Windows.Forms.Timer { Interval = 200 };
        saveConfigTimer.Tick += (s, e) =>
        {
            saveConfigTimer.Stop();
            ownerForm.Config.Save(ownerForm.ReportSaveFailure, ownerForm.ConfigurationBaseName);
        };
        components ??= new Container();
        components.Add(saveConfigTimer);

        Size = ownerForm.Config.WindowSize;
        this.SetLocationWithClip(ownerForm.Config.WindowPos);
    }

    public void PreInitialize()
    {
        _ = Handle;
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
        textBox1_TextChanged(this, EventArgs.Empty);
        ApplyConfig();
    }

    private void CommandLauncherForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.FormOwnerClosing)
        {
            if (ownerForm.Config.CloseButton == CloseButtonBehavior.Hide)
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

        FlushPendingSave();

        iconLoader.IconLoaded -= iconLoader_IconLoaded;
        iconLoader.Dispose();
    }

    public void FlushPendingSave()
    {
        if (saveConfigTimer.Enabled)
        {
            saveConfigTimer.Stop();
            ownerForm.Config.Save(ownerForm.ReportSaveFailure, ownerForm.ConfigurationBaseName);
        }
    }

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

        ReloadIcons();

        ownerForm.RequestEnvironmentReplacement();
    }

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
        this.SetLocationWithClip(ownerForm.Config.WindowPos);
        WindowHelper.ActivateForce(this);
    }

    public void HideWindow()
    {
        Hide();
        textBox1.Clear();
    }

    #endregion

    #region CommandLauncherForm

    private void CommandLauncherForm_LocationChanged(object sender, EventArgs e)
    {
        if (Visible && WindowState == FormWindowState.Normal)
        {
            ownerForm.Config.WindowPos = Location;
            saveConfigTimer.Stop();
            saveConfigTimer.Start();
        }
    }

    private void CommandLauncherForm_SizeChanged(object sender, EventArgs e)
    {
        if (Visible && WindowState == FormWindowState.Normal)
        {
            ownerForm.Config.WindowSize = Size;
            saveConfigTimer.Stop();
            saveConfigTimer.Start();
        }
    }

    private void CommandLauncherForm_Activated(object sender, EventArgs e)
    {
        BringToFront();
        Activate();
        textBox1.Focus();
    }

    private void CommandLauncherForm_Deactivate(object sender, EventArgs e)
    {
        if (ownerForm.Config.WindowHideNoActive)
        {
            if (OwnedForms.Length <= 0 && !ownerForm.HasActiveNotifications)
            {
                HideWindow();
            }
        }
    }

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

    private void textBox1_Enter(object sender, EventArgs e)
    {
        lastFocus = 0;
    }

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

    private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == '\b')
        {
            int n1 = textBox1.SelectionStart;
            int n2 = n1 + textBox1.SelectionLength;
            if (textBox1.Text.Length == n2)
            {
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

    private void textBox1_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up)
        {
            if (0 < listView1.Items.Count)
            {
                SetListViewSelection(listView1.Items.Count - 1);
                listView1.Select();
                e.Handled = true;
            }
        }
        else if (e.KeyCode == Keys.Down)
        {
            if (0 < listView1.Items.Count)
            {
                SetListViewSelection(0);
                listView1.Select();
                e.Handled = true;
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

    private void textBox1_KeyUp(object sender, KeyEventArgs e)
    {
        UpdateButtonText();
    }

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

    private void ActivateTextBox()
    {
        SetListViewSelection(0);
        textBox1.Select();
        textBox1.Focus();
    }

    #region listView1

    private void listView1_Enter(object sender, EventArgs e)
    {
        lastFocus = 1;
    }

    private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        switch (ownerForm.Config.ItemDoubleClick)
        {
            case ItemAction.Execute: 実行RToolStripMenuItem_Click(this, EventArgs.Empty); break;
            case ItemAction.OpenDirectory: フォルダを開くFToolStripMenuItem_Click(this, EventArgs.Empty); break;
            case ItemAction.EditCommand: 設定CToolStripMenuItem1_Click(this, EventArgs.Empty); break;
            case ItemAction.Delete: 削除DToolStripMenuItem_Click(this, EventArgs.Empty); break;
        }
    }

    private void listView1_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up)
        {
            if (listView1.SelectedIndices.Count == 1 &&
                listView1.SelectedIndices[0] == 0)
            {
                ActivateTextBox();
                e.Handled = true;
            }
            UpdateButtonText();
        }
        else if (e.KeyCode == Keys.Down)
        {
            if (listView1.SelectedIndices.Count == 1 &&
                listView1.SelectedIndices[0] == listView1.Items.Count - 1)
            {
                listView1.EnsureVisible(0);
                ActivateTextBox();
                e.Handled = true;
            }
            UpdateButtonText();
        }
        else if (e.KeyCode == Keys.Home)
        {
            ActivateTextBox();
            e.Handled = true;
            UpdateButtonText();
        }
        else if (e.KeyCode == Keys.Apps)
        {
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

    private void listView1_KeyUp(object sender, KeyEventArgs e)
    {
        UpdateButtonText();
    }

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
                ownerForm.SaveEditedCommand(command);
                ApplyConfig();
                textBox1.Clear();
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
                ownerForm.CommandList.Save(ownerForm.ReportSaveFailure, ownerForm.ConfigurationBaseName);
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
                ownerForm.CommandList.Save(ownerForm.ReportSaveFailure, ownerForm.ConfigurationBaseName);
                ApplyConfig();
                textBox1_TextChanged(this, EventArgs.Empty);
            }
        }
    }

    #endregion

    #region iconLoader

    void iconLoader_IconLoaded(object? sender, IconLoadedEventArgs e)
    {
        IconReceiver.Receive(this, iconLoader, e, icon =>
        {
            Command command = (Command)e.Arg!;
            imageList1.Images.Add(icon);
            imageList1.Images.SetKeyName(imageList1.Images.Count - 1, command.FileName);
            command.IconIndex = imageList1.Images.IndexOfKey(command.FileName);
            foreach (ListViewItem item in listView1.Items)
            {
                if (command.Equals(item.Tag))
                {
                    item.ImageIndex = command.IconIndex;
                    listView1.RedrawItems(item.Index, item.Index, true);
                    break;
                }
            }
        });
    }

    #endregion

    private void InnerTextChanged()
    {
        var result = presenter.ProcessTextChange(GetInputText());
        state = result.State;

        if (result.CompletionText is not null)
        {
            textBox1.Text = result.CompletionText;
            textBox1.Select(result.SelectionStart, result.SelectionLength);
        }

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

    private void UpdateButtonText()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(state, lastFocus, GetInputModifiers());
        button1.Text = texts.Button1Text;
        button2.Text = texts.Button2Text;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        Command? firstCommand = listView1.Items.Count > 0
            ? (Command)listView1.Items[0].Tag! : null;
        Command? selectedCommand = listView1.SelectedItems.Count > 0
            ? (Command)listView1.SelectedItems[0].Tag! : null;

        var result = CommandLauncherPresenter.DetermineAction(
            state, lastFocus, firstCommand, selectedCommand, GetInputModifiers());

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
                        ownerForm.CommandList.Save(ownerForm.ReportSaveFailure, ownerForm.ConfigurationBaseName);
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
                        ownerForm.SaveEditedCommand(result.TargetCommand!);
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

    private void OpenDirectory(Command command)
    {
        ShellLaunchService.OpenDirectory(this, command, ownerForm.Config);
    }

    private static InputModifiers GetInputModifiers()
    {
        var result = InputModifiers.None;
        if (ModifierKeys.HasFlag(Keys.Shift)) result |= InputModifiers.Shift;
        if (ModifierKeys.HasFlag(Keys.Control)) result |= InputModifiers.Control;
        if (ModifierKeys.HasFlag(Keys.Alt)) result |= InputModifiers.Alt;
        return result;
    }

    private void ExecuteCommand(Command command, string input)
    {
        DiagnosticLog.Info("Command.Execute", $"name={command.Name}");
        var snapshot = command.Clone();
        var config = ownerForm.Config.Clone();
        var handle = Handle;
        ShellLaunchService.Start(this, () => snapshot.Execute(input, config, handle, NativeMethods.IsUserAnAdmin()));
    }

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
