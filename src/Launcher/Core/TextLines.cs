namespace Launcher.Core;

/// <summary>プレーンテキストの選択範囲を行単位で扱う計算をまとめる。</summary>
public static class TextLines
{
    /// <summary>
    /// 選択範囲を行単位へ拡張した範囲を求める。
    /// 選択が改行の直後 (次行の行頭) で終わる場合は、その行を範囲へ含めない。
    /// </summary>
    public static (int start, int length) ExpandToLines(string text, int selStart, int selLength)
    {
        int selEnd = selStart + selLength;

        int lineStart = selStart;
        while (lineStart > 0 && text[lineStart - 1] != '\n') lineStart--;

        int regionEnd = selEnd;
        bool endsAtLineHead = selEnd > selStart && selEnd <= text.Length && text[selEnd - 1] == '\n';
        if (!endsAtLineHead)
        {
            while (regionEnd < text.Length && text[regionEnd] != '\n') regionEnd++;
        }
        return (lineStart, regionEnd - lineStart);
    }

    /// <summary>
    /// 選択範囲を行単位へ拡張し、その行を序数比較の安定ソートで並べ替えた置換文字列を返す。
    /// 改行 (<c>\n</c>と、その直前の<c>\r</c>) は元の位置に残し、行の内容だけを並べ替える。
    /// </summary>
    public static string ComputeSortReplacement(string text, int selStart, int selLength, out int regionStart, out int regionLength)
    {
        (regionStart, regionLength) = ExpandToLines(text, selStart, selLength);
        string segment = text.Substring(regionStart, regionLength);
        string[] parts = segment.Split('\n');
        // 末尾が改行なら最後の空要素は次行の行頭であり、並べ替えの対象にしない。
        int lineCount = segment.EndsWith('\n') ? parts.Length - 1 : parts.Length;

        var contents = new string[lineCount];
        var separators = new string[parts.Length - 1];
        for (int i = 0; i < lineCount; i++)
        {
            string part = parts[i];
            bool hasCr = i < separators.Length && part.EndsWith('\r');
            contents[i] = hasCr ? part[..^1] : part;
            if (i < separators.Length) separators[i] = hasCr ? "\r\n" : "\n";
        }

        // OrderByは安定ソートのため、同じ内容の行は元の順序を保つ。
        var sorted = contents.OrderBy(line => line, StringComparer.Ordinal).ToArray();
        var sb = new System.Text.StringBuilder(segment.Length);
        for (int i = 0; i < sorted.Length; i++)
        {
            sb.Append(sorted[i]);
            if (i < separators.Length) sb.Append(separators[i]);
        }
        return sb.ToString();
    }
}
