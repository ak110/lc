using System.Diagnostics;
using System.Runtime.InteropServices;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Updater;
using Launcher.Win32;
using Process = System.Diagnostics.Process;

namespace Launcher.UI;

public partial class ApplicationHostForm : Form
{
    Config config = new();
    Data data = new();
    SchedulerData schedulerData = new();

    HookManager hookManager;
    CommandLauncherForm commandLauncherForm;
    ButtonLauncherForm? buttonLauncherForm;
    MemoForm? memoForm;
    bool shuttingDown;
    bool replacementRunning;
    bool replacementRequested;

    public event EventHandler? CommandsChanged;

    readonly SchedulerRunCoordinator schedulerRunCoordinator;

    readonly List<Form> activeNotifications = [];

    public bool HasActiveNotifications => activeNotifications.Exists(f => !f.IsDisposed);

    readonly EnvironmentRefresher envRefresher = new();

    Action<string, string>? schedulerShowBalloonTip;
    Action<string, string>? schedulerShowMessageBox;

    public Config Config
    {
        get { return config; }
    }

    public CommandList CommandList { get; private set; }
    public ButtonLauncherData ButtonLauncherData { get; private set; }
    public MemoData MemoData { get; private set; }

    private void IfCommandLauncherFormAlive(Action<CommandLauncherForm> action)
    {
        if (!commandLauncherForm.IsDisposed)
        {
            action(commandLauncherForm);
        }
    }

    /// <summary>
    /// ダイアログのオーナーとして利用できる表示中のフォームを返す。
    /// 不可視の ApplicationHostForm を owner にすると別モニターに表示されるため、表示中のフォームを優先する。
    /// owner が null の場合は CenterParent が機能しないため CenterScreen にフォールバックする。
    /// </summary>
    private IWin32Window? GetVisibleOwner()
    {
        if (buttonLauncherForm is { IsDisposed: false, Visible: true })
            return buttonLauncherForm;
        if (memoForm is { IsDisposed: false, Visible: true })
            return memoForm;
        if (commandLauncherForm is { IsDisposed: false, Visible: true })
            return commandLauncherForm;

        // ConfigForm など launcher 内のモーダル表示中フォームを owner に使う。
        // Form.ActiveForm は現在アクティブな自プロセスのフォームを返す。
        // ApplicationHostForm 自身は Visible=false のため候補から除外される。
        if (Form.ActiveForm is { IsDisposed: false, Visible: true } active && active != this)
            return active;

        return null;
    }

    public string? ConfigurationBaseName { get; }

    public ApplicationHostForm(string? baseName = null)
    {
        ConfigurationBaseName = baseName;
        InitializeComponent();
        Visible = false;

        var screenRect = new Rectangle();
        foreach (Screen s in Screen.AllScreens) { screenRect = Rectangle.Union(screenRect, s.Bounds); }
        Location = new Point(screenRect.Left - Size.Width, screenRect.Top - Size.Height);

        var configResult = Config.Load(baseName);
        config = AcceptLoadResult(configResult, Config.Store, configResult.Value);
        var commandResult = CommandList.Load(baseName);
        CommandList = AcceptLoadResult(commandResult, CommandList.Store, commandResult.Value);
        var buttonResult = ButtonLauncherData.Load(baseName);
        ButtonLauncherData = AcceptLoadResult(buttonResult, ButtonLauncherData.Store, buttonResult.Value);
        var memoResult = MemoData.Load(baseName);
        MemoData = AcceptLoadResult(memoResult, MemoData.Store, memoResult.Value);
        var schedulerResult = SchedulerData.Load(baseName);
        schedulerData = AcceptLoadResult(schedulerResult, SchedulerData.Store, schedulerResult.Value);
        var dataResult = Data.Load(baseName);
        data = AcceptLoadResult(dataResult, Data.Store, dataResult.Value);

        notifyIcon1.Text = Infrastructure.AppVersion.Title;
        notifyIcon1.Visible = config.TrayIcon;

        data.WindowHandle = Handle.ToInt64();
        data.Save(ReportSaveFailure, ConfigurationBaseName);

        // Control作成時にWinFormsが登録するコンテキストは専用の配送用HWNDを持つ。
        // ホストのHWND取得が失敗しても、そのホストへBeginInvokeせずに報告を届ける。
        var uiContext = (WindowsFormsSynchronizationContext)SynchronizationContext.Current!;
        hookManager = new HookManager(() => config, () => Handle,
            a => UiThreadDispatcher.SafeBeginInvoke(uiContext, this, () =>
            {
                if (!shuttingDown) a();
            }));

        commandLauncherForm = new CommandLauncherForm(this, contextMenuStrip1);
        ErrorReporter.Instance.SetOwner(this, GetVisibleOwner);
        commandLauncherForm.PreInitialize();
        if (!config.HideFirst)
        {
            commandLauncherForm.Show(this);
        }
        ApplyConfig();
        SetupSchedulerActions();
        schedulerRunCoordinator = new SchedulerRunCoordinator(() => schedulerData.Items, StartSchedulerItem);
        EnsureSchedulerIds();
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    private void ApplicationHostForm_Load(object sender, EventArgs e)
    {
        Visible = false;
        hookManager.Register();
        schedulerTimer.Start();
    }

    private void ApplicationHostForm_Shown(object sender, EventArgs e)
    {
        Visible = false;
    }

    private void ApplicationHostForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (!ConfirmPendingMemoSave("終了"))
        {
            e.Cancel = true;
            return;
        }
        PrepareShutdown();
    }

