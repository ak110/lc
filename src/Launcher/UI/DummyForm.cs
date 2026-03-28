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
    SchedulerData schedulerData = new();

    HookManager hookManager;
    MainForm mainForm;
    ButtonLauncherForm? buttonLauncherForm;

    /// <summary>スケジューラータスク実行中フラグ (二重実行防止)</summary>
    bool schedulerRunning;

    public Config Config
    {
        get { return config; }
    }

    public CommandList CommandList { get; private set; }
    public ButtonLauncherData ButtonLauncherData { get; private set; }

    /// <summary>
    /// ダイアログのオーナーとして使える表示中のフォームを返す。
    /// 不可視のDummyFormをownerにすると別モニターに表示されるため、表示中のフォームを優先する。
    /// ownerがnullの場合、CenterParentが効かないためCenterScreenにフォールバックする。
    /// </summary>
    private IWin32Window? GetVisibleOwner()
    {
        if (buttonLauncherForm is { IsDisposed: false, Visible: true })
            return buttonLauncherForm;
        if (!mainForm.IsDisposed && mainForm.Visible)
            return mainForm;
        return null;
    }

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
        schedulerData = SchedulerData.Deserialize();
        new ReplaceEnvList(config.ReplaceEnv).Replace(schedulerData);
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
        schedulerTimer.Start();
    }

    private void DummyForm_Shown(object sender, EventArgs e)
    {
        Visible = false;
    }

    private void DummyForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        schedulerTimer.Stop();
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
        schedulerData = SchedulerData.Deserialize();
        new ReplaceEnvList(config.ReplaceEnv).Replace(schedulerData);
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
        using var form = new CommandManagementForm(this);
        form.ShowDialog(GetVisibleOwner());
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
    /// MainFormのコマンド一覧表示だけを更新（アイコン再読込なし）
    /// </summary>
    public void RefreshMainFormCommandList()
    {
        if (!mainForm.IsDisposed)
        {
            mainForm.RefreshCommandList();
        }
    }

    /// <summary>
    /// コンフィグダイアログ。
    /// </summary>
    public void ShowConfigDialog()
    {
        using var form = new ConfigForm(config, ButtonLauncherData);
        if (form.ShowDialog(GetVisibleOwner()) == DialogResult.OK)
        {
            config = form.Config;
            config.Serialize();

            // ボタンランチャーのColumns/Rows変更を検出して反映
            bool gridChanged = form.ButtonColumns != ButtonLauncherData.Columns
                || form.ButtonRows != ButtonLauncherData.Rows;
            if (gridChanged)
            {
                ButtonLauncherData.Columns = form.ButtonColumns;
                ButtonLauncherData.Rows = form.ButtonRows;
                ButtonLauncherData.Serialize();
                buttonLauncherForm?.ApplyGridSize();
            }

            ApplyConfig();
            if (!mainForm.IsDisposed)
            {
                mainForm.ApplyConfig();
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
                ShowUpdateForm(release);
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
    /// <summary>
    /// UpdateFormを表示する。更新実行はUpdateForm内で完結する。
    /// </summary>
    private void ShowUpdateForm(GitHubRelease release)
    {
        using var form = new UpdateForm(release);
        if (form.ShowDialog(GetVisibleOwner()) == DialogResult.OK)
        {
            // バッチスクリプトが起動済み。アプリを終了してバッチに再起動を任せる。
            Application.Exit();
        }
    }

    private void スケジューラー設定SToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var form = new SchedulerConfigForm(schedulerData);
        if (form.ShowDialog(GetVisibleOwner()) == DialogResult.OK)
        {
            schedulerData = form.Value;
            schedulerData.Serialize();
        }
    }

    private void スケジューラー一時停止PToolStripMenuItem_Click(object sender, EventArgs e)
    {
        schedulerTimer.Enabled = !schedulerTimer.Enabled;
        スケジューラー一時停止PToolStripMenuItem.Text = schedulerTimer.Enabled
            ? "スケジューラー一時停止(&P)"
            : "スケジューラー再開(&P)";
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

    /// <summary>
    /// スケジューラーのタイマーTick。スケジュール判定→タスク実行→LastCheckTime更新。
    /// </summary>
    private void schedulerTimer_Tick(object sender, EventArgs e)
    {
        if (schedulerRunning) return; // 前回のタスクがまだ実行中

        var now = DateTime.Now;
        var itemsToRun = SchedulerPresenter.GetItemsToRun(schedulerData, data.SchedulerLastCheckTime, now);
        if (itemsToRun.Count == 0)
        {
            // 実行対象なしでもLastCheckTimeを前進 (正常動作時の二重実行防止)
            data.SchedulerLastCheckTime = now;
            data.Serialize();
            return;
        }

        schedulerRunning = true;
        // 各アイテムのタスクをSTAスレッドで並行実行し、完了後にLastCheckTimeを更新
        foreach (var item in itemsToRun)
        {
            SchedulerPresenter.ExecuteItemTasks(item);
        }
        // ExecuteItemTasksはバックグラウンドスレッドを起動して即座に返る。
        // 厳密な完了待ちはせず、次のTickでisRunningガードを外す簡易方式。
        // (元のすけじゅらと同等の挙動)
        data.SchedulerLastCheckTime = now;
        data.Serialize();
        schedulerRunning = false;
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
