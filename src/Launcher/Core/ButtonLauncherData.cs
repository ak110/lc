#nullable disable
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

/// <summary>
/// ボタン型ランチャーのデータ
/// </summary>
public class ButtonLauncherData : ConfigStore
{
    public List<ButtonTab> Tabs { get; set; } = new List<ButtonTab>();
    public int DefaultTabIndex { get; set; } = 0;
    public int Columns { get; set; } = 7;
    public int Rows { get; set; } = 7;
    public bool IsLocked { get; set; } = false;
    public Point WindowPos { get; set; } = Point.Empty;
    public Size WindowSize { get; set; } = Size.Empty;

    /// <summary>
    /// 書き込み
    /// </summary>
    public void Serialize()
    {
        Serialize(".btns.cfg");
    }

    /// <summary>
    /// 読み込み
    /// </summary>
    public static ButtonLauncherData Deserialize()
    {
        try
        {
            return Deserialize<ButtonLauncherData>(".btns.cfg");
        }
        catch
        {
            return new ButtonLauncherData();
        }
    }
}

/// <summary>
/// ボタンランチャーのタブ
/// </summary>
public class ButtonTab
{
    public string Name { get; set; } = "";
    public List<ButtonEntry> Buttons { get; set; } = new List<ButtonEntry>();

    /// <summary>
    /// 指定位置のボタンを取得。未割り当てならnull。
    /// </summary>
    public ButtonEntry GetButton(int row, int col)
    {
        return Buttons.Find(b => b.Row == row && b.Col == col);
    }

    /// <summary>
    /// 指定位置のボタンを設定。nullなら削除。
    /// </summary>
    public void SetButton(int row, int col, ButtonEntry entry)
    {
        Buttons.RemoveAll(b => b.Row == row && b.Col == col);
        if (entry != null)
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
public class ButtonEntry : Command
{
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
        var entry = new ButtonEntry
        {
            Row = row,
            Col = col,
            Name = cmd.Name,
            FileName = cmd.FileName,
            Param = cmd.Param,
            WorkDir = cmd.WorkDir,
            Show = cmd.Show,
            Priority = cmd.Priority,
            RunAsAdmin = cmd.RunAsAdmin,
        };
        return entry;
    }
}