    public void PrepareShutdown()
    {
        if (shuttingDown) return;
        shuttingDown = true;
        schedulerTimer.Stop();
        envChangeDebounceTimer.Stop();
        schedulerRunCoordinator.Shutdown();
        hookManager.Unregister();
        notifyIcon1.Dispose();

        if (memoForm is { IsDisposed: false })
        {
            memoForm.FlushPendingSave();
        }
        IfCommandLauncherFormAlive(form => form.FlushPendingSave());

        data.WindowHandle = 0;
        data.Save(ReportSaveFailure, ConfigurationBaseName);
    }

    private bool ConfirmPendingMemoSave(string operation)
    {
        if (memoForm is not { IsDisposed: false } || memoForm.FlushPendingSave()) return true;

        memoForm.ShowMemo();
        return MessageBox.Show(memoForm,
            $"メモを保存できませんでした。{operation}すると未保存の内容が失われます。\r\n"
                + $"「いいえ」で戻り、再保存するか本文を別の場所へコピーしてください。\r\n\r\n{operation}しますか？",
            AppVersion.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM.WM_SETTINGCHANGE && m.LParam != IntPtr.Zero)
        {
            // 環境変数の変更通知のみを取得する (色・フォント等の変更も同じメッセージで通知される)。
            string? area = Marshal.PtrToStringAuto(m.LParam);
            if (string.Equals(area, "Environment", StringComparison.Ordinal))
            {
                // 短時間に複数回通知されるためデバウンスで集約する。
                envChangeDebounceTimer.Stop();
                envChangeDebounceTimer.Start();
            }
        }
        if (m.Msg == ResidentMessages.WM_APPMSG)
        {
            if (m.WParam == ResidentMessages.WM_APPMSG_WPARAM)
            {
                try
                {
                    if (m.LParam == ResidentMessages.WM_APPMSG_SHOWHIDE)
                    {
                        ShowHide();
                    }
                    else if (m.LParam == ResidentMessages.WM_APPMSG_RELOAD)
                    {
                        Reload();
                    }
                    else if (m.LParam == ResidentMessages.WM_APPMSG_RESTART)
                    {
                        Restart();
                    }
                    else if (m.LParam == ResidentMessages.WM_APPMSG_SHOWBUTTONLAUNCHER)
                    {
                        HideMemoIfVisible();
                        buttonLauncherForm?.ShowLauncher();
                    }
                    else if (m.LParam == ResidentMessages.WM_APPMSG_SHOWMEMO)
                    {
                        ShowHideMemo();
                    }
                }
                // WndProc内の例外ハンドラ: WinFormsのメッセージループの最終防御ラインのため全例外を捕捉する
#pragma warning disable CA1031
                catch (Exception ex)
                {
                    ErrorReporter.Instance.OnException(ex);
                }
#pragma warning restore CA1031
            }
        }
        base.WndProc(ref m);
    }

