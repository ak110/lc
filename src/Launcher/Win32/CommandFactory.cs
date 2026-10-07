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
                command.Name = Path.GetFileNameWithoutExtension(targetPath);
                command.FileName = targetPath;
                command.Param = link.Arguments ?? string.Empty;
                command.WorkDir =
                    PathHelper.EqualsPath(
                    Path.GetDirectoryName(targetPath) ?? string.Empty,
                    workingDirectory) &&
                    2 <= workingDirectory.Length &&
                    workingDirectory[1] == ':' ? null : workingDirectory;
                command.Show = link.DisplayMode switch
                {
                    ShellLink.ShellLinkDisplayMode.Maximized => WindowStyle.Maximized,
                    ShellLink.ShellLinkDisplayMode.Minimized => WindowStyle.Minimized,
                    _ => WindowStyle.Normal,
                };
                return command;
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
}
