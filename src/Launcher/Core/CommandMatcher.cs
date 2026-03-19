namespace Launcher.Core;

/// <summary>
/// コマンド名のマッチング処理
/// </summary>
public static class CommandMatcher
{
    const int NAME_MAXLEN = 9999; // コマンド名最大長
    const int FIRSTMATCH = 100; // 先頭一致の点数
    const int MIDMATCH = 0; // 部分一致の点数

    /// <summary>
    /// コマンド名と比較し、一致した長さに応じた点数を返す
    /// </summary>
    public static int GetMatchScore(string name, string input, Config config)
    {
        ParseInput(name, input, config, out string commandName, out _);
        // 先頭一致
        int result = InnerGetMatchScore(name, commandName, config, 0);
        if (0 < result)
        {
            return result + FIRSTMATCH + (NAME_MAXLEN - name.Length);
        }
        // 部分一致
        int n = name.Length - commandName.Length;
        for (int i = 0; i < n; i++)
        {
            int r = InnerGetMatchScore(name, commandName, config, i + 1);
            if (0 < r) return r + MIDMATCH + (NAME_MAXLEN - name.Length);
        }
        return 0;
    }

    /// <summary>
    /// 内部マッチスコア計算
    /// </summary>
    public static int InnerGetMatchScore(string name, string commandName, Config config, int offset)
    {
        int result = 0;
        for (int i = 0; ; i++)
        {
            if (commandName.Length <= i)
            {
                // 入力の方が短い、または同じ長さならそこまで。
                break;
            }
            else if (name.Length <= i + offset)
            {
                // 例えば、コマンドhogeに対して入力hoge_なら未一致扱い。
                result = 0;
                break;
            }
            else if (name[i + offset] == commandName[i])
            {
                result++;
            }
            else if (config.CommandIgnoreCase &&
                char.ToLower(name[i + offset]) == char.ToLower(commandName[i]))
            {
                result++;
            }
            else
            {
                result = 0; // 違う文字入ってたら未一致。
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// 先頭からの一致文字数を返す
    /// </summary>
    public static int GetMatchLength(string x, string y, Config config)
    {
        int n = Math.Min(x.Length, y.Length);
        for (int i = 0; i < n; i++)
        {
            if (x[i] != y[i])
            {
                if (!config.CommandIgnoreCase ||
                    char.ToLower(x[i]) != char.ToLower(y[i]))
                {
                    return i;
                }
            }
        }
        return n;
    }

    /// <summary>
    /// 入力文字列を、コマンド名と引数に分ける。
    /// </summary>
    /// <returns>コマンド名が一致した場合はtrue。falseだと割と適当な結果が返る。</returns>
    public static bool ParseInput(string name, string input, Config config, out string commandName, out string? arguments)
    {
        int n = GetMatchLength(name, input, config);
        // コマンド名と完全一致
        if (name.Length == n &&
            (input.Length <= n || input[n] == ' '))
        {
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

    /// <summary>
    /// 入力文字列をコマンド名と引数に分ける（マッチしない場合用）
    /// </summary>
    public static void ParseInputNotMatch(string input, out string commandName, out string? arguments)
    {
        int n = input.IndexOf(' ');
        if (0 <= n)
        {
            commandName = input.Substring(0, n);
            arguments = input.Substring(n + 1).TrimStart();
        }
        else
        {
            commandName = input;
            arguments = null;
        }
    }
}
