#nullable disable
using System.Diagnostics;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Updater;
using Launcher.Win32;
using Microsoft.Win32;
using Process = System.Diagnostics.Process;

namespace Launcher.UI;

public partial class DummyForm : Form
{
    Config config = new Config();
    Data data = new Data();

    Keys hotkeyVK = 0;
    KeyTable.Modifiers modifiers;

    MainForm mainForm = null;
    ButtonLauncherForm buttonLauncherForm = null;
    bool lbuttonDown;
    bool rbuttonDown;
    // トリガーボタンのUPイベント抑制用フラグ
    bool suppressNextLButtonUp;
    bool suppressNextRButtonUp;
    // ホットキーのKEYUPイベント抑制用
    int suppressKeyUpVK;

    public Config Config
    {
        get { return config; }
    }

    public CommandList CommandList { get; private set; }
    public ButtonLauncherData ButtonLauncherData { get; private set; }

    public DummyForm()
    {
        InitializeComponent();
        Visible = false;

        Rectangle screenRect = new Rectangle();
        foreach (Screen s in Screen.AllScreens) { screenRect = Rectangle.Union(screenRect, s.Bounds); }
        Location = new Point(screenRect.Left - Size.Width, screenRect.Top - Size.Height);

        // 設定ファイルの読み込み
        config = Config.Deserialize();
        CommandList = CommandList.Deserialize(".cmd.cfg");
        ButtonLauncherData = ButtonLauncherData.Deserialize();
        try { data = Data.Deserialize(); } catch { }

        notifyIcon1.Text = Infrastructure.AppVersion.Title;
        notifyIcon1.Visible = config.TrayIcon;

        data.WindowHandle = Handle.ToInt64();
        data.Serialize();

        mainForm = new MainForm(this, contextMenuStrip1);
        mainForm.PreInitialize();
        if (!config.HideFirst)
        {
            mainForm.Show(this);
        }
        ApplyConfig();

        // 起動時の自動更新チェックは無効化（手動で実行する）
    }

    /// <summary>
    /// 非アクティブに表示
    /// </summary>
    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    private void DummyForm_Load(object sender, EventArgs e)
    {
        Visible = false;

        // KeyHookとか。
        Hook.KeyHook += Hook_KeyHook;
        Hook.MouseHook += Hook_MouseHook;
        Hook.SetKeyHook();
        Hook.SetMouseHook();
    }

    private void DummyForm_Shown(object sender, EventArgs e)
    {
        Visible = false;
    }

    private void DummyForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        notifyIcon1.Dispose();

