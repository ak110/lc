using System.Drawing;
using System.IO;
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
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
}
