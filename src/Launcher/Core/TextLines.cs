namespace Launcher.Core;

public static class TextLines
{
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

    public static string ComputeSortReplacement(string text, int selStart, int selLength, out int regionStart, out int regionLength)
    {
        (regionStart, regionLength) = ExpandToLines(text, selStart, selLength);
        string segment = text.Substring(regionStart, regionLength);
        string[] parts = segment.Split('\n');
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
