using Launcher.Core;
using Launcher.Infrastructure;

namespace Launcher.Win32;

/// <summary>Windowsのファイルとショートカットから登録コマンドを作成する。</summary>
public static class CommandFactory
{
    /// <summary>
    /// 指定されたファイルからコマンドの初期値を生成する。
    /// </summary>
    public static Command FromFile(string file)
    {
        file = PathHelper.PathNormalize(file);
        var command = new Command();
        if (string.Equals(Path.GetExtension(file), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            // lnk
            try
            {
                using var link = new ShellLink(file);
                string targetPath = PathHelper.PathNormalize(link.TargetPath);
                string workingDirectory = PathHelper.PathNormalize(link.WorkingDirectory);
                return CreateFromShortcutProperties(
                    targetPath,
                    link.Arguments,
                    workingDirectory,
                    PathHelper.EqualsPath(Path.GetDirectoryName(targetPath) ?? string.Empty, workingDirectory),
                    link.DisplayMode);
            }
            catch (IOException)
            {
                // エラー時はそのまま↓へ。
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // ShellLinkのCOM操作失敗時もそのまま↓へ。
            }
        }
        // lnk以外
        command.Name = Path.GetFileNameWithoutExtension(file);
        command.FileName = file;
        return command;
    }

    public static Command CreateFromShortcutProperties(
        string normalizedTargetPath,
        string? arguments,
        string normalizedWorkingDirectory,
        bool workDirMatchesTargetDir,
        ShellLink.ShellLinkDisplayMode displayMode) => new()
        {
            Name = Path.GetFileNameWithoutExtension(normalizedTargetPath),
            FileName = normalizedTargetPath,
            Param = arguments ?? string.Empty,
            WorkDir = workDirMatchesTargetDir && 2 <= normalizedWorkingDirectory.Length && normalizedWorkingDirectory[1] == ':'
            ? null
            : normalizedWorkingDirectory,
            Show = displayMode switch
            {
                ShellLink.ShellLinkDisplayMode.Maximized => WindowStyle.Maximized,
                ShellLink.ShellLinkDisplayMode.Minimized => WindowStyle.Minimized,
                _ => WindowStyle.Normal,
            },
        };
}
