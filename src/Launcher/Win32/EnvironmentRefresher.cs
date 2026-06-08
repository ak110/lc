using Microsoft.Win32;

namespace Launcher.Win32;

/// <summary>
/// レジストリから環境変数を読み込み、現在のプロセスの環境ブロックに反映する。
/// WM_SETTINGCHANGE (lParam="Environment") を受け取った際に呼び出すことで、
/// 環境変数の変更を子プロセスに伝搬できる (ShellExecuteEx は親プロセスの
/// 環境ブロックを継承するため)。
/// </summary>
/// <remarks>
/// マージ規則の詳細は <see cref="EnvironmentVarsMerger"/> を参照。
/// </remarks>
public sealed class EnvironmentRefresher
{
    const string SystemEnvKeyPath = @"System\CurrentControlSet\Control\Session Manager\Environment";
    const string UserEnvKeyPath = @"Environment";

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
        return EnvironmentVarsMerger.Merge(systemRaw, userRaw);
    }

    static List<KeyValuePair<string, string>> ReadKey(RegistryKey root, string subKey)
    {
        var list = new List<KeyValuePair<string, string>>();
        using RegistryKey? key = root.OpenSubKey(subKey);
        if (key is null) return list;

        foreach (string name in key.GetValueNames())
        {
            // DoNotExpandEnvironmentNames で生値を取得し、展開は EnvironmentVarsMerger.Merge で一括
            object? raw = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is string s)
            {
                list.Add(new KeyValuePair<string, string>(name, s));
            }
        }
        return list;
    }
}
