using System.Diagnostics;
using System.Net.Http;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Updater;
using Launcher.Win32;
using Microsoft.Win32;
using Process = System.Diagnostics.Process;

namespace Launcher.UI;

public partial class DummyForm : Form
{
    Config config = new();
    Data data = new();

    HookManager hookManager;
    MainForm mainForm;
    ButtonLauncherForm? buttonLauncherForm;

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

        var screenRect = new Rectangle();
        foreach (Screen s in Screen.AllScreens) { screenRect = Rectangle.Union(screenRect, s.Bounds); }
        Location = new Point(screenRect.Left - Size.Width, screenRect.Top - Size.Height);

        // 設定ファイルの読み込み
        config = Config.Deserialize();
        CommandList = CommandList.Deserialize(".cmd.cfg");
        ButtonLauncherData = ButtonLauncherData.Deserialize();
        try { data = Data.Deserialize(); } catch (IOException) { } catch (InvalidOperationException) { }

        notifyIcon1.Text = Infrastructure.AppVersion.Title;
        notifyIcon1.Visible = config.TrayIcon;

        data.WindowHandle = Handle.ToInt64();
        data.Serialize();

        hookManager = new HookManager(() => config, () => Handle, a => BeginInvoke(a));

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
        hookManager.Register();
    }

    private void DummyForm_Shown(object sender, EventArgs e)
    {
        Visible = false;
    }

    private void DummyForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        hookManager.Unregister();
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
                // WndProc内の例外ハンドラ: WinFormsのメッセージループ内なので握りつぶす必要がある
#pragma warning disable CA1031 // WndProcはメッセージループの最終防御ライン
                catch (Exception ex)
                {
                    Debug.WriteLine($"WndProc WM_APPMSG処理で例外: {ex}");
                    MessageBox.Show($"メッセージ処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                        "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
#pragma warning restore CA1031
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
            case TrayIconAction.ShowConfig: 設定CToolStripMenuItem_Click(this, EventArgs.Empty); break;
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
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"更新チェックに失敗しました。\n{ex.Message}", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (TaskCanceledException ex)
        {
            MessageBox.Show($"更新チェックに失敗しました。\n{ex.Message}", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (IOException ex)
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
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"更新失敗: {ex.Message}");
                    MessageBox.Show($"更新に失敗しました: {ex.Message}", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"更新失敗: {ex.Message}");
                    MessageBox.Show($"更新に失敗しました: {ex.Message}", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (InvalidOperationException ex)
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
        Process.GetCurrentProcess().PriorityClass = config.ProcessPriority switch
        {
            0 => ProcessPriorityClass.RealTime,
            1 => ProcessPriorityClass.High,
            2 => ProcessPriorityClass.AboveNormal,
            3 => ProcessPriorityClass.Normal,
            4 => ProcessPriorityClass.BelowNormal,
            5 => ProcessPriorityClass.Idle,
            _ => ProcessPriorityClass.Normal,
        };
        // ホットキー
        hookManager.UpdateHotkey(config.HotKey);

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
}
