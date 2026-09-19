using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using Launcher.Infrastructure;

namespace Launcher.Core;

/// <summary>
/// メモパッドのデータ
/// </summary>
public sealed class MemoData : ConfigStore
{
    /// <summary>閉じたタブを保持するゴミ箱の既定上限</summary>
    public const int DefaultClosedTabsLimit = 10;

    public List<MemoTab> Tabs { get; set; } = new List<MemoTab>();
    public int CurrentTabIndex { get; set; }

    /// <summary>閉じたタブのゴミ箱。先頭ほど新しい。</summary>
    public List<MemoTab> ClosedTabs { get; set; } = new List<MemoTab>();
    public int ClosedTabsLimit { get; set; } = DefaultClosedTabsLimit;

    public Point WindowPos { get; set; } = Point.Empty;
    public Size WindowSize { get; set; } = Size.Empty;

    public string FontName { get; set; } = "Consolas";
    public float FontSize { get; set; } = 11f;

    /// <summary>
    /// 書き込み
    /// </summary>
    public void Serialize()
    {
        Serialize(".memo.cfg");
    }

    /// <summary>
    /// 読み込み
    /// </summary>
    public static MemoData Deserialize()
    {
        try
        {
            return Deserialize<MemoData>(".memo.cfg");
        }
        catch (InvalidOperationException)
        {
            return new MemoData();
        }
        catch (XmlException)
        {
            return new MemoData();
        }
        catch (IOException)
        {
            return new MemoData();
        }
    }
}

/// <summary>
/// メモパッドのタブ。タブ名と本文のプレーンテキストを保持する。
/// </summary>
public sealed class MemoTab
{
    private string name = "";
    private string text = "";

    public string Name
    {
        get => name;
        set => name = RemoveInvalidXmlChars(value);
    }

    public string Text
    {
        get => text;
        set => text = RemoveInvalidXmlChars(value);
    }

    private static string RemoveInvalidXmlChars(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";

        StringBuilder? result = null;
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsHighSurrogate(current) &&
                i + 1 < value.Length &&
                char.IsLowSurrogate(value[i + 1]))
            {
                if (result is not null)
                {
                    result.Append(current);
                    result.Append(value[i + 1]);
                }
                i++;
                continue;
            }

            if (XmlConvert.IsXmlChar(current))
            {
                result?.Append(current);
                continue;
            }

            result ??= new StringBuilder(value.Length).Append(value, 0, i);
        }

        return result?.ToString() ?? value;
    }
}
