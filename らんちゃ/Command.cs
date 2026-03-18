using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;

namespace らんちゃ {
    [Serializable]
    public class Command : ICloneable, IComparable<Command>, IComparable {
        /// <summary>
        /// コマンド名
        /// </summary>
        public string Name;
        /// <summary>
        /// 実行ファイル等へのパス
        /// </summary>
        public string FileName;
        /// <summary>
        /// 実行時の引数
        /// </summary>
        public string Param;
        /// <summary>
        /// 作業ディレクトリ
        /// </summary>
        public string WorkDir;
        /// <summary>
        /// 表示モード
        /// </summary>
        public int Show = 0;
        /// <summary>
        /// 優先度
        /// </summary>
        public int Priority = 3;
        /// <summary>
        /// 管理者権限で実行
        /// </summary>
        public bool RunAsAdmin = false;

        /// <summary>
        /// アイコンのインデックス
        /// </summary>
        [NonSerialized]
        [XmlIgnore]
        public int IconIndex = -1;

        /// <summary>
        /// 複製の作成
        /// </summary>
        public Command Clone() {
            Command copy = (Command)MemberwiseClone();
            return copy;
        }

        #region ICloneable メンバ

        object ICloneable.Clone() {
            return Clone();
        }

        #endregion

        #region IComparable<Command> メンバ

        public int CompareTo(Command other) {
            try {
                return Name.CompareTo(other.Name);
            } catch (NullReferenceException) {
                return 1; // TODO: ?
            }
        }

        #endregion

        #region IComparable メンバ

        public int CompareTo(object obj) {
            return CompareTo(obj as Command);
        }

        #endregion

        /// <summary>
        /// 親ディレクトリを開く。
        /// </summary>
        public void OpenDirectory(Config config) {
            string path = Toolkit.IO.Utility.PathNormalize(FileName);
            if (File.Exists(path) || Directory.Exists(path)) {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Toolkit.IO.Utility.PathNormalize(config.OpenParentFiler);
                info.Arguments = Environment.ExpandEnvironmentVariables(
                    config.OpenParentFilerParam1 +
                    path +
                    config.OpenParentFilerParam2);
                using (Process p = Process.Start(info)) { }
            } else {
                while (!string.IsNullOrEmpty(path)) {
                    path = Path.GetDirectoryName(path);
                    if (Directory.Exists(path)) {
                        InnerOpenExistsDirectory(config, path);
                        break;
                    }
                }
            }
        }

        private static void InnerOpenExistsDirectory(Config config, string path) {
            ProcessStartInfo info = new ProcessStartInfo();
            if (config.OpenDirByFiler) {
                info.FileName = Toolkit.IO.Utility.PathNormalize(config.Filer);
                info.Arguments = path;
                using (Process p = Process.Start(info)) { }
            } else {
                info.FileName = path;
                using (Process p = Process.Start(info)) { }
            }
        }

