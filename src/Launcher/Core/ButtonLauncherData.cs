using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

/// <summary>
/// ボタン型ランチャーのデータ
/// </summary>
public sealed class ButtonLauncherData : ConfigStore
{
    public List<ButtonTab> Tabs { get; set; } = new List<ButtonTab>();
    public int DefaultTabIndex { get; set; }
    public int Columns { get; set; } = 7;
    public int Rows { get; set; } = 7;
    public bool IsLocked { get; set; }
    public Point WindowPos { get; set; } = Point.Empty;
    public Size WindowSize { get; set; } = Size.Empty;

    /// <summary>
    /// 書き込み
    /// </summary>
    public bool Save(Action<ConfigSaveFailure> notify, string? baseName = null) => Store.Save(this, notify, baseName);

    /// <summary>
    /// 読み込み
    /// </summary>
    public static ConfigLoadResult<ButtonLauncherData> Load(string? baseName = null) => Store.Load(baseName);

    /// <summary>
    /// 保存データ (.btns.cfg) の読込・復元
    /// </summary>
    public static ConfigFile<ButtonLauncherData> Store { get; } = new(".btns.cfg");
}

/// <summary>
/// ボタンランチャーのタブ
/// </summary>
public sealed class ButtonTab
{
    public string Name { get; set; } = "";
    public List<ButtonEntry> Buttons { get; set; } = new List<ButtonEntry>();

    /// <summary>
    /// 指定位置のボタンを取得。未割り当てならnull。
    /// </summary>
    public ButtonEntry? GetButton(int row, int col)
    {
        return Buttons.Find(b => b.Row == row && b.Col == col);
    }

    /// <summary>
    /// 指定位置のボタンを設定。nullなら削除。
    /// </summary>
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

/// <summary>
/// ボタンランチャーの個別ボタン。Commandを継承しRow/Colを追加。
/// </summary>
public sealed class ButtonEntry : Command
{
    public ButtonEntry() { }

    private ButtonEntry(Command source) : base(source) { }

    public int Row { get; set; }
    public int Col { get; set; }

    /// <summary>
    /// コマンドが未割り当てかどうか
    /// </summary>
    [XmlIgnore]
    public bool IsEmpty => string.IsNullOrEmpty(FileName);

    /// <summary>
    /// Commandからプロパティをコピーして生成
    /// </summary>
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
