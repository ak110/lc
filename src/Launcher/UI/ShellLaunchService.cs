using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>起動要求の生成とShell実行を専用STAで完了させ、失敗をUIへ配送する。</summary>
public static class ShellLaunchService
{
    public static Task<bool> Start(Control owner, Func<ShellProcessStartInfo?> createRequest, Action? onStarted = null)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        StaThreadRunner.Start(() =>
        {
            bool started = ExecuteOnSta(owner, createRequest);
            if (started && onStarted is not null)
                UiThreadDispatcher.SafeBeginInvoke(owner, onStarted);
            completion.SetResult(started);
        });
        return completion.Task;
    }

    /// <summary>ランチャーが保持するコマンドの親フォルダーを専用STAで開く。</summary>
    public static Task<bool> OpenDirectory(Control owner, Command command, Config config)
    {
        var snapshot = command.Clone();
        var settings = config.Clone();
        var handle = owner.Handle;
        return Start(owner, () => Command.OpenDirectory(settings,
            FileHelper.ResolveCommandPath(snapshot.FileName), handle));
    }

    /// <summary>タスク列を実行中の専用STAから、起動が終わるまで同期実行する。</summary>
    public static bool ExecuteOnSta(Control owner, Func<ShellProcessStartInfo?> createRequest)
    {
        try
        {
            return LaunchFailureHandler.Execute(() =>
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA || !Thread.CurrentThread.IsBackground)
                    throw new InvalidOperationException("起動処理は専用STAスレッドで実行してください。");
                var request = createRequest();
                if (request is not null) ProcessLauncher.Start(request);
            }, ex => DiagnosticLog.Warn("Shell.Execute", $"起動失敗: {ex.GetType().Name}"),
            ex => UiThreadDispatcher.SafeBeginInvoke(owner, () =>
                MessageBox.Show(owner, $"起動できませんでした。対象と起動設定を確認してください。\n{ex.Message}",
                    AppVersion.Title, MessageBoxButtons.OK, MessageBoxIcon.Error)));
        }
#pragma warning disable CA1031 // 専用STA境界の想定外例外を共通レポーターへ配送する
        catch (Exception ex)
        {
            DiagnosticLog.Error("Shell.Execute", ex);
            UiThreadDispatcher.SafeBeginInvoke(owner, () => ErrorReporter.Instance.OnException(ex));
            return false;
        }
#pragma warning restore CA1031
    }
}