        /// <summary>
        /// コマンドの実行
        /// </summary>
        public void Execute(string input, Config config, IntPtr owner) {
            string args;
            if (string.IsNullOrEmpty(input)) {
                args = "";
            } else {
                string commandName;
                ParseInput(input, config, out commandName, out args);
            }

            string fileName = Toolkit.IO.Utility.PathNormalize(FileName);
            string workDir;
            if (string.IsNullOrEmpty(WorkDir)) {
                if (System.IO.Directory.Exists(fileName)) {
                    workDir = fileName;
                } else {
                    string dir = System.IO.Path.GetDirectoryName(fileName);
                    if (!string.IsNullOrEmpty(dir) &&
                        System.IO.Directory.Exists(dir)) {
                        workDir = dir;
                    } else {
                        workDir = null; // 未指定とする
                    }
                }
            } else {
                workDir = Toolkit.IO.Utility.PathNormalize(WorkDir);
            }

            string param;
            if (config.OpenDirByFiler && System.IO.Directory.Exists(fileName)) {
                // フォルダをファイラで開く
                param = fileName;
                fileName = Toolkit.IO.Utility.PathNormalize(config.Filer);
            } else {
                // 通常処理
                param = Environment.ExpandEnvironmentVariables(Param + " " + args);
            }

            Toolkit.Windows.ProcessStartInfo info = new Toolkit.Windows.ProcessStartInfo();
            info.FileName = fileName;
            info.Arguments = param;
            info.WorkingDirectory = workDir;
            info.CreateNoWindow = false;
            info.ErrorDialog = true;
            info.ErrorDialogParentHandle = owner;
            if (RunAsAdmin && !Toolkit.Utility.IsUserAnAdmin()) {
                switch (config.RunAsAdminType) {
                    default:
                    case 0: info.Verb = "runas"; break;
                    case 1: // runasコマンド
                        info.Arguments = string.Format("{0} \"\\\"{1}\\\" {2}\"",
                            config.RunAsCommandLine,
                            info.FileName,
                            info.Arguments.Replace("\"", "\\\""));
                        info.FileName = "runas";
                        break;

                    case 2: // Vistaのエレベータ
                        char delim = '/';
                        foreach (char c in ",;*?<>|") {
                            if (info.FileName.IndexOf(delim) < 0 &&
                                info.Arguments.IndexOf(delim) < 0) break;
                            delim = c;
                        }
                        info.Arguments = string.Format("0{0}{1}{0}{2}{0}{0}", delim, info.FileName, info.Arguments);
                        info.FileName = Toolkit.IO.Utility.PathNormalize(config.VECmdPath);
                        break;
                }
            }
            switch (Show) {
                case 0: info.WindowStyle = Toolkit.Windows.ProcessWindowStyle.Normal; break;
                case 1: info.WindowStyle = Toolkit.Windows.ProcessWindowStyle.Minimized; break;
                case 2: info.WindowStyle = Toolkit.Windows.ProcessWindowStyle.Maximized; break;
                case 3: info.WindowStyle = Toolkit.Windows.ProcessWindowStyle.NoActivate; break;
                case 4: info.WindowStyle = Toolkit.Windows.ProcessWindowStyle.MinimizedNoActivate; break;
                case 5: info.WindowStyle = Toolkit.Windows.ProcessWindowStyle.Hidden; break;
            }

            switch (Priority) {
                case 0: Toolkit.Windows.Process.Start(info, ProcessPriorityClass.RealTime); break;
                case 1: Toolkit.Windows.Process.Start(info, ProcessPriorityClass.High); break;
                case 2: Toolkit.Windows.Process.Start(info, ProcessPriorityClass.AboveNormal); break;
                case 3: Toolkit.Windows.Process.Start(info, ProcessPriorityClass.Normal); break;
                case 4: Toolkit.Windows.Process.Start(info, ProcessPriorityClass.BelowNormal); break;
                case 5: Toolkit.Windows.Process.Start(info, ProcessPriorityClass.Idle); break;
            }
        }

        /// <summary>
        /// 後方互換性のための処理
        /// </summary>
        public static Command LoadFrom(string name, string data) {
            Command cmd = new Command();
            string[] list = data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            int i = 0;
            if (name == null) {
                cmd.Name = list[i++];
            } else {
                cmd.Name = name;
            }
            cmd.FileName = list[i++];
            cmd.Param = list[i++];
            cmd.WorkDir = list[i++];
            if (!int.TryParse(list[i++], out cmd.Show)) cmd.Show = 0;
            if (!int.TryParse(list[i++], out cmd.Priority)) cmd.Priority = 3;
            if (5 <= cmd.Priority) {
                cmd.Priority = 5;
            }
            return cmd;
        }

        /// <summary>
        /// 入力文字列とコマンド名とを比較し、完全一致したらtrue
        /// </summary>
        public bool IsMatch(string input, Config config) {
            string commandName, arguments;
            return ParseInput(input, config, out commandName, out arguments);
        }

        /// <summary>
        /// 入力文字列を、コマンド名と引数に分ける。
        /// </summary>
        /// <returns>コマンド名が一致した場合はtrue。falseだと割と適当な結果が返る。</returns>
        public bool ParseInput(string input, Config config, out string commandName, out string arguments) {
            int n = GetMatchLength(Name, input, config);
            // コマンド名と完全一致
            if (Name.Length == n &&
                (input.Length <= n || input[n] == ' ')) {
                commandName = input.Substring(0, n);
                arguments = input.Length <= n ? "" :
                    input.Substring(n).TrimStart();
                return true;
            }
            // コマンド名に一致してないなら全てコマンド名と扱う。
            commandName = input;
            arguments = null;
            return false;
        }

