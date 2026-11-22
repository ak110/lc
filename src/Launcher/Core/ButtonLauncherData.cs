using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

public sealed class ButtonLauncherData : ConfigStore
{
    public List<ButtonTab> Tabs { get; set; } = new List<ButtonTab>();
    public int DefaultTabIndex { get; set; }
    public int Columns { get; set; } = 7;
    public int Rows { get; set; } = 7;
    public bool IsLocked { get; set; }
    public Point WindowPos { get; set; } = Point.Empty;
    public Size WindowSize { get; set; } = Size.Empty;

    public bool Save(Action<ConfigSaveFailure> notify, string? baseName = null) => Store.Save(this, notify, baseName);

    public static ConfigLoadResult<ButtonLauncherData> Load(string? baseName = null) => Store.Load(baseName);

    public static ConfigFile<ButtonLauncherData> Store { get; } = new(".btns.cfg");
}

public sealed class ButtonTab
{
    public string Name { get; set; } = "";
    public List<ButtonEntry> Buttons { get; set; } = new List<ButtonEntry>();

    public ButtonEntry? GetButton(int row, int col)
    {
        return Buttons.Find(b => b.Row == row && b.Col == col);
    }

    public void SetButton(int row, int col, ButtonEntry? entry)
    {
        Buttons.RemoveAll(b => b.Row == row && b.Col == col);
        if (entry is not null)
        {
            entry.Row = row;
            entry.Col = col;
            Buttons.Add(entry);
        }
    }
}

public sealed class ButtonEntry : Command
{
    public ButtonEntry() { }

    private ButtonEntry(Command source) : base(source) { }

    public int Row { get; set; }
    public int Col { get; set; }

    [XmlIgnore]
    public bool IsEmpty => string.IsNullOrEmpty(FileName);

    public static ButtonEntry FromCommand(Command cmd, int row, int col)
    {
        var entry = new ButtonEntry(cmd)
        {
            Row = row,
            Col = col,
        };
        return entry;
    }
}
