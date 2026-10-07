namespace Launcher.Core;

/// <summary>
/// 環境変数の置換処理。
/// </summary>
public sealed class ReplaceEnvList
{
    List<KeyValuePair<string, string>> vars = [];

    public ReplaceEnvList(List<string> list)
    {
        foreach (string name in list)
        {
            string? value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(value))
            {
                vars.Add(new KeyValuePair<string, string>("%" + name + "%", value));
            }
        }
        // Valueの長さの長い順に並べる。
        vars.Sort((x, y) => -x.Value.Length.CompareTo(y.Value.Length));
        System.Diagnostics.Debug.Assert(vars.Count <= 1 ||
            vars[vars.Count - 1].Value.Length <= vars[0].Value.Length);
    }

    /// <summary>
    /// 取得済みの文字列の置換を背景スレッドで計算する。
    /// 置換は<see cref="InnerReplace"/>でパスの実在を確認するため、
    /// 切断済みのネットワークドライブやリムーバブルメディアが対象に含まれると
    /// 1件あたり数十秒ブロックする。
    /// 複数のコマンドやタスクをまとめて置換する呼び出しは、
    /// 本メソッドを通してUIスレッドの占有を避ける。完了通知も背景スレッドから呼ぶため、
    /// 共有データへの適用は所有者がUIスレッドへ配送する。
    /// </summary>
    /// <param name="names">置換対象の環境変数名</param>
    /// <param name="values">UIスレッドで取得したパスの文字列</param>
    /// <param name="completed">計算結果の通知</param>
    public static void StartBackgroundReplace(List<string> names, IReadOnlyList<string?> values,
        Action<IReadOnlyList<string?>> completed)
    {
        var namesCopy = names.ToList();
        var valuesCopy = values.ToArray();
        var thread = new Thread(() => completed(new ReplaceEnvList(namesCopy).Calculate(valuesCopy)))
        {
            IsBackground = true,
            Priority = ThreadPriority.Lowest,
        };
        thread.Start();
    }

    /// <summary>
    /// 共有データに触れず、パスの置換結果を計算する。
    /// </summary>
    public IReadOnlyList<string?> Calculate(IReadOnlyList<string?> values) => values.Select(InnerReplace).ToArray();

    /// <summary>
    /// 共有前の単一コマンドを同期置換する。
    /// </summary>
    public void Replace(Command command)
    {
        string? rep = InnerReplace(command.FileName);
        if (!string.IsNullOrEmpty(rep))
        {
            command.FileName = rep;
        }
        string? repDir = InnerReplace(command.WorkDir);
        if (!string.IsNullOrEmpty(repDir))
        {
            command.WorkDir = repDir;
        }
    }

    private string? InnerReplace(string? str)
    {
        // まず逆向きに置換
        string str2 = InnerReplace2(str, false);
        // 存在しないパスは置換しない
        if (!File.Exists(str2) && !Directory.Exists(str2))
        {
            return null;
        }
        // 置換処理
        return InnerReplace2(str2, true);
    }

    private string InnerReplace2(string? str, bool valueToName)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str ?? string.Empty;
        }
        foreach (KeyValuePair<string, string> p in vars)
        {
            string s1, s2;
            if (valueToName)
            {
                s1 = p.Value;
                s2 = p.Key;
            }
            else
            {
                s1 = p.Key;
                s2 = p.Value;
            }
            if (str.StartsWith(s1, StringComparison.CurrentCultureIgnoreCase))
            {
                return str.Replace(s1, s2);
            }
        }
        // 該当なし。置換済み文字列を元に戻すケースもあるため str をそのまま返す。
        return str;
    }
}
