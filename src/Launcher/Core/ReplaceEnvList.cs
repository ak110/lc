#nullable disable
using System.IO;

namespace Launcher.Core;

/// <summary>
/// 環境変数の置換処理。
/// </summary>
sealed class ReplaceEnvList
{
    // 置換時のスレッドセーフ用ロック（commandオブジェクト自体をロックしない）
    readonly object lockObj = new object();
    List<KeyValuePair<string, string>> vars
        = new List<KeyValuePair<string, string>>();

    public ReplaceEnvList(List<string> list)
    {
        foreach (string name in list)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(value))
            {
                vars.Add(new KeyValuePair<string, string>("%" + name + "%", value));
            }
        }
        // Valueの長さの長い順に並べる。
        vars.Sort(delegate (KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            return -x.Value.Length.CompareTo(y.Value.Length);
        });
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
    /// 置換処理その2
    /// </summary>
    public void Replace(Command command)
    {
        // パスを置換
        {
            string old = command.FileName;
            string rep = InnerReplace(old);
            if (!string.IsNullOrEmpty(rep))
            {
                lock (lockObj)
                {
                    if (command.FileName == old)
                        command.FileName = rep;
                }
            }
        }
        // 作業フォルダを置換
        {
            string old = command.WorkDir;
            string rep = InnerReplace(old);
            if (!string.IsNullOrEmpty(rep))
            {
                lock (lockObj)
                {
                    if (command.WorkDir == old)
                        command.WorkDir = rep;
                }
            }
        }
    }

    private string InnerReplace(string str)
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

    private string InnerReplace2(string str, bool valueToName)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
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
