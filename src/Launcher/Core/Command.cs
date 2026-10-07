using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

public class Command : ICloneable, IComparable<Command>, IComparable
{
    public Command() { }

    /// <summary>派生型へ変換するときも、コマンドの全プロパティを引き継ぐ。</summary>
    protected Command(Command source)
    {
        foreach (var property in CopyProperties)
            property.SetValue(this, property.GetValue(source));
    }

    // 公開プロパティの追加を派生型への変換にも自動で反映する。
    private static readonly System.Reflection.PropertyInfo[] CopyProperties = typeof(Command)
        .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
        .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
        .ToArray();

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

    /// <summary>親フォルダーを開く要求を作成する。パスの解決はWindows側が担う。</summary>
    public static ShellProcessStartInfo? OpenDirectory(Config config, string? resolvedPath, IntPtr owner = default)
    {
        string? path = resolvedPath;
        if (File.Exists(path) || Directory.Exists(path))
        {
            return LaunchRequestBuilder.Create(config.OpenParentFiler,
                $"{config.OpenParentFilerParam1}{path}{config.OpenParentFilerParam2}", owner: owner);
        }
        while (!string.IsNullOrEmpty(path))
        {
            path = Path.GetDirectoryName(path);
            if (path is not null && Directory.Exists(path))
                return config.OpenDirByFiler
                    ? LaunchRequestBuilder.Create(config.Filer, path, path, owner: owner)
                    : LaunchRequestBuilder.Create(path, owner: owner);
        }
        return null;
    }

    /// <summary>コマンドの起動要求を作成する。実行と権限の照会は呼び出し側が担う。</summary>
    public ShellProcessStartInfo Execute(string input, Config config, IntPtr owner, bool isAdministrator)
    {
        string? args = "";
        if (!string.IsNullOrEmpty(input)) ParseInput(input, config, out _, out args);
        var info = LaunchRequestBuilder.Create(FileName, $"{Param} {args}", WorkDir, Show, Priority, owner);
        if (config.OpenDirByFiler && Directory.Exists(info.FileName))
        {
            info.Arguments = info.FileName;
            info.FileName = PathHelper.PathNormalize(config.Filer);
        }
        if (RunAsAdmin && !isAdministrator)
            AdminElevationApplier.Apply(info, config.RunAsAdminType, config.RunAsCommandLine, config.VECmdPath);
        return info;
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

}