    public void ShowHide()
    {
        bool hasNotifications = HasActiveNotifications;

        if (commandLauncherForm.IsDisposed)
        {
            HideMemoIfVisible();
            commandLauncherForm = new CommandLauncherForm(this, contextMenuStrip1);
            commandLauncherForm.Show(this);
        }
        else if (!commandLauncherForm.Visible)
        {
            HideMemoIfVisible();
            commandLauncherForm.ShowWindow();
        }
        else if (!hasNotifications)
        {
            commandLauncherForm.HideWindow();
        }

        foreach (var form in activeNotifications)
        {
            if (!form.IsDisposed)
            {
                form.Activate();
                form.BringToFront();
            }
        }
    }

    void HideMemoIfVisible()
    {
        if (memoForm is { IsDisposed: false })
        {
            memoForm.HideIfVisible();
        }
    }

    public void ShowHideMemo()
    {
        if (memoForm is null || memoForm.IsDisposed)
        {
            memoForm = new MemoForm(this);
        }
        memoForm.ToggleVisible();
    }

    public void Reload()
    {
        var stopwatch = Stopwatch.StartNew();
        CommandList.ReplaceContents(AcceptLoadResult(CommandList.Load(ConfigurationBaseName), CommandList.Store, CommandList));
        RefreshCommandLauncherFormCommandList();
        IfCommandLauncherFormAlive(form => form.ApplyConfig());
        RequestEnvironmentReplacement();
        DiagnosticLog.Info("Host.Reload", $"elapsed={stopwatch.ElapsedMilliseconds}ms");
    }

    T AcceptLoadResult<T>(ConfigLoadResult<T> result, ConfigFile<T> store, T fallback)
        where T : ConfigStore, new()
    {
        return ConfigLoadInteraction.Accept(result, store, fallback, notice =>
            MessageBox.Show(notice.Message, AppVersion.Title,
                notice.ConfirmRestore ? MessageBoxButtons.YesNo : MessageBoxButtons.OK,
                MessageBoxIcon.Warning) == DialogResult.Yes, ConfigurationBaseName);
    }

    public void ReportSaveFailure(ConfigSaveFailure failure) => MessageBox.Show(
        GetVisibleOwner(), failure.Message, AppVersion.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public bool SaveEditedCommand(Command command)
    {
        if (!CommandList.Commands.Contains(command))
        {
            MessageBox.Show(GetVisibleOwner(), "編集中のコマンドが再読込で変更または削除されました。\r\n"
                + "一覧からコマンドを選び直して編集してください。", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        bool saved = CommandList.Save(ReportSaveFailure, ConfigurationBaseName);
        RefreshCommandLauncherFormCommandList();
        RequestEnvironmentReplacement();
        return saved;
    }

    #region メニューなど

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
        form.ShowDialogOver(GetVisibleOwner());
    }

    public void RefreshCommandLauncherFormCommandList()
    {
        IfCommandLauncherFormAlive(form => form.RefreshCommandList());
        CommandsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ShowConfigDialog()
    {
        using var form = new ConfigForm(config, ButtonLauncherData);
        if (form.ShowDialogOver(GetVisibleOwner()) == DialogResult.OK)
        {
            config = form.Config;
            config.Save(ReportSaveFailure, ConfigurationBaseName);

            bool gridChanged = form.ButtonColumns != ButtonLauncherData.Columns
                || form.ButtonRows != ButtonLauncherData.Rows;
            if (gridChanged)
            {
                ButtonLauncherData.Columns = form.ButtonColumns;
                ButtonLauncherData.Rows = form.ButtonRows;
                ButtonLauncherData.Save(ReportSaveFailure, ConfigurationBaseName);
                buttonLauncherForm?.ApplyGridSize();
            }

            ApplyConfig();
            IfCommandLauncherFormAlive(form => form.ApplyConfig());
        }
    }

    private void メインウィンドウを表示非表示VToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowHide();
    }

    private void メモパッドDToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowHideMemo();
    }

    private void 実行ファイルのあるフォルダを開くMToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var handle = Handle;
        ShellLaunchService.Start(this,
            () => LaunchRequestBuilder.Create(Path.GetDirectoryName(Application.ExecutablePath)!, owner: handle));
    }

