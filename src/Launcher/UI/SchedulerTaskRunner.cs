using Launcher.Core;
using Launcher.Infrastructure;

namespace Launcher.UI;

/// <summary>専用STAで予定のタスクを順番に実行する。</summary>
public static class SchedulerTaskRunner
{
    /// <summary>
    /// アイテムのタスクを逐次実行する。STAスレッドで実行し、呼出元へはすぐに戻る。
    /// タスク列 (タスク間の待機を含む) が終わると、そのスレッドで<paramref name="onCompleted"/>を1回呼ぶ。
    /// 完了はらんちゃ内のタスク列の完了であり、起動した外部アプリの終了は待たない。
    /// </summary>
    public static void ExecuteItemTasks(
        Control owner,
        SchedulerItem item,
        Action<string, string>? showBalloonTip,
        Action<string, string>? showMessageBox,
        Action? onCompleted = null)
    {
        var ownerHandle = owner.Handle;
        StaThreadRunner.Start(() =>
        {
            try
            {
                InnerExecuteTasks(owner, ownerHandle, item, showBalloonTip, showMessageBox);
            }
            finally
            {
                onCompleted?.Invoke();
            }
        });
    }

    private static void InnerExecuteTasks(
        Control owner,
        IntPtr ownerHandle,
        SchedulerItem item,
        Action<string, string>? showBalloonTip,
        Action<string, string>? showMessageBox)
    {
        foreach (var task in item.Tasks)
        {
            if (!task.Enable) continue;
            try
            {
                DiagnosticLog.Info("Scheduler.Task", "started");
                ShellLaunchService.ExecuteOnSta(owner, () =>
                {
                    var request = SchedulerTaskExecutor.Execute(task, showBalloonTip, showMessageBox);
                    if (request is not null) request.ErrorDialogParentHandle = ownerHandle;
                    return request;
                });
                DiagnosticLog.Info("Scheduler.Task", "completed");
            }
#pragma warning disable CA1031 // スケジューラータスクの例外は無視して次のタスクへ進む
            catch (Exception ex)
            {
                DiagnosticLog.Error("Scheduler.Task", ex);
            }
#pragma warning restore CA1031
            Thread.Sleep(item.SleepTimeMs);
        }
    }
}
