using Launcher.Infrastructure;

namespace Launcher.Core;

/// <summary>
/// スケジューラーのオーケストレーション層。
/// 走査対象アイテムの判定 (<see cref="SchedulerScheduleEvaluator"/> 経由) と、
/// 実行スケジュールに沿った STA スレッドの起動 (<see cref="SchedulerTaskExecutor"/> 経由) を担う。
/// </summary>
public static class SchedulerPresenter
{
    /// <summary>
    /// 実行対象のアイテムを取得する。
    /// </summary>
    public static List<SchedulerItem> GetItemsToRun(SchedulerData data, DateTime lastCheckTime, DateTime now)
    {
        var result = new List<SchedulerItem>();
        foreach (var item in data.Items)
        {
            if (ShouldItemRun(item, now, lastCheckTime))
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// アイテムが実行すべきか判定する。
    /// </summary>
    public static bool ShouldItemRun(SchedulerItem item, DateTime now, DateTime last)
    {
        if (!item.Enable) return false;
        foreach (var schedule in item.Schedules)
        {
            if (IsScheduleActive(schedule, now, last))
                return true;
        }
        return false;
    }

    /// <summary>
    /// スケジュールが last～now の間に発火すべきか判定する。
    /// last～now 間の全ての日を走査し、日付条件と時刻条件の両方を満たす時刻があるか確認する。
    /// </summary>
    public static bool IsScheduleActive(Schedule schedule, DateTime now, DateTime last)
    {
        if (!schedule.Enable) return false;

        // last～now 間の全日を走査
        var startDate = DateOnly.FromDateTime(last);
        var endDate = DateOnly.FromDateTime(now);
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (SchedulerScheduleEvaluator.CheckDateAndTime(schedule, date, now, last))
                return true;
        }
        return false;
    }

    /// <summary>
    /// アイテムのタスクを逐次実行する。STAスレッドで実行し、呼出元へはすぐに戻る。
    /// タスク列 (タスク間の待機を含む) が終わると、そのスレッドで<paramref name="onCompleted"/>を1回呼ぶ。
    /// 完了はらんちゃ内のタスク列の完了であり、起動した外部アプリの終了は待たない。
    /// </summary>
    public static void ExecuteItemTasks(
        SchedulerItem item,
        Action<string, string>? showBalloonTip,
        Action<string, string>? showMessageBox,
        Action? onCompleted = null)
    {
        var thread = new Thread(() =>
        {
            try
            {
                InnerExecuteTasks(item, showBalloonTip, showMessageBox);
            }
            finally
            {
                onCompleted?.Invoke();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
    }

    private static void InnerExecuteTasks(
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
                SchedulerTaskExecutor.Execute(task, showBalloonTip, showMessageBox);
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
