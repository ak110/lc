using System.Diagnostics;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.Core;

/// <summary>
/// 単一スケジューラータスクの実行ロジック。
/// STAスレッド生成を伴う <see cref="SchedulerPresenter.ExecuteItemTasks"/> から、
/// タスク種別ごとの実行ロジックをテスト容易性のために分離した。
/// </summary>
public static class SchedulerTaskExecutor
{
    /// <summary>
    /// 単一タスクを実行する。タスク種類に応じてファイル実行またはメッセージ表示を行う。
    /// </summary>
    public static void Execute(
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
                ExecuteFileTask(task);
                break;
        }
    }

    /// <summary>
    /// ファイル実行タスク。ShellExecuteExでプログラムを起動する。
    /// </summary>
    private static void ExecuteFileTask(SchedulerTask task)
    {
        string fileName = Environment.ExpandEnvironmentVariables(task.FileName);
        string param = Environment.ExpandEnvironmentVariables(task.Param);

        string? workDir = null;
        try
        {
            workDir = Path.GetDirectoryName(fileName);
        }
#pragma warning disable CA1031 // パス解析エラーは無視して workDir=null で続行
        catch (Exception ex)
        {
            Debug.WriteLine($"作業ディレクトリ取得エラー: {ex.Message}");
        }
#pragma warning restore CA1031

        var info = new ShellProcessStartInfo
        {
            FileName = fileName,
            Arguments = param,
            WorkingDirectory = workDir,
            CreateNoWindow = false,
            ErrorDialog = true,
            WindowStyle = ProcessLauncher.ToWindowStyle(task.Show),
        };

        ProcessLauncher.Start(info, ProcessLauncher.ToPriorityClass(task.Priority));
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
