#nullable disable
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
    public string Name { get; set; }
    /// <summary>
    /// 実行ファイル等へのパス
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// 実行時の引数
    /// </summary>
    public string Param { get; set; }
    /// <summary>
    /// 作業ディレクトリ
    /// </summary>
    public string WorkDir { get; set; }
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
    [NonSerialized]
    [XmlIgnore]
    public int IconIndex = -1;

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

    public int CompareTo(Command other)
    {
        if (other == null) return 1;
        if (Name == null && other.Name == null) return 0;
        if (Name == null) return -1;
        if (other.Name == null) return 1;
        return Name.CompareTo(other.Name);
    }

    #endregion

    #region IComparable メンバ

    public int CompareTo(object obj)
    {
        return CompareTo(obj as Command);
    }

    #endregion

    /// <summary>
    /// 親ディレクトリを開く。
    /// </summary>
    public void OpenDirectory(Config config)
    {
        string path = PathHelper.PathNormalize(FileName);
        if (File.Exists(path) || Directory.Exists(path))
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = PathHelper.PathNormalize(config.OpenParentFiler);
            info.Arguments = Environment.ExpandEnvironmentVariables(
                config.OpenParentFilerParam1 +
                path +
                config.OpenParentFilerParam2);
            using (Process p = Process.Start(info)) { }
        }
        else
        {
            while (!string.IsNullOrEmpty(path))
            {
                path = Path.GetDirectoryName(path);
                if (Directory.Exists(path))
                {
                    InnerOpenExistsDirectory(config, path);
                    break;
                }
            }
        }
    }

    private static void InnerOpenExistsDirectory(Config config, string path)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        if (config.OpenDirByFiler)
        {
            info.FileName = PathHelper.PathNormalize(config.Filer);
            info.Arguments = path;
            using (Process p = Process.Start(info)) { }
        }
        else
        {
            info.FileName = path;
            using (Process p = Process.Start(info)) { }
        }
    }

    /// <summary>
    /// コマンドの実行
    /// </summary>
    public void Execute(string input, Config config, IntPtr owner)
    {
        string args;
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
        string workDir;
        if (string.IsNullOrEmpty(WorkDir))
        {
            if (Directory.Exists(fileName))
            {
                workDir = fileName;
            }
            else
            {
                string dir = Path.GetDirectoryName(fileName);
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
            param = Environment.ExpandEnvironmentVariables(Param + " " + args);
        }

        ShellProcessStartInfo info = new ShellProcessStartInfo();
        info.FileName = fileName;
        info.Arguments = param;
        info.WorkingDirectory = workDir;
        info.CreateNoWindow = false;
        info.ErrorDialog = true;
        info.ErrorDialogParentHandle = owner;
        if (RunAsAdmin && !NativeMethods.IsUserAnAdmin())
        {
            switch (config.RunAsAdminType)
            {
                default:
                case AdminElevation.RunAs: info.Verb = "runas"; break;
                case AdminElevation.RunAsCommand: // runasコマンド
                    info.Arguments = string.Format("{0} \"\\\"{1}\\\" {2}\"",
                        config.RunAsCommandLine,
                        info.FileName,
                        info.Arguments.Replace("\"", "\\\""));
                    info.FileName = "runas";
                    break;

                case AdminElevation.VistaElevator: // Vistaのエレベータ
                    char delim = '/';
                    foreach (char c in ",;*?<>|")
                    {
                        if (info.FileName.IndexOf(delim) < 0 &&
                            info.Arguments.IndexOf(delim) < 0) break;
                        delim = c;
                    }
                    info.Arguments = string.Format("0{0}{1}{0}{2}{0}{0}", delim, info.FileName, info.Arguments);
                    info.FileName = PathHelper.PathNormalize(config.VECmdPath);
                    break;
            }
        }

        // WindowStyle列挙型からShellProcessWindowStyleへの変換
        info.WindowStyle = Show switch
        {
            WindowStyle.Normal => ShellProcessWindowStyle.Normal,
            WindowStyle.Minimized => ShellProcessWindowStyle.Minimized,
            WindowStyle.Maximized => ShellProcessWindowStyle.Maximized,
            WindowStyle.NoActivate => ShellProcessWindowStyle.NoActivate,
            WindowStyle.MinimizedNoActivate => ShellProcessWindowStyle.MinimizedNoActivate,
            WindowStyle.Hidden => ShellProcessWindowStyle.Hidden,
            _ => ShellProcessWindowStyle.Normal,
        };

        // ProcessPriorityLevel列挙型からProcessPriorityClassへの変換
        ProcessPriorityClass priorityClass = Priority switch
        {
            ProcessPriorityLevel.RealTime => ProcessPriorityClass.RealTime,
            ProcessPriorityLevel.High => ProcessPriorityClass.High,
            ProcessPriorityLevel.AboveNormal => ProcessPriorityClass.AboveNormal,
            ProcessPriorityLevel.Normal => ProcessPriorityClass.Normal,
            ProcessPriorityLevel.BelowNormal => ProcessPriorityClass.BelowNormal,
            ProcessPriorityLevel.Idle => ProcessPriorityClass.Idle,
            _ => ProcessPriorityClass.Normal,
        };
        ProcessLauncher.Start(info, priorityClass);
    }

    /// <summary>
    /// 後方互換性のための処理
    /// </summary>
    private static readonly string[] LineSeparators = ["\r\n", "\n"];

    public static Command LoadFrom(string name, string data)
    {
        Command cmd = new Command();
        string[] list = data.Split(LineSeparators, StringSplitOptions.None);
        int i = 0;
        if (name == null)
        {
            cmd.Name = list[i++];
        }
        else
        {
            cmd.Name = name;
        }
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
        string commandName, arguments;
        return ParseInput(input, config, out commandName, out arguments);
    }

    /// <summary>
    /// 入力文字列を、コマンド名と引数に分ける。
    /// </summary>
    /// <returns>コマンド名が一致した場合はtrue。falseだと割と適当な結果が返る。</returns>
#nullable enable
    public bool ParseInput(string input, Config config, out string commandName, out string? arguments)
#nullable disable
    {
        return CommandMatcher.ParseInput(Name, input, config, out commandName, out arguments);
    }

    /// <summary>
    /// コマンド名と比較し、一致した長さに応じた点数を返す
    /// </summary>
    public int GetMatchScore(string input, Config config)
    {
        return CommandMatcher.GetMatchScore(Name, input, config);
    }

#if DEBUG
    static Command()
    {
        Config config = new Config();
        Command cmd = new Command();
        cmd.Name = "htdocs";
        int n = cmd.GetMatchScore("docs", config);
        Debug.Assert(n != 0);
    }
#endif

    /// <summary>
    /// 指定されたファイルからコマンドのデフォルトを適当に作成。
    /// </summary>
    public static Command FromFile(string file)
    {
        file = PathHelper.PathNormalize(file);
        Command command = new Command();
        if (string.Equals(Path.GetExtension(file), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            // lnk
            try
            {
                using (ShellLink link = new ShellLink(file))
                {
                    string targetPath = PathHelper.PathNormalize(link.TargetPath);
                    string workingDirectory = PathHelper.PathNormalize(link.WorkingDirectory);
                    command.Name = Path.GetFileNameWithoutExtension(targetPath);
                    command.FileName = targetPath;
                    command.Param = link.Arguments;
                    command.WorkDir =
                        PathHelper.EqualsPath(
                        Path.GetDirectoryName(targetPath),
                        workingDirectory) &&
                        2 <= workingDirectory.Length &&
                        workingDirectory[1] == ':' ? null : workingDirectory;
                    switch (link.DisplayMode)
                    {
                        case ShellLink.ShellLinkDisplayMode.Maximized:
                            command.Show = WindowStyle.Maximized;
                            break;
                        case ShellLink.ShellLinkDisplayMode.Minimized:
                            command.Show = WindowStyle.Minimized;
                            break;
                        case ShellLink.ShellLinkDisplayMode.Normal:
                        default:
                            command.Show = WindowStyle.Normal;
                            break;
                    }
                    return command;
                }
            }
            catch
            {
                // エラー時はそのまま↓へ。
            }
        }
        // lnk以外
        command.Name = Path.GetFileNameWithoutExtension(file);
        command.FileName = file;
        return command;
    }
}
