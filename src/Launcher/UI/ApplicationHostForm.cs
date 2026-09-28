using System.Diagnostics;
using System.Runtime.InteropServices;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Updater;
using Launcher.Win32;
using Microsoft.Win32;
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

    /// <summary>スケジューラーの予定実行の管理 (同じアイテムの重複防止と予定の集約)</summary>
    readonly SchedulerRunCoordinator schedulerRunCoordinator;

    /// <summary>ShowHide 再入防止フラグ (DoEvents 経由の多重呼び出しを防ぐ)</summary>
    bool showHideInProgress;

    /// <summary>現在表示中の非同期通知ダイアログの追跡リスト (全てUIスレッドから操作する)</summary>
    readonly List<Form> activeNotifications = [];

    /// <summary>
    /// 非同期通知ダイアログが1つ以上表示中か。
    /// CommandLauncherFormの auto-hide 抑制判定 (CommandLauncherForm_Deactivate/Leave) で参照する。
    /// </summary>
    public bool HasActiveNotifications => activeNotifications.Exists(f => !f.IsDisposed);

    /// <summary>環境変数変更の自動取り込み用 (WM_SETTINGCHANGE)</summary>
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

    /// <summary>
    /// commandLauncherForm が破棄されていなければ <paramref name="action"/> を実行する。
    /// IsDisposed ガードと操作呼び出しの繰り返しを集約する。
    /// </summary>
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
        if (!commandLauncherForm.IsDisposed && commandLauncherForm.Visible)
            return commandLauncherForm;

        // ConfigForm など launcher 内のモーダル表示中フォームを owner に使う。
        // Form.ActiveForm は現在アクティブな自プロセスのフォームを返す。
        // ApplicationHostForm 自身は Visible=false のため候補から除外される。
        if (Form.ActiveForm is { IsDisposed: false, Visible: true } active && active != this)
            return active;

        return null;
    }

    public ApplicationHostForm()
    {
        InitializeComponent();
        Visible = false;

        var screenRect = new Rectangle();
        foreach (Screen s in Screen.AllScreens) { screenRect = Rectangle.Union(screenRect, s.Bounds); }
        Location = new Point(screenRect.Left - Size.Width, screenRect.Top - Size.Height);

        // 設定ファイルの読み込み
        // 読めないファイルは通知し、復元しなければ初期値で起動する (そのファイルへの保存は止まる)
        var configResult = Config.Load();
        config = AcceptLoadResult(configResult, Config.Store, configResult.Value);
        var commandResult = CommandList.Load(".cmd.cfg");
        CommandList = AcceptLoadResult(commandResult, CommandList.Store(".cmd.cfg"), commandResult.Value);
        var buttonResult = ButtonLauncherData.Load();
        ButtonLauncherData = AcceptLoadResult(buttonResult, ButtonLauncherData.Store, buttonResult.Value);
        var memoResult = MemoData.Load();
        MemoData = AcceptLoadResult(memoResult, MemoData.Store, memoResult.Value);
        var schedulerResult = SchedulerData.Load();
        schedulerData = AcceptLoadResult(schedulerResult, SchedulerData.Store, schedulerResult.Value);
        new ReplaceEnvList(config.ReplaceEnv).Replace(schedulerData);
        try { data = Data.Deserialize(); } catch (IOException) { } catch (InvalidOperationException) { }

        notifyIcon1.Text = Infrastructure.AppVersion.Title;
        notifyIcon1.Visible = config.TrayIcon;

        data.WindowHandle = Handle.ToInt64();
        data.Serialize();

        hookManager = new HookManager(() => config, () => Handle, a => BeginInvoke(a));

        commandLauncherForm = new CommandLauncherForm(this, contextMenuStrip1);
        commandLauncherForm.PreInitialize();
        if (!config.HideFirst)
        {
            commandLauncherForm.Show(this);
        }
        ApplyConfig();
        SetupSchedulerActions();
        schedulerRunCoordinator = new SchedulerRunCoordinator(() => schedulerData.Items, StartSchedulerItem);
        EnsureSchedulerIds();

        // 起動時の自動更新チェックは無効化 (手動で実行する)
    }

    /// <summary>
    /// 非アクティブに表示
    /// </summary>
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
        schedulerTimer.Stop();
        schedulerRunCoordinator.Shutdown();
        hookManager.Unregister();
        notifyIcon1.Dispose();

        // メモ内容のデバウンス保存が残っていれば最終保存する
        if (memoForm is { IsDisposed: false })
        {
            memoForm.FlushPendingSave();
        }

        data.WindowHandle = 0;
        data.Serialize();
    }

    /// <summary>
    /// WndProc。
    /// </summary>
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
                        HideMemoIfVisible();
                        buttonLauncherForm?.ShowLauncher();
                    }
                    else if (m.LParam == Program.WM_APPMSG_SHOWMEMO)
                    {
                        ShowHideMemo();
                    }
                }
                // WndProc内の例外ハンドラ: WinFormsのメッセージループの最終防御ラインのため全例外を捕捉する
