namespace Launcher.Core;

public static class CommandMatcher
{
    const int NAME_MAXLEN = 9999;
    const int FIRSTMATCH = 100;
    const int MIDMATCH = 0;

    public static int GetMatchScore(string name, string input, Config config)
    {
        ParseInput(name, input, config, out string commandName, out _);
        int result = InnerGetMatchScore(name, commandName, config, 0);
        if (0 < result)
        {
            return result + FIRSTMATCH + (NAME_MAXLEN - name.Length);
        }
        int n = name.Length - commandName.Length;
        for (int i = 0; i < n; i++)
        {
            int r = InnerGetMatchScore(name, commandName, config, i + 1);
            if (0 < r) return r + MIDMATCH + (NAME_MAXLEN - name.Length);
        }
        return 0;
    }

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

    public static bool ParseInput(string name, string input, Config config, out string commandName, out string? arguments)
    {
        int n = GetMatchLength(name, input, config);
        if (name.Length == n &&
            (input.Length <= n || input[n] == ' '))
        {
            commandName = input.Substring(0, n);
            arguments = input.Length <= n ? "" :
                input.Substring(n).TrimStart();
            return true;
        }
        commandName = input;
        arguments = null;
        return false;
    }

}
