using Launcher.Infrastructure;

namespace Launcher.Core;

/// <summary>コマンド、予定、フォルダーの起動条件を同じ規則で組み立てる。</summary>
public static class LaunchRequestBuilder
{
    public static ShellProcessStartInfo Create(
        string fileName, string arguments = "", string? workingDirectory = null,
        WindowStyle windowStyle = WindowStyle.Normal,
        ProcessPriorityLevel priority = ProcessPriorityLevel.Normal, IntPtr owner = default)
    {
        fileName = PathHelper.PathNormalize(fileName);
        string? directory = workingDirectory;
        if (string.IsNullOrEmpty(directory))
        {
            directory = Directory.Exists(fileName) ? fileName : Path.GetDirectoryName(fileName);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) directory = null;
        }
        else
        {
            directory = PathHelper.PathNormalize(directory);
        }
        return new ShellProcessStartInfo(fileName, Environment.ExpandEnvironmentVariables(arguments))
        {
            WorkingDirectory = directory,
            WindowStyle = windowStyle,
            Priority = priority,
            ErrorDialogParentHandle = owner,
        };
    }
}