#pragma warning disable CA1031 // WndProcはメッセージループの最終防御ライン
                catch (Exception ex)
                {
                    DiagnosticLog.Error("Host.HandleMessage", ex);
                    MessageBox.Show($"メッセージ処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                        "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
#pragma warning restore CA1031
            }
        }
        base.WndProc(ref m);
    }

    /// <summary>
    /// メインウィンドウの表示と非表示を切り替える。
    /// 非同期通知ダイアログが表示中のときは、CommandLauncherFormをHideせず、通知Formを最前面化する。
    /// </summary>
    public void ShowHide()
    {
        if (showHideInProgress) return;
        showHideInProgress = true;
        try
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
                // 通常のトグル: 表示中→非表示
                commandLauncherForm.HideWindow();
            }
            // 通知がある場合はHideせず、表示を維持する

            // 追跡中の通知ダイアログを最前面にアクティブ化する (DoEventsを伴わない軽量版)
            foreach (var form in activeNotifications)
            {
                if (!form.IsDisposed)
                {
                    form.Activate();
                    form.BringToFront();
                }
            }
        }
        finally
        {
            showHideInProgress = false;
        }
    }

    /// <summary>
    /// メモパッドが表示中なら閉じる。ランチャー起動と連動して呼ばれる。
    /// </summary>
    void HideMemoIfVisible()
    {
        if (memoForm is { IsDisposed: false })
        {
            memoForm.HideIfVisible();
        }
    }

    /// <summary>
    /// メモパッドの表示と非表示を切り替える。未生成なら生成する。
    /// </summary>
    public void ShowHideMemo()
    {
        if (memoForm is null || memoForm.IsDisposed)
        {
            memoForm = new MemoForm(this);
        }
        memoForm.ToggleVisible();
    }

    /// <summary>
    /// らんちゃ.cmd.cfgのリロード
    /// </summary>
    public void Reload()
    {
        var stopwatch = Stopwatch.StartNew();
        // 読めないファイルは通知し、復元しなければ保持中のデータを使い続ける
        CommandList = AcceptLoadResult(CommandList.Load(".cmd.cfg"), CommandList.Store(".cmd.cfg"), CommandList);
        ButtonLauncherData = AcceptLoadResult(ButtonLauncherData.Load(), ButtonLauncherData.Store, ButtonLauncherData);
        schedulerData = AcceptLoadResult(SchedulerData.Load(), SchedulerData.Store, schedulerData);
        schedulerRunCoordinator.ItemsChanged();
        EnsureSchedulerIds();
        var data = schedulerData;
        ReplaceEnvList.StartBackgroundReplace(config.ReplaceEnv, rep => rep.Replace(data));
        IfCommandLauncherFormAlive(form => form.ApplyConfig());
        DiagnosticLog.Info("Host.Reload", $"elapsed={stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 読込結果を受け取る。失敗していれば通知し、バックアップがあれば復元を選べるようにする。
    /// 復元したらそのデータを、復元しなければ<paramref name="fallback"/>を返す。
    /// </summary>
    static T AcceptLoadResult<T>(ConfigLoadResult<T> result, ConfigFile<T> store, T fallback)
        where T : ConfigStore, new()
    {
        if (result.Status != ConfigLoadStatus.Failed)
        {
            return result.Value;
        }
        string text = $"設定ファイル({result.Kind})を読み込めませんでした。\r\n原因: {result.Error?.Message}\r\n\r\n"
            + "原本を上書きしないよう、このファイルへの保存を停止しています。";
        if (!result.BackupAvailable)
        {
            MessageBox.Show(text + "\r\nファイルを直してから再起動してください。", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return fallback;
        }
        var answer = MessageBox.Show(
            text + "\r\n\r\n前回正常に読み込めた内容のバックアップから復元しますか？\r\n"
                + "(読み込めなかったファイルは「.broken-日時」を付けた名前で残します)",
            AppVersion.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
        {
            return fallback;
        }
        try
        {
            return store.RestoreFromBackup();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or InvalidDataException or System.Xml.XmlException)
        {
            DiagnosticLog.Warn("Config.Restore", $"復元失敗: {result.Kind} {ex.GetType().Name}");
            MessageBox.Show($"復元できませんでした。保存の停止は続きます。\r\n原因: {ex.Message}", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return fallback;
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
        form.ShowDialogOver(GetVisibleOwner());
    }

    /// <summary>
    /// CommandLauncherFormの表示を更新
    /// </summary>
    public void RefreshCommandLauncherForm()
    {
        IfCommandLauncherFormAlive(form => form.ApplyConfig());
    }

    /// <summary>
    /// CommandLauncherFormのコマンド一覧表示だけを更新 (アイコン再読込なし)
    /// </summary>
    public void RefreshCommandLauncherFormCommandList()
    {
        IfCommandLauncherFormAlive(form => form.RefreshCommandList());
    }

    /// <summary>
    /// コンフィグダイアログ。
    /// </summary>
    public void ShowConfigDialog()
    {
        using var form = new ConfigForm(config, ButtonLauncherData);
        if (form.ShowDialogOver(GetVisibleOwner()) == DialogResult.OK)
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
            if (!GitHubUpdateClient.IsUpdateAvailable(release))
            {
                MessageBox.Show("現在のバージョンは最新です。", AppVersion.Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var form = new UpdateForm(release!);
            form.ShowDialogOver(GetVisibleOwner());
            // UpdateForm 内でバッチ起動と Environment.Exit() を実行するため、ここに到達するのはキャンセル時のみ。
        }
#pragma warning disable CA1031 // ネットワーク更新は様々な例外が発生しうるため包括的にキャッチ
        catch (Exception ex)
        {
            MessageBox.Show($"更新チェックに失敗しました。\n{ex.Message}", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
#pragma warning restore CA1031
    }

    private void ホームページを開くHToolStripMenuItem_Click(object sender, EventArgs e)
    {
        const string url = "https://github.com/ak110/lc";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
#pragma warning disable CA1031 // ブラウザ起動は様々な例外が発生しうるため包括的にキャッチ
        catch (Exception ex)
        {
            MessageBox.Show($"ブラウザの起動に失敗しました。\n{ex.Message}", AppVersion.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
#pragma warning restore CA1031
    }

    private void スケジューラー設定SToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var form = new SchedulerConfigForm(schedulerData, schedulerShowBalloonTip, schedulerShowMessageBox);
        if (form.ShowDialogOver(GetVisibleOwner()) == DialogResult.OK)
        {
            schedulerData = form.Value;
            schedulerRunCoordinator.ItemsChanged();
            EnsureSchedulerIds();
            schedulerData.Serialize();
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
        AppBase.SetRestart();
        Close();
    }

    /// <summary>
    /// スケジューラーのメッセージ表示アクションをフィールドに設定する。
    /// </summary>
    private void SetupSchedulerActions()
    {
        schedulerShowBalloonTip = (title, message) =>
        {
            BeginInvoke(() =>
            {
                // トレイアイコンが非表示の場合、一時的に表示してバルーン通知を表示する
                bool wasVisible = notifyIcon1.Visible;
                if (!wasVisible) notifyIcon1.Visible = true;

                // バルーンが閉じたらトレイアイコンの表示を元に戻す
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

    /// <summary>
    /// バルーン通知後にトレイアイコンの表示を元の設定に戻す。
    /// </summary>
    private void RestoreNotifyIconVisibility(object? sender, EventArgs e)
    {
        notifyIcon1.BalloonTipClosed -= RestoreNotifyIconVisibility;
        notifyIcon1.BalloonTipClicked -= RestoreNotifyIconVisibility;
        notifyIcon1.Visible = config.TrayIcon;
    }

    /// <summary>
    /// スケジューラーのタイマーTick。スケジュール判定→タスク実行→LastCheckTime更新。
    /// </summary>
    private void schedulerTimer_Tick(object sender, EventArgs e)
    {
        var now = DateTime.Now;
        // 別アイテムは並行して開始する。実行中のアイテムへの予定は保留にまとめ、完了後に1回実行する
        foreach (var item in SchedulerPresenter.GetItemsToRun(schedulerData, data.SchedulerLastCheckTime, now))
        {
            schedulerRunCoordinator.Request(item);
        }
        // 実行対象の有無によらずLastCheckTimeを前進させ、同じ予定を二重に数えない
        data.SchedulerLastCheckTime = now;
        data.Serialize();
    }

    /// <summary>
    /// 予定実行の開始。タスク列の完了をUIスレッドへ配送し、配送できなければ実行状態だけを解放する。
    /// </summary>
    private void StartSchedulerItem(SchedulerItem item, Action onCompleted)
    {
        SchedulerPresenter.ExecuteItemTasks(item, schedulerShowBalloonTip, schedulerShowMessageBox,
            () => UiThreadDispatcher.SafeBeginInvoke(this, onCompleted,
                onSkipped: () => schedulerRunCoordinator.Release(item.Id)));
    }

    /// <summary>
    /// 識別子の無いスケジューラーアイテムへ識別子を補って保存する。保存できなければ予定実行を止めて通知する。
    /// </summary>
    private void EnsureSchedulerIds()
    {
        if (!schedulerRunCoordinator.EnsureIds(schedulerData, schedulerData.Serialize))
        {
            MessageBox.Show(
                "スケジューラー設定(.sch.cfg)へアイテムの識別子を保存できなかったため、スケジューラーの予定実行を停止しています。\r\n"
                    + "ファイルを書き込める状態にしてから、らんちゃを再起動してください。",
                AppVersion.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            // レジストリの読み込みに失敗してもアプリ本体の動作は継続する。
            DiagnosticLog.Error("Env.Reload", ex);
            return;
        }
        if (!changed) return;

        // ReplaceEnvList 側は static ロックで直列化される。
        ReplaceEnvList.StartBackgroundReplace(config.ReplaceEnv, rep =>
        {
            rep.Replace(CommandList);
            rep.Replace(schedulerData);
            try
            {
                BeginInvoke(() =>
                {
                    IfCommandLauncherFormAlive(form => form.RefreshCommandList());
                });
            }
            catch (InvalidOperationException)
            {
                // フォーム破棄済み (ObjectDisposedException を含む)
            }
        });
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
        // ホットキー (ランチャー用・メモパッド用の2組)
        hookManager.UpdateHotkeys(
            (config.HotKey, Program.WM_APPMSG_SHOWHIDE),
            (config.MemoHotKey, Program.WM_APPMSG_SHOWMEMO));

        // ボタンランチャーの生成・破棄
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