    private async void ネットワーク更新NToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            var release = await GitHubUpdateClient.GetLatestReleaseAsync();
            if (!GitHubUpdateClient.IsUpdateAvailable(release))
            {
                MessageBox.Show("現在のバージョンは最新です。", AppVersion.Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ConfirmPendingMemoSave("更新")) return;
            using var form = new UpdateForm(release!, PrepareShutdown);
            form.ShowDialogOver(GetVisibleOwner());
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MessageBox.Show($"更新チェックに失敗しました。\n{ex.Message}\n\n{UpdateForm.RetryGuidance}", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
#pragma warning restore CA1031
    }

    private void ホームページを開くHToolStripMenuItem_Click(object sender, EventArgs e)
    {
        const string url = "https://github.com/ak110/lc";
        var handle = Handle;
        ShellLaunchService.Start(this, () => LaunchRequestBuilder.Create(url, owner: handle));
    }

    private void スケジューラー設定SToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var form = new SchedulerConfigForm(schedulerData, schedulerShowBalloonTip, schedulerShowMessageBox);
        if (form.ShowDialogOver(GetVisibleOwner()) == DialogResult.OK)
        {
            schedulerData.Items = form.Value.Items;
            schedulerRunCoordinator.ItemsChanged();
            EnsureSchedulerIds();
            schedulerData.Save(ReportSaveFailure, ConfigurationBaseName);
            RequestEnvironmentReplacement();
        }
    }

    private void スケジューラー一時停止PToolStripMenuItem_Click(object sender, EventArgs e)
    {
        schedulerTimer.Enabled = !schedulerTimer.Enabled;
        if (schedulerTimer.Enabled)
        {
            schedulerRunCoordinator.Resume();
        }
        else
        {
            schedulerRunCoordinator.Pause();
        }
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
        Close();
        if (IsDisposed) AppBase.SetRestart();
    }

    private void SetupSchedulerActions()
    {
        schedulerShowBalloonTip = (title, message) =>
        {
            UiThreadDispatcher.SafeBeginInvoke(this, () =>
            {
                bool wasVisible = notifyIcon1.Visible;
                if (!wasVisible) notifyIcon1.Visible = true;

                if (!wasVisible)
                {
                    notifyIcon1.BalloonTipClosed += RestoreNotifyIconVisibility;
                    notifyIcon1.BalloonTipClicked += RestoreNotifyIconVisibility;
                }

                notifyIcon1.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
            });
        };

        // MessageBoxタスクはNotificationFormをモーダル表示する。
        // Invokeでスケジューラー STAスレッドをブロックし、ShowDialog のネストメッセージループ中も
        // ApplicationHostForm.WndProcは動作するため、表示中でもホットキー (WM_APPMSG_SHOWHIDE) を受け付けられる。
        //
        // owner が null のとき (launcher 内に表示フォームが無いとき) は、通知表示前の前景ウィンドウを記録し、
        // 閉じたときに SetForegroundWindow で戻す。そうしないと owner 無しダイアログの終了時に
        // Windowsが z-order 上の任意ウィンドウを前面化し、ユーザーの作業コンテキストが失われる。
        schedulerShowMessageBox = (title, message) =>
        {
            Invoke(() =>
            {
                IntPtr prevForeground = WindowHelper.GetForegroundWindowHandle();

                var form = new NotificationForm(title, message);
                activeNotifications.Add(form);
                try
                {
                    form.ShowDialogOver(GetVisibleOwner());
                }
                finally
                {
                    activeNotifications.Remove(form);
                    form.Dispose();
                }

                // launcher 内に表示フォームが無いままなら、通知表示前の前景ウィンドウへ戻す。
                // 閉じた時点で owner 候補が出現している場合 (ホットキーで CommandLauncherForm を開いた等) は、
                // WinForms の標準挙動に任せてスキップする。
                if (GetVisibleOwner() is null)
                {
                    WindowHelper.RestoreForegroundWindow(prevForeground);
                }
            });
        };
    }

    private void RestoreNotifyIconVisibility(object? sender, EventArgs e)
    {
        notifyIcon1.BalloonTipClosed -= RestoreNotifyIconVisibility;
        notifyIcon1.BalloonTipClicked -= RestoreNotifyIconVisibility;
        notifyIcon1.Visible = config.TrayIcon;
    }

