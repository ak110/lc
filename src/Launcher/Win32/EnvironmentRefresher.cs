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
        var snapshot = ReadRegistryVars();
        trackedNames = new HashSet<string>(snapshot.Keys, StringComparer.OrdinalIgnoreCase);
    }

    public bool Refresh()
    {
        var newVars = ReadRegistryVars();
        var current = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in newVars.Keys.Concat(trackedNames))
            current[name] = Environment.GetEnvironmentVariable(name);

        var diff = ComputeRefreshDiff(newVars, current, trackedNames);
        foreach (var (name, value) in diff.Updates)
            Environment.SetEnvironmentVariable(name, value);
        trackedNames = diff.TrackedNames;
        return diff.Updates.Count > 0;
    }

    public static EnvironmentRefreshDiff ComputeRefreshDiff(
        IReadOnlyDictionary<string, string> newRegistryVars,
        IReadOnlyDictionary<string, string?> currentProcessVars,
        IReadOnlySet<string> trackedNames)
    {
        var updates = new List<KeyValuePair<string, string?>>();
        foreach (var (name, value) in newRegistryVars)
        {
            if (!string.Equals(currentProcessVars.GetValueOrDefault(name), value, StringComparison.Ordinal))
                updates.Add(new(name, value));
        }

        var nextTracked = new HashSet<string>(newRegistryVars.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (string name in trackedNames)
        {
            if (!nextTracked.Contains(name))
                updates.Add(new(name, null));
        }
        return new EnvironmentRefreshDiff(updates, nextTracked);
    }

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

public sealed record EnvironmentRefreshDiff(
    IReadOnlyList<KeyValuePair<string, string?>> Updates,
    HashSet<string> TrackedNames);