        private static int GetMatchLength(string x, string y, Config config) {
            int n = Math.Min(x.Length, y.Length);
            for (int i = 0; i < n; i++) {
                if (x[i] != y[i]) {
                    if (!config.CommandIgnoreCase ||
                        char.ToLower(x[i]) != char.ToLower(y[i])) {
                        return i;
                    }
                }
            }
            return n;
        }

        private static void ParseInputNotMatch(string input, out string commandName, out string arguments) {
            int n = input.IndexOf(' ');
            if (0 <= n) {
                commandName = input.Substring(0, n);
                arguments = input.Substring(n + 1).TrimStart();
            } else {
                commandName = input;
                arguments = null;
            }
        }

        const int NAME_MAXLEN = 9999; // コマンド名最大長
        const int FIRSTMATCH = 100; // 先頭一致の点数
        const int MIDMATCH = 0; // 部分一致の点数
        /// <summary>
        /// コマンド名と比較し、一致した長さに応じた点数を返す
        /// </summary>
        public int GetMatchScore(string input, Config config) {
            string commandName, arguments;
            ParseInput(input, config, out commandName, out arguments);
            // 先頭一致
            int result = InnerGetMatchScore(commandName, config, 0);
            if (0 < result) {
                return result + FIRSTMATCH + (NAME_MAXLEN - Name.Length);
            }
            // 部分一致
            int n = Name.Length - commandName.Length;
            for (int i = 0; i < n; i++) {
                int r = InnerGetMatchScore(commandName, config, i + 1);
                if (0 < r) return r + MIDMATCH + (NAME_MAXLEN - Name.Length);
            }
            return 0;
        }

        private int InnerGetMatchScore(string commandName, Config config, int offset) {
            int result = 0;
            for (int i = 0; ; i++) {
                if (commandName.Length <= i) {
                    // 入力の方が短い、または同じ長さならそこまで。
                    break;
                } else if (Name.Length <= i + offset) {
                    // 例えば、コマンドhogeに対して入力hoge_なら未一致扱い。
                    result = 0;
                    break;
                } else if (Name[i + offset] == commandName[i]) {
                    result++;
                } else if (config.CommandIgnoreCase &&
                    char.ToLower(Name[i + offset]) == char.ToLower(commandName[i])) {
                    result++;
                } else {
                    result = 0; // 違う文字入ってたら未一致。
                    break;
                }
            }
            return result;
        }

#if DEBUG
		static Command() {
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
        public static Command FromFile(string file) {
            file = Toolkit.IO.Utility.PathNormalize(file);
            Command command = new Command();
            if (string.Compare(Path.GetExtension(file), ".lnk", true) == 0) {
                // lnk
                try {
                    using (Toolkit.Windows.ShellLink link = new Toolkit.Windows.ShellLink(file)) {
                        string targetPath = Toolkit.IO.Utility.PathNormalize(link.TargetPath);
                        string workingDirectory = Toolkit.IO.Utility.PathNormalize(link.WorkingDirectory);
                        //command.Name = Path.GetFileNameWithoutExtension(file);
                        command.Name = Path.GetFileNameWithoutExtension(targetPath);
                        command.FileName = targetPath;
                        command.Param = link.Arguments;
                        command.WorkDir =
                            Toolkit.IO.Utility.EqualsPath(
                            Path.GetDirectoryName(targetPath),
                            workingDirectory) &&
                            2 <= workingDirectory.Length &&
                            workingDirectory[1] == ':' ? null : workingDirectory;
                        switch (link.DisplayMode) {
                            case Toolkit.Windows.ShellLink.ShellLinkDisplayMode.Maximized:
                                command.Show = 2;
                                break;
                            case Toolkit.Windows.ShellLink.ShellLinkDisplayMode.Minimized:
                                command.Show = 1;
                                break;
                            case Toolkit.Windows.ShellLink.ShellLinkDisplayMode.Normal:
                            default:
                                command.Show = 0;
                                break;
                        }
                        return command;
                    }
                } catch {
                    // エラー時はそのまま↓へ。
                }
            }
            // lnk以外
            command.Name = Path.GetFileNameWithoutExtension(file);
            command.FileName = file;
            return command;
        }
    }
}
