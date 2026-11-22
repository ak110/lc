using System.Text;
using System.Xml;
using Launcher.Infrastructure;

namespace Launcher.Core;

public sealed class MemoData : ConfigStore
{
    public const int DefaultClosedTabsLimit = 10;

    public List<MemoTab> Tabs { get; set; } = new List<MemoTab>();
    public int CurrentTabIndex { get; set; }

    public List<MemoTab> ClosedTabs { get; set; } = new List<MemoTab>();
    public int ClosedTabsLimit { get; set; } = DefaultClosedTabsLimit;

    public Point WindowPos { get; set; } = Point.Empty;
    public Size WindowSize { get; set; } = Size.Empty;

    public string FontName { get; set; } = "Consolas";
    public float FontSize { get; set; } = 11f;

    public bool Save(Action<ConfigSaveFailure> notify, string? baseName = null) => Store.Save(this, notify, baseName);

    public static ConfigLoadResult<MemoData> Load(string? baseName = null) => Store.Load(baseName);

    public static ConfigFile<MemoData> Store { get; } = new(".memo.cfg");
}

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