    private void schedulerTimer_Tick(object sender, EventArgs e)
    {
        var now = DateTime.Now;
        foreach (var item in SchedulerScheduleEvaluator.GetItemsToRun(schedulerData, data.SchedulerLastCheckTime, now))
        {
            schedulerRunCoordinator.Request(item);
        }
        data.SchedulerLastCheckTime = now;
        data.Save(ReportSaveFailure, ConfigurationBaseName);
    }

    private void StartSchedulerItem(SchedulerItem item, Action onCompleted)
    {
        SchedulerTaskRunner.ExecuteItemTasks(this, item, schedulerShowBalloonTip, schedulerShowMessageBox,
            () => UiThreadDispatcher.SafeBeginInvoke(this, onCompleted,
                onSkipped: () => schedulerRunCoordinator.Release(item.Id)));
    }

    private void EnsureSchedulerIds()
    {
        if (!schedulerRunCoordinator.EnsureIds(schedulerData, () => schedulerData.Save(failure =>
            MessageBox.Show(GetVisibleOwner(), failure.Message
                + "\r\n識別子を保存できないため、スケジューラーの予定実行を停止しています。"
                + "\r\n書き込める状態にした後、タスクトレイの「スケジューラー設定」でOKを押すか、らんちゃを再起動すると再開します。",
                AppVersion.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning), ConfigurationBaseName)))
        {
            DiagnosticLog.Warn("Scheduler.EnsureIds", "識別子を保存できないため予定実行を停止",
                "画面の保存失敗の案内に従って書き込める状態にし、タスクトレイの「スケジューラー設定」でOKを押すか、らんちゃを再起動すると予定実行を再開する");
        }
    }

    /// <summary>
    /// 環境変数変更通知 (WM_SETTINGCHANGE) を集約して処理するデバウンス Tick。
    /// レジストリからの再読み込み、プロセス環境ブロックの更新、ReplaceEnvList の再適用の順に実行する。
    /// </summary>
    private void envChangeDebounceTimer_Tick(object? sender, EventArgs e)
    {
        envChangeDebounceTimer.Stop();

        bool changed;
        try
        {
            changed = envRefresher.Refresh();
        }
        catch (Exception ex) when (ex is IOException
                                || ex is System.Security.SecurityException
                                || ex is UnauthorizedAccessException)
        {
            DiagnosticLog.Error("Env.Reload", ex, "対応不要（環境変数の再読込を中止し、以前の値で処理を継続した）。変更した環境変数を反映するには、タスクトレイの「再起動」でらんちゃを再起動する");
            return;
        }
        if (!changed) return;

        RequestEnvironmentReplacement();
    }

    public void RequestEnvironmentReplacement()
    {
        if (shuttingDown) return;
        if (replacementRunning)
        {
            replacementRequested = true;
            return;
        }
        replacementRunning = true;
        replacementRequested = false;
        var batch = EnvironmentReplacementBatch.Capture(CommandList, schedulerData);
        ReplaceEnvList.StartBackgroundReplace(config.ReplaceEnv, batch.Values, results =>
            UiThreadDispatcher.SafeBeginInvoke(this, () =>
        {
            replacementRunning = false;
            if (shuttingDown) return;
            bool changedWhileRunning = batch.Apply(CommandList, schedulerData, results);
            RefreshCommandLauncherFormCommandList();
            if (replacementRequested || changedWhileRunning)
            {
                RequestEnvironmentReplacement();
            }
        }));
    }

    private void ApplyConfig()
    {
        using (var process = Process.GetCurrentProcess())
            process.PriorityClass = ProcessLauncher.ToPriorityClass(config.ProcessPriority);
        hookManager.UpdateHotkeys(
            (config.HotKey, ResidentMessages.WM_APPMSG_SHOWHIDE),
            (config.MemoHotKey, ResidentMessages.WM_APPMSG_SHOWMEMO));

        bool enabled = config.ButtonLauncherActivation != Core.ButtonLauncherActivation.Disabled;
        if (enabled && buttonLauncherForm is null)
        {
            buttonLauncherForm = new ButtonLauncherForm(this, contextMenuStrip1);
        }
        else if (!enabled && buttonLauncherForm is not null)
        {
            buttonLauncherForm.Close();
            buttonLauncherForm.Dispose();
            buttonLauncherForm = null;
        }
    }
}
