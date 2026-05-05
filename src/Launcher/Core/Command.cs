using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.Core;

[Serializable]
public class Command : ICloneable, IComparable<Command>, IComparable
{
    /// <summary>
    /// コマンド名
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 実行ファイル等へのパス
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// 実行時の引数
    /// </summary>
    public string Param { get; set; } = string.Empty;
    /// <summary>
    /// 作業ディレクトリ
    /// </summary>
    public string? WorkDir { get; set; }
    /// <summary>
    /// 表示モード
    /// </summary>
    public WindowStyle Show { get; set; } = WindowStyle.Normal;
    /// <summary>
    /// 優先度
    /// </summary>
    public ProcessPriorityLevel Priority { get; set; } = ProcessPriorityLevel.Normal;
    /// <summary>
    /// 管理者権限で実行
    /// </summary>
    public bool RunAsAdmin { get; set; }

    /// <summary>
    /// アイコンのインデックス
    /// </summary>
    [XmlIgnore]
    public int IconIndex { get; set; } = -1;

    /// <summary>
    /// 複製の作成
    /// </summary>
    public Command Clone()
    {
        Command copy = (Command)MemberwiseClone();
        return copy;
    }

    #region ICloneable メンバ

    object ICloneable.Clone()
    {
        return Clone();
    }

    #endregion

    #region IComparable<Command> メンバ

    public int CompareTo(Command? other)
    {
        if (other is null) return 1;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    #endregion

    #region IComparable メンバ

    public int CompareTo(object? obj)
    {
        return CompareTo(obj as Command);
    }

    #endregion

    /// <summary>
    /// 親ディレクトリを開く。
    /// </summary>
    public void OpenDirectory(Config config)
    {
        string? path = FileHelper.ResolveCommandPath(FileName);
        if (File.Exists(path) || Directory.Exists(path))
        {
            var info = new ProcessStartInfo();
            info.FileName = PathHelper.PathNormalize(config.OpenParentFiler);
            info.Arguments = Environment.ExpandEnvironmentVariables(
                $"{config.OpenParentFilerParam1}{path}{config.OpenParentFilerParam2}");
            using Process? p = Process.Start(info);
        }
        else
        {
            while (!string.IsNullOrEmpty(path))
            {
                path = Path.GetDirectoryName(path);
                if (path is not null && Directory.Exists(path))
                {
                    InnerOpenExistsDirectory(config, path);
                    break;
                }
            }
        }
    }

    private static void InnerOpenExistsDirectory(Config config, string path)
    {
        var info = new ProcessStartInfo();
        if (config.OpenDirByFiler)
        {
            info.FileName = PathHelper.PathNormalize(config.Filer);
            info.Arguments = path;
            using Process? p = Process.Start(info);
        }
        else
        {
            info.FileName = path;
            using Process? p = Process.Start(info);
        }
    }

    /// <summary>
    /// コマンドの実行
    /// </summary>
    public void Execute(string input, Config config, IntPtr owner)
    {
        string? args;
        if (string.IsNullOrEmpty(input))
        {
            args = "";
        }
        else
        {
            string commandName;
            ParseInput(input, config, out commandName, out args);
        }

        string fileName = PathHelper.PathNormalize(FileName);
        string? workDir;
        if (string.IsNullOrEmpty(WorkDir))
        {
            if (Directory.Exists(fileName))
            {
                workDir = fileName;
            }
            else
            {
                string? dir = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(dir) &&
                    Directory.Exists(dir))
                {
                    workDir = dir;
                }
                else
                {
                    workDir = null; // 未指定とする
                }
            }
        }
        else
        {
            workDir = PathHelper.PathNormalize(WorkDir);
        }

        string param;
        if (config.OpenDirByFiler && Directory.Exists(fileName))
        {
            // フォルダをファイラで開く
            param = fileName;
            fileName = PathHelper.PathNormalize(config.Filer);
        }
        else
        {
            // 通常処理
            param = Environment.ExpandEnvironmentVariables($"{Param} {args}");
        }

        var info = new ShellProcessStartInfo();
        info.FileName = fileName;
        info.Arguments = param;
        info.WorkingDirectory = workDir;
        info.CreateNoWindow = false;
        info.ErrorDialog = true;
        info.ErrorDialogParentHandle = owner;
        if (RunAsAdmin && !NativeMethods.IsUserAnAdmin())
        {
            ApplyAdminElevation(info, config.RunAsAdminType, config.RunAsCommandLine, config.VECmdPath);
        }

        info.WindowStyle = ProcessLauncher.ToWindowStyle(Show);
        ProcessLauncher.Start(info, ProcessLauncher.ToPriorityClass(Priority));
    }

