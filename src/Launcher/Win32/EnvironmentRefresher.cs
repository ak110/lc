using Microsoft.Win32;

namespace Launcher.Win32;

/// <summary>
/// レジストリから環境変数を読み込み、現在のプロセスの環境ブロックに反映する。
/// WM_SETTINGCHANGE (lParam="Environment") を受け取った際に呼び出すことで、
/// 環境変数の変更を子プロセスに伝搬できる (ShellExecuteEx は親プロセスの
/// 環境ブロックを継承するため)。
/// </summary>
/// <remarks>
/// Explorer 互換のマージ規則: PATH / PATHEXT / LIBPATH / OS2LIBPATH は
/// システム + ユーザーを ; で連結、それ以外はユーザー変数がシステム変数を上書きする。
/// REG_EXPAND_SZ は <see cref="Environment.ExpandEnvironmentVariables(string)"/>
/// で展開するが、これは現プロセス環境ベースのため、レジストリ変数同士の相互参照は
/// 完全には解決しない (実用上 %SystemRoot% 等プロセス環境にある値への参照が大半)。
/// </remarks>
public sealed class EnvironmentRefresher
{
    const string SystemEnvKeyPath = @"System\CurrentControlSet\Control\Session Manager\Environment";
    const string UserEnvKeyPath = @"Environment";

    // Explorer 互換: システム + ユーザーを連結する変数名
    static readonly string[] PathLikeVars = ["Path", "PATHEXT", "LIBPATH", "OS2LIBPATH"];

    // 過去にレジストリから読み込んだ変数名 (削除検出用)。
    // 起動時に外部から注入された変数 (USERNAME 等) を誤って削除しないため、
    // ここに載っているものだけを削除対象とする。
    HashSet<string> trackedNames;

    public EnvironmentRefresher()
    {
        // 起動時に一度スナップショットを取り、削除検出用の名前集合を初期化する
        // (この時点ではプロセスの環境ブロックには手を加えない)
        var snapshot = ReadRegistryVars();
        trackedNames = new HashSet<string>(snapshot.Keys, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// レジストリを読み直し、差分をプロセスの環境ブロックに反映する。
    /// </summary>
    /// <returns>何らかの変更を適用した場合は true</returns>
    public bool Refresh()
    {
        var newVars = ReadRegistryVars();
        bool changed = false;

        // 追加・変更の適用
        foreach (var (name, value) in newVars)
        {
            string? current = Environment.GetEnvironmentVariable(name);
            if (!string.Equals(current, value, StringComparison.Ordinal))
            {
                Environment.SetEnvironmentVariable(name, value);
                changed = true;
            }
        }

        // 削除の適用: 前回あったが今回無い変数のみクリアする
        foreach (string name in trackedNames)
        {
            if (!newVars.ContainsKey(name))
            {
                Environment.SetEnvironmentVariable(name, null);
                changed = true;
            }
        }

        trackedNames = new HashSet<string>(newVars.Keys, StringComparer.OrdinalIgnoreCase);
        return changed;
    }

    /// <summary>
    /// レジストリから環境変数を読み込み、Explorer 互換のマージ規則を適用した
    /// 期待値の辞書を構築する。
    /// </summary>
    static Dictionary<string, string> ReadRegistryVars()
    {
        var systemRaw = ReadKey(Registry.LocalMachine, SystemEnvKeyPath);
        var userRaw = ReadKey(Registry.CurrentUser, UserEnvKeyPath);
        return BuildExpectedEnv(systemRaw, userRaw);
    }

    /// <summary>
    /// システム変数とユーザー変数を Explorer 互換のマージ規則で結合し、
    /// %VAR% 展開済みの辞書を返す (純粋関数; テスト容易性のため分離)。
    /// </summary>
    internal static Dictionary<string, string> BuildExpectedEnv(
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

    static bool IsPathLike(string name)
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

    static List<KeyValuePair<string, string>> ReadKey(RegistryKey root, string subKey)
    {
        var list = new List<KeyValuePair<string, string>>();
        using RegistryKey? key = root.OpenSubKey(subKey);
        if (key is null) return list;

        foreach (string name in key.GetValueNames())
        {
            // DoNotExpandEnvironmentNames で生値を取得し、展開は BuildExpectedEnv で一括
            object? raw = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is string s)
            {
                list.Add(new KeyValuePair<string, string>(name, s));
            }
        }
        return list;
    }
}
