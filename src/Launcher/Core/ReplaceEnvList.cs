using System.IO;

namespace Launcher.Core;

/// <summary>
/// 環境変数の置換処理。
/// </summary>
sealed class ReplaceEnvList
{
    // 置換時のスレッドセーフ用ロック。
    // 複数スレッド (MainForm.ApplyConfig の背景スレッド、環境変数変更時の Refresh 等)
    // から同じ Command / SchedulerTask インスタンスを並行更新する可能性があるため、
    // ReplaceEnvList インスタンス間でも排他する必要があり static にしている。
    static readonly object lockObj = new();
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
    /// 置換処理
    /// </summary>
    public void Replace(CommandList commandList)
    {
        try
        {
            foreach (Command command in commandList.Commands)
            {
                Replace(command);
            }
        }
        catch (InvalidOperationException)
        {
            // コレクション変更された、とか
        }
    }

    /// <summary>
    /// スケジューラーデータの置換処理
    /// </summary>
    public void Replace(SchedulerData schedulerData)
    {
        try
        {
            foreach (var item in schedulerData.Items)
            {
                foreach (var task in item.Tasks)
                {
                    Replace(task);
                }
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    /// <summary>
    /// スケジューラータスクの置換処理
    /// </summary>
    public void Replace(SchedulerTask task)
    {
        lock (lockObj)
        {
            string? rep = InnerReplace(task.FileName);
            if (!string.IsNullOrEmpty(rep))
            {
                task.FileName = rep;
            }
        }
    }

    /// <summary>
    /// コマンドの置換処理
    /// </summary>
    public void Replace(Command command)
    {
        // 読み取り→置換→書き込みを一貫して行うためメソッド全体をロック
        lock (lockObj)
        {
            // パスを置換
            string? rep = InnerReplace(command.FileName);
            if (!string.IsNullOrEmpty(rep))
            {
                command.FileName = rep;
            }
            // 作業フォルダを置換
            string? repDir = InnerReplace(command.WorkDir);
            if (!string.IsNullOrEmpty(repDir))
            {
                command.WorkDir = repDir;
            }
        }
    }

    private string? InnerReplace(string? str)
    {
        // ひとまず逆向きに置換
        string str2 = InnerReplace2(str, false);
        // 存在せんのは置換しない。
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
        // 該当が無かった場合。
        // 置換してたものをやめる場合もありうるので、strをそのまま返す。
        return str;
    }
}
