using System.Text;
using System.Text.RegularExpressions;

namespace Launcher.Core;

public static class MemoSearch
{
    public static (int start, int length) FindMatch(
        string text, string pattern, int startIndex, bool reverse, bool matchCase, bool useRegex)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text)) return (-1, 0);
        startIndex = Math.Clamp(startIndex, 0, text.Length);

        if (useRegex)
        {
            var options = RegexOptions.Multiline;
            if (!matchCase) options |= RegexOptions.IgnoreCase;
            if (reverse) options |= RegexOptions.RightToLeft;
            try
            {
                var regex = new Regex(pattern, options);
                var match = regex.Match(text, startIndex);
                return match.Success ? (match.Index, match.Length) : (-1, 0);
            }
            catch (ArgumentException)
            {
                return (-1, 0);
            }
        }
        else
        {
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int index = reverse
                ? text.LastIndexOf(pattern, Math.Max(0, startIndex - 1), comparison)
                : text.IndexOf(pattern, startIndex, comparison);
            return index >= 0 ? (index, pattern.Length) : (-1, 0);
        }
    }

    public static bool IsMatch(string text, string pattern, bool matchCase, bool useRegex)
    {
        if (useRegex)
        {
            var options = RegexOptions.Multiline;
            if (!matchCase) options |= RegexOptions.IgnoreCase;
            try
            {
                var match = Regex.Match(text, pattern, options);
                return match.Success && match.Index == 0 && match.Length == text.Length;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
        else
        {
            return string.Equals(text, pattern, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }
    }

    public static string ComputeReplacement(string matched, string pattern, string replacement, bool matchCase, bool useRegex)
    {
        if (!useRegex) return replacement;
        var options = RegexOptions.Multiline;
        if (!matchCase) options |= RegexOptions.IgnoreCase;
        return Regex.Replace(matched, pattern, ExpandReplacementEscapes(replacement), options);
    }

    public static string ReplaceAllInText(
        string text, string pattern, string replacement, bool matchCase, bool useRegex, out int count)
    {
        if (useRegex)
        {
            var options = RegexOptions.Multiline;
            if (!matchCase) options |= RegexOptions.IgnoreCase;
            var regex = new Regex(pattern, options);
            string expanded = ExpandReplacementEscapes(replacement);
            int localCount = 0;
            string result = regex.Replace(text, m =>
            {
                localCount++;
                return m.Result(expanded);
            });
            count = localCount;
            return result;
        }
        else
        {
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var sb = new StringBuilder(text.Length);
            int index = 0;
            int localCount = 0;
            while (index <= text.Length)
            {
                int next = text.IndexOf(pattern, index, comparison);
                if (next < 0)
                {
                    sb.Append(text, index, text.Length - index);
                    break;
                }
                sb.Append(text, index, next - index);
                sb.Append(replacement);
                index = next + pattern.Length;
                localCount++;
                if (pattern.Length == 0)
                {
                    if (index < text.Length) sb.Append(text[index]);
                    index++;
                }
            }
            count = localCount;
            return sb.ToString();
        }
    }

    static string ExpandReplacementEscapes(string replacement)
    {
        var sb = new StringBuilder(replacement.Length);
        int i = 0;
        while (i < replacement.Length)
        {
            char c = replacement[i];
            if (c != '\\' || i + 1 >= replacement.Length)
            {
                sb.Append(c);
                i++;
                continue;
            }
            char next = replacement[i + 1];
            char? simple = next switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'a' => '\a',
                'f' => '\f',
                'v' => '\v',
                'e' => '\u001B',
                '0' => '\0',
                '\\' => '\\',
                _ => null,
            };
            int consumed = 2;
            if (simple is null && (next == 'x' || next == 'u'))
            {
                int digits = next == 'x' ? 2 : 4;
                if (i + 2 + digits <= replacement.Length
                    && int.TryParse(replacement.AsSpan(i + 2, digits), System.Globalization.NumberStyles.AllowHexSpecifier,
                        System.Globalization.CultureInfo.InvariantCulture, out int code))
                {
                    simple = (char)code;
                    consumed = 2 + digits;
                }
            }
            if (simple is char value)
            {
                sb.Append(value == '$' ? "$$" : value.ToString());
                i += consumed;
            }
            else
            {
                sb.Append(c).Append(next);
                i += 2;
            }
        }
        return sb.ToString();
    }
}
