namespace Launcher.Core;

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
        vars.Sort((x, y) => -x.Value.Length.CompareTo(y.Value.Length));
        System.Diagnostics.Debug.Assert(vars.Count <= 1 ||
            vars[vars.Count - 1].Value.Length <= vars[0].Value.Length);
    }

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

    public IReadOnlyList<string?> Calculate(IReadOnlyList<string?> values) => values.Select(InnerReplace).ToArray();

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
        string str2 = InnerReplace2(str, false);
        if (!File.Exists(str2) && !Directory.Exists(str2))
        {
            return null;
        }
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
            if (str.StartsWith(s1, StringComparison.OrdinalIgnoreCase))
            {
                return s2 + str[s1.Length..];
            }
        }
        return str;
    }
}
