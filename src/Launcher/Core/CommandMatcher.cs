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
        if ((uint)offset > (uint)name.Length || commandName.Length > name.Length - offset)
        {
            return 0;
        }

        int end = offset + commandName.Length;
        if (!IsCharacterBoundary(name, offset) || !IsCharacterBoundary(name, end))
        {
            return 0;
        }

        var comparison = config.CommandIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return name.AsSpan(offset, commandName.Length).Equals(commandName.AsSpan(), comparison)
            ? commandName.Length : 0;
    }

    /// <summary>
    /// 先頭から一致した部分のUTF-16コード単位数を返す。
    /// </summary>
    public static int GetMatchLength(string x, string y, Config config)
    {
        var comparison = config.CommandIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int n = Math.Min(x.Length, y.Length);
        int i = 0;
        while (i < n)
        {
            int width = IsCharacterBoundary(x, i + 1) ? 1 : 2;
            if (i + width > n || !IsCharacterBoundary(y, i + width) ||
                !x.AsSpan(i, width).Equals(y.AsSpan(i, width), comparison))
            {
                break;
            }
            i += width;
        }
        return i;
    }

    static bool IsCharacterBoundary(string text, int index) =>
        index == 0 || index == text.Length ||
        !char.IsSurrogatePair(text[index - 1], text[index]);

    /// <summary>
    /// 入力文字列を、コマンド名と引数に分ける。
    /// </summary>
    /// <returns>コマンド名が一致した場合は true。false の場合の戻り値は信頼できない。</returns>
    public static bool ParseInput(string name, string input, Config config, out string commandName, out string? arguments)
    {
        int n = GetMatchLength(name, input, config);
        // コマンド名と完全一致する場合。
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

}