    /// <summary>
    /// 管理者権限での実行方法に応じて <see cref="ShellProcessStartInfo"/> を加工する。
    /// </summary>
    /// <remarks>
    /// 呼び出し元は事前に「現在のユーザーが管理者ではないが管理者権限が必要」な状況であることを確認すること。
    /// </remarks>
    /// <param name="info">加工対象の起動情報。<see cref="ShellProcessStartInfo.FileName"/> および
    /// <see cref="ShellProcessStartInfo.Arguments"/> は呼び出し前に設定済みであること。</param>
    /// <param name="elevation">管理者権限への昇格方法。</param>
    /// <param name="runAsCommandLine"><see cref="AdminElevation.RunAsCommand"/> 時に runas へ渡す引数雛形。</param>
    /// <param name="vECmdPath"><see cref="AdminElevation.VistaElevator"/> 時に呼び出すエレベーター実行ファイルのパス。</param>
    internal static void ApplyAdminElevation(
        ShellProcessStartInfo info,
        AdminElevation elevation,
        string runAsCommandLine,
        string vECmdPath)
    {
        switch (elevation)
        {
            default:
            case AdminElevation.RunAs:
                info.Verb = "runas";
                break;

            case AdminElevation.RunAsCommand: // runasコマンド
                info.Arguments = $"{runAsCommandLine} \"\\\"{info.FileName}\\\" {info.Arguments!.Replace("\"", "\\\"")}\"";
                info.FileName = "runas";
                break;

            case AdminElevation.VistaElevator: // Vistaのエレベータ
                char delim = '/';
                foreach (char c in ",;*?<>|")
                {
                    if (info.FileName!.IndexOf(delim) < 0 &&
                        info.Arguments!.IndexOf(delim) < 0) break;
                    delim = c;
                }
                info.Arguments = $"0{delim}{info.FileName}{delim}{info.Arguments}{delim}{delim}";
                info.FileName = PathHelper.PathNormalize(vECmdPath);
                break;
        }
    }

    // 後方互換性のためのレガシーフォーマット読み込み用
    private static readonly string[] LineSeparators = ["\r\n", "\n"];

    public static Command LoadFrom(string? name, string data)
    {
        var cmd = new Command();
        string[] list = data.Split(LineSeparators, StringSplitOptions.None);
        int i = 0;
        cmd.Name = name ?? list[i++];
        cmd.FileName = list[i++];
        cmd.Param = list[i++];
        cmd.WorkDir = list[i++];
        int showVal;
        if (!int.TryParse(list[i++], out showVal)) showVal = 0;
        cmd.Show = (WindowStyle)showVal;
        int priorityVal;
        if (!int.TryParse(list[i++], out priorityVal)) priorityVal = 3;
        if (5 <= priorityVal)
        {
            priorityVal = 5;
        }
        cmd.Priority = (ProcessPriorityLevel)priorityVal;
        return cmd;
    }

    /// <summary>
    /// 入力文字列とコマンド名とを比較し、完全一致したらtrue
    /// </summary>
    public bool IsMatch(string input, Config config)
    {
        string commandName;
        string? arguments;
        return ParseInput(input, config, out commandName, out arguments);
    }

    /// <summary>
    /// 入力文字列を、コマンド名と引数に分ける。
    /// </summary>
    /// <returns>コマンド名が一致した場合は true。false の場合の戻り値は信頼できない。</returns>
    public bool ParseInput(string input, Config config, out string commandName, out string? arguments)
    {
        return CommandMatcher.ParseInput(Name, input, config, out commandName, out arguments);
    }

    /// <summary>
    /// コマンド名と比較し、一致した長さに応じた点数を返す。
    /// </summary>
    public int GetMatchScore(string input, Config config)
    {
        return CommandMatcher.GetMatchScore(Name, input, config);
    }

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
