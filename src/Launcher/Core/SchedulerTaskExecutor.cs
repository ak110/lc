using Launcher.Infrastructure;

namespace Launcher.Core;

/// <summary>
/// 単一スケジューラータスクの実行ロジック。
/// ファイルタスクは起動要求を返し、通知タスクは渡された通知処理へ委譲する。
/// </summary>
public static class SchedulerTaskExecutor
{
    /// <summary>
    /// 単一タスクを実行する。タスク種類に応じてファイル実行またはメッセージを表示する。
    /// </summary>
    public static ShellProcessStartInfo? Execute(
        SchedulerTask task,
        Action<string, string>? showBalloonTip,
        Action<string, string>? showMessageBox)
    {
        switch (task.Type)
        {
            case SchedulerTaskType.BalloonTip:
                ExecuteBalloonTipTask(task, showBalloonTip);
                break;
            case SchedulerTaskType.MessageBox:
                ExecuteMessageBoxTask(task, showMessageBox);
                break;
            default:
                return ExecuteFileTask(task);
        }
        return null;
    }

    /// <summary>
    /// ファイル実行タスク。ShellExecuteExでプログラムを起動する。
    /// </summary>
    public static ShellProcessStartInfo ExecuteFileTask(SchedulerTask task)
    {
        return LaunchRequestBuilder.Create(task.FileName, task.Param,
            windowStyle: task.Show, priority: task.Priority);
    }

    /// <summary>
    /// バルーン通知タスク。デリゲート経由でUI層に委譲する。
    /// </summary>
    private static void ExecuteBalloonTipTask(SchedulerTask task, Action<string, string>? showBalloonTip)
    {
        string message = Environment.ExpandEnvironmentVariables(task.Message);
        showBalloonTip?.Invoke(AppVersion.Title, message);
    }

    /// <summary>
    /// メッセージボックスタスク。デリゲート経由でUI層に委譲する。
    /// Invoke (同期) で実行されるため、ダイアログが閉じるまでスレッドをブロックする。
    /// </summary>
    private static void ExecuteMessageBoxTask(SchedulerTask task, Action<string, string>? showMessageBox)
    {
        string message = Environment.ExpandEnvironmentVariables(task.Message);
        showMessageBox?.Invoke(AppVersion.Title, message);
    }
}
