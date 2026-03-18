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
    TreeLauncherForm treeLauncherForm = null;
    bool lbuttonDown;

    public Config Config
    {
        get { return config; }
    }

    public CommandList CommandList { get; private set; }
    public CommandList TreeCommandList { get; private set; }

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
        TreeCommandList = CommandList.Deserialize(".treecmd.cfg");
        try { data = Data.Deserialize(); } catch { }

        int build = System.Diagnostics.Process.GetCurrentProcess()
            .MainModule.FileVersionInfo.FileBuildPart;
        notifyIcon1.Text = "らんちゃ build-" + build;
        notifyIcon1.Visible = config.TrayIcon;

        data.WindowHandle = Handle.ToInt64();
        data.Serialize();

        mainForm = new MainForm(this, contextMenuStrip1);
        if (!config.HideFirst)
        {
            mainForm.Show(this);
        }
        if (config.UseTreeLauncher)
        {
            treeLauncherForm = new TreeLauncherForm(this);
        }

        ApplyConfig();

        // 起動時の更新チェック
        if (new GitHubUpdateClient(config.UpdateConfig).ShouldCheck(data.UpdateRecord))
        {
            CheckForUpdateAsync();
        }
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
        TreeCommandList = CommandList.Deserialize(".treecmd.cfg");
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
        System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(Application.ExecutablePath));
    }

    private async void ネットワーク更新NToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var client = new GitHubUpdateClient(config.UpdateConfig);
        try
        {
            var release = await client.GetLatestReleaseAsync();
            if (release == null) return;

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
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"更新チェック失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 起動時の更新チェック（バックグラウンドで実行）
    /// </summary>
    private async void CheckForUpdateAsync()
    {
        var client = new GitHubUpdateClient(config.UpdateConfig);
        try
        {
            var release = await client.GetLatestReleaseAsync();
            if (release == null) return;

            data.UpdateRecord.LastChecked = DateTime.Now;

            if (GitHubUpdateClient.IsUpdateAvailable(release, data.UpdateRecord))
            {
                data.UpdateRecord.LastKnownVersion = release.TagName;
                data.Serialize();
                // UIスレッドに戻してダイアログ表示
                BeginInvoke(async () =>
                {
                    await ShowUpdateFormAsync(release);
                });
            }
            else
            {
                data.Serialize();
            }
        }
        catch
        {
            // 起動時の自動チェック失敗は無視
        }
    }

    /// <summary>
    /// UpdateFormを表示し、更新実行またはスキップを処理する
    /// </summary>
    private async Task ShowUpdateFormAsync(GitHubRelease release)
    {
        using (var form = new UpdateForm(release))
        {
            var result = form.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                // 更新実行
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
            else if (result == DialogResult.Ignore)
            {
                // スキップ
                data.UpdateRecord.SkippedVersion = release.TagName;
                data.Serialize();
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
    }

    void Hook_KeyHook(object sender, KeyHookEventArgs e)
    {
        if (e.HookCode == Hook.HC_ACTION)
        {
            bool todo = (e.WParam == Hook.WM_KEYDOWN ||
                          e.WParam == Hook.WM_SYSKEYDOWN);
            if (todo)
            {
                if (e.HookStruct.vkCode == (int)hotkeyVK &&
                    KeyTable.GetModifiers() == modifiers)
                {
                    WindowHelper window = new WindowHelper(Handle);
                    window.SendMessage(WM.WM_APP,
                        Program.WM_APPMSG_WPARAM,
                        Program.WM_APPMSG_SHOWHIDE);
                    e.Handled = true;
                }
            }
        }
    }

    void Hook_MouseHook(object sender, MouseHookEventArgs e)
    {
        if (config.UseTreeLauncher)
        {
            if (e.HookCode == Hook.HC_ACTION)
            {
                if (e.WParam == Hook.WM_LBUTTONDOWN)
                {
                    lbuttonDown = true;
                }
                else if (e.WParam == Hook.WM_LBUTTONUP)
                {
                    lbuttonDown = false;
                }
                else if (e.WParam == Hook.WM_RBUTTONDOWN)
                {
                    if (lbuttonDown)
                    {
                        treeLauncherForm.ShowLauncher();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
