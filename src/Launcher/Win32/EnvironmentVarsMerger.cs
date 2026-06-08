namespace Launcher.Win32;

/// <summary>
/// レジストリ生値（システム変数・ユーザー変数）を Explorer 互換のマージ規則で結合し、
/// %VAR% 展開済みの辞書を構築する純粋関数。
/// 副作用ある <see cref="EnvironmentRefresher"/> から、テスト容易性のために分離した。
/// </summary>
/// <remarks>
/// Explorer 互換のマージ規則: PATH / PATHEXT / LIBPATH / OS2LIBPATH は
/// システム + ユーザーを ; で連結、それ以外はユーザー変数がシステム変数を上書きする。
/// REG_EXPAND_SZ は <see cref="Environment.ExpandEnvironmentVariables(string)"/>
/// で展開するが、これは現プロセス環境ベースのため、レジストリ変数同士の相互参照は
/// 完全には解決しない (実用上 %SystemRoot% 等プロセス環境にある値への参照が大半)。
/// </remarks>
public static class EnvironmentVarsMerger
{
    // Explorer 互換: システム + ユーザーを連結する変数名
    private static readonly string[] PathLikeVars = ["Path", "PATHEXT", "LIBPATH", "OS2LIBPATH"];

    /// <summary>
    /// システム変数とユーザー変数を Explorer 互換のマージ規則で結合し、
    /// %VAR% 展開済みの辞書を返す。
    /// </summary>
    public static Dictionary<string, string> Merge(
        IReadOnlyList<KeyValuePair<string, string>> systemRaw,
        IReadOnlyList<KeyValuePair<string, string>> userRaw)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in systemRaw)
        {
            merged[name] = value;
        }

        foreach (var (name, value) in userRaw)
        {
            if (IsPathLike(name)
                && merged.TryGetValue(name, out string? sysValue)
                && !string.IsNullOrEmpty(sysValue))
            {
                merged[name] = sysValue.TrimEnd(';') + ";" + value;
            }
            else
            {
                merged[name] = value;
            }
        }

        // REG_EXPAND_SZ の %VAR% を現プロセス環境ベースで展開
        var expanded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in merged)
        {
            expanded[name] = Environment.ExpandEnvironmentVariables(value);
        }
        return expanded;
    }

    private static bool IsPathLike(string name)
    {
        foreach (string p in PathLikeVars)
        {
            if (string.Equals(name, p, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
