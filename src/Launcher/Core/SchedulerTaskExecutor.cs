using Launcher.Infrastructure;

namespace Launcher.Core;

public static class SchedulerTaskExecutor
{
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

    public static ShellProcessStartInfo ExecuteFileTask(SchedulerTask task)
    {
        return LaunchRequestBuilder.Create(task.FileName, task.Param,
            windowStyle: task.Show, priority: task.Priority);
    }

    private static void ExecuteBalloonTipTask(SchedulerTask task, Action<string, string>? showBalloonTip)
    {
        string message = Environment.ExpandEnvironmentVariables(task.Message);
        showBalloonTip?.Invoke(AppVersion.Title, message);
    }

    private static void ExecuteMessageBoxTask(SchedulerTask task, Action<string, string>? showMessageBox)
    {
        string message = Environment.ExpandEnvironmentVariables(task.Message);
        showMessageBox?.Invoke(AppVersion.Title, message);
    }
}
