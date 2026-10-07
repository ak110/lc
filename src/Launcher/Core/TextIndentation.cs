using System.Text;

namespace Launcher.Core;

/// <summary>プレーンテキストの行単位インデントと選択範囲を計算する。</summary>
public static class TextIndentation
{
    const int IndentWidth = 2;

    /// <summary>
    /// 選択範囲を行単位へ拡張し、インデントを加減した置換文字列と新しい選択範囲を算出する。
    /// </summary>
    public static string ComputeIndentReplacement(
        string text, int selStart, int selLength, bool dedent,
        out int regionStart, out int regionLength, out int newSelLength)
    {
        int selEnd = selStart + selLength;

        // 行頭まで拡張
        int lineStart = selStart;
        while (lineStart > 0 && text[lineStart - 1] != '\n') lineStart--;

        // 行末まで拡張。ただし選択が改行直後 (次行冒頭) で終わる場合はその行を含めない。
        int regionEnd;
        if (selEnd > selStart && selEnd <= text.Length && text[selEnd - 1] == '\n')
        {
            regionEnd = selEnd;
        }
        else
        {
            regionEnd = selEnd;
            while (regionEnd < text.Length && text[regionEnd] != '\n') regionEnd++;
        }

        regionStart = lineStart;
        regionLength = regionEnd - lineStart;

        string segment = text.Substring(regionStart, regionLength);
        // \nで分割し、末尾が\nなら末尾の空要素は変換対象外 (次行冒頭)
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
                    // \rを除いた実質が空行なら追加しない
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