        data.WindowHandle = 0;
        data.Serialize();
    }

    /// <summary>
    /// WndProc。
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Program.WM_APPMSG)
        {
            if (m.WParam == Program.WM_APPMSG_WPARAM)
            {
                try
                {
                    if (m.LParam == Program.WM_APPMSG_SHOWHIDE)
                    {
                        ShowHide();
                    }
                    else if (m.LParam == Program.WM_APPMSG_RELOAD)
                    {
                        Reload();
                    }
                    else if (m.LParam == Program.WM_APPMSG_RESTART)
                    {
                        Restart();
                    }
                    else if (m.LParam == Program.WM_APPMSG_SHOWBUTTONLAUNCHER)
                    {
                        buttonLauncherForm?.ShowLauncher();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WndProc WM_APPMSG処理で例外: {ex}");
                    MessageBox.Show($"メッセージ処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                        "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        base.WndProc(ref m);
    }

    /// <summary>
    /// メインウィンドウを表示したり隠したり
    /// </summary>
    public void ShowHide()
    {
        if (mainForm.IsDisposed)
        {
            mainForm = new MainForm(this, contextMenuStrip1);
            mainForm.Show(this);
        }
        else if (!mainForm.Visible)
        {
            mainForm.ShowWindow();
        }
        else
        {
            mainForm.HideWindow();
        }
    }

    /// <summary>
    /// らんちゃ.cmd.cfgのリロード
    /// </summary>
    public void Reload()
    {
        CommandList = CommandList.Deserialize(".cmd.cfg");
        ButtonLauncherData = ButtonLauncherData.Deserialize();
        if (!mainForm.IsDisposed)
        {
            mainForm.ApplyConfig();
        }
    }

    #region メニューなど

    /// <summary>
    /// アイコンがダブルクリックされた
    /// </summary>
    private void notifyIcon1_DoubleClick(object sender, EventArgs e)
    {
        switch (config.IconDoubleClick)
        {
            case TrayIconAction.ShowHide: ShowHide(); break;
            case TrayIconAction.ShowConfig: 設定CToolStripMenuItem_Click(this, null); break;
            default: break;
        }
    }

    private void 設定CToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowConfigDialog();
    }

    private void コマンドの管理LToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using (var form = new CommandManagementForm(this))
        {
            form.ShowDialog(this);
        }
    }

    /// <summary>
    /// MainFormの表示を更新
    /// </summary>
    public void RefreshMainForm()
    {
        if (!mainForm.IsDisposed)
        {
            mainForm.ApplyConfig();
        }
    }

    /// <summary>
    /// コンフィグダイアログ。
    /// </summary>
    public void ShowConfigDialog()
    {
        using (ConfigForm form = new ConfigForm(config))
        {
            Form owner = !mainForm.IsDisposed && mainForm.Visible ? (Form)mainForm : (Form)this;
            if (form.ShowDialog(owner) == DialogResult.OK)
            {
                config = form.Config;
                config.Serialize();

                ApplyConfig();
                if (!mainForm.IsDisposed)
                {
                    mainForm.ApplyConfig();
                }
            }
        }
    }

    private void メインウィンドウを表示非表示VToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowHide();
    }

    private void 実行ファイルのあるフォルダを開くMToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var psi = new ProcessStartInfo
        {
            FileName = System.IO.Path.GetDirectoryName(Application.ExecutablePath),
            UseShellExecute = true,
        };
        Process.Start(psi);
    }

    private async void ネットワーク更新NToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            var release = await GitHubUpdateClient.GetLatestReleaseAsync();
            if (release == null)
            {
                MessageBox.Show("リリース情報を取得できませんでした。", AppVersion.Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            data.UpdateRecord.LastChecked = DateTime.Now;

            if (GitHubUpdateClient.IsUpdateAvailable(release, data.UpdateRecord))
            {
                data.UpdateRecord.LastKnownVersion = release.TagName;
                data.Serialize();
                await ShowUpdateFormAsync(release);
            }
            else
            {
                data.Serialize();
                MessageBox.Show("現在のバージョンは最新です。", AppVersion.Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"更新チェックに失敗しました。\n{ex.Message}", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// UpdateFormを表示し、更新実行を処理する
    /// </summary>
    private async Task ShowUpdateFormAsync(GitHubRelease release)
    {
        using (var form = new UpdateForm(release))
        {
            var result = form.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                try
                {
                    await form.PerformUpdateAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"更新失敗: {ex.Message}");
                    MessageBox.Show($"更新に失敗しました: {ex.Message}", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    private void 再起動RToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Restart();
    }

    private void 終了XToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Close();
    }

    #endregion

    private void Restart()
    {
        AppBase.SetRestart();
        Close();
    }

    private void ApplyConfig()
    {
        // プロセス優先度
        switch (config.ProcessPriority)
        {
            case 0: Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime; break;
            case 1: Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; break;
            case 2: Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal; break;
            case 3: Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal; break;
            case 4: Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; break;
            case 5: Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle; break;
            default: goto case 3;
        }
        // ほっときー
        var hk = KeyTable.GetKeyWithModifiers(config.HotKey);
        hotkeyVK = KeyTable.KeysToVKey(hk.First);
        modifiers = hk.Second;

        // ボタンランチャーの生成・破棄
        bool enabled = config.ButtonLauncherActivation != Core.ButtonLauncherActivation.Disabled;
        if (enabled && buttonLauncherForm == null)
        {
            buttonLauncherForm = new ButtonLauncherForm(this, contextMenuStrip1);
        }
        else if (!enabled && buttonLauncherForm != null)
        {
            buttonLauncherForm.Close();
            buttonLauncherForm.Dispose();
            buttonLauncherForm = null;
        }
    }

    void Hook_KeyHook(object sender, KeyHookEventArgs e)
    {
        try
        {
            if (e.HookCode == Hook.HC_ACTION)
            {
                if (e.WParam == Hook.WM_KEYDOWN || e.WParam == Hook.WM_SYSKEYDOWN)
                {
                    if (e.HookStruct.vkCode == (int)hotkeyVK &&
                        KeyTable.GetModifiers() == modifiers)
                    {
                        WindowHelper window = new WindowHelper(Handle);
                        window.SendMessage(WM.WM_APP,
                            Program.WM_APPMSG_WPARAM,
                            Program.WM_APPMSG_SHOWHIDE);
                        e.Handled = true;
                        // 対応するKEYUPを1回だけ抑制
                        suppressKeyUpVK = e.HookStruct.vkCode;
                    }
                }
                else if (e.WParam == Hook.WM_KEYUP || e.WParam == Hook.WM_SYSKEYUP)
                {
                    if (suppressKeyUpVK != 0 && e.HookStruct.vkCode == suppressKeyUpVK)
                    {
                        suppressKeyUpVK = 0;
                        e.Handled = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hook_KeyHookで例外: {ex}");
            // フックコールバック内ではMessageBoxを直接表示するとフックがタイムアウトするため、
            // BeginInvokeで非同期表示
            BeginInvoke(() => MessageBox.Show(
                $"キーボードフック処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    void Hook_MouseHook(object sender, MouseHookEventArgs e)
    {
        try
        {
            if (config.ButtonLauncherActivation == Core.ButtonLauncherActivation.Disabled) return;
            if (e.HookCode != Hook.HC_ACTION) return;

            if (e.WParam == Hook.WM_LBUTTONDOWN)
            {
                lbuttonDown = true;
                // 右→左: 右ボタン押下中に左クリック
                if (config.ButtonLauncherActivation == Core.ButtonLauncherActivation.RightThenLeft && rbuttonDown)
                {
                    // フック内から直接ShowLauncher()を呼ぶとSetForegroundWindowが拒否されるため、
                    // PostMessageで間接的に呼び出す
                    new WindowHelper(Handle).PostMessage(
                        Program.WM_APPMSG, Program.WM_APPMSG_WPARAM, Program.WM_APPMSG_SHOWBUTTONLAUNCHER);
                    e.Handled = true;
                    suppressNextLButtonUp = true;
                }
            }
            else if (e.WParam == Hook.WM_LBUTTONUP)
            {
                lbuttonDown = false;
                if (suppressNextLButtonUp)
                {
                    suppressNextLButtonUp = false;
                    e.Handled = true;
                }
            }
            else if (e.WParam == Hook.WM_RBUTTONDOWN)
            {
                rbuttonDown = true;
                // 左→右: 左ボタン押下中に右クリック
                if (config.ButtonLauncherActivation == Core.ButtonLauncherActivation.LeftThenRight && lbuttonDown)
                {
                    new WindowHelper(Handle).PostMessage(
                        Program.WM_APPMSG, Program.WM_APPMSG_WPARAM, Program.WM_APPMSG_SHOWBUTTONLAUNCHER);
                    e.Handled = true;
                    suppressNextRButtonUp = true;
                }
            }
            else if (e.WParam == Hook.WM_RBUTTONUP)
            {
                rbuttonDown = false;
                if (suppressNextRButtonUp)
                {
                    suppressNextRButtonUp = false;
                    e.Handled = true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hook_MouseHookで例外: {ex}");
            BeginInvoke(() => MessageBox.Show(
                $"マウスフック処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }
}
