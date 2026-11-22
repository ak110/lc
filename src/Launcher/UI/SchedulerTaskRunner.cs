using Launcher.Core;
using Launcher.Infrastructure;

namespace Launcher.UI;

public static class SchedulerTaskRunner
{
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
#pragma warning disable CA1031
            catch (Exception ex)
            {
                DiagnosticLog.Error("Scheduler.Task", ex, "このタスクを中断し、次のタスクへ進んだ。繰り返す場合は、タスクトレイの「スケジューラー設定」で該当タスクの種類と設定値を確認する");
            }
#pragma warning restore CA1031
            Thread.Sleep(item.SleepTimeMs);
        }
    }
}
