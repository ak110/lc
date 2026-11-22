using System.Text;

namespace Launcher.Core;

public static class TextIndentation
{
    const int IndentWidth = 2;

    public static string ComputeIndentReplacement(
        string text, int selStart, int selLength, bool dedent,
        out int regionStart, out int regionLength, out int newSelLength)
    {
        (regionStart, regionLength) = TextLines.ExpandToLines(text, selStart, selLength);

        string segment = text.Substring(regionStart, regionLength);
        string[] lines = segment.Split('\n');
        int transformCount = segment.EndsWith('\n') ? lines.Length - 1 : lines.Length;

        string indent = new(' ', IndentWidth);
        var sb = new StringBuilder(segment.Length + transformCount * IndentWidth);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (i < transformCount)
            {
                if (dedent)
                {
                    int remove = 0;
                    while (remove < IndentWidth && remove < line.Length && line[remove] == ' ')
                    {
                        remove++;
                    }
                    if (remove > 0) line = line[remove..];
                }
                else
                {
                    string trimmed = line.TrimEnd('\r');
                    if (trimmed.Length > 0)
                    {
                        line = indent + line;
                    }
                }
            }
            sb.Append(line);
            if (i < lines.Length - 1) sb.Append('\n');
        }

        string replaced = sb.ToString();
        newSelLength = replaced.Length;
        return replaced;
    }

}
