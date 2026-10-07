namespace Launcher.Core;

/// <summary>
/// D&amp;D操作の状態を管理するクラス。
/// 4つの個別フィールドを1つのクラスに凝集する。
/// </summary>
public sealed class DragDropState
{
    /// <summary>ドラッグ中のButtonEntry</summary>
    public ButtonEntry? DragEntry { get; private set; }

    /// <summary>ドラッグ元のタブ</summary>
    public ButtonTab? SourceTab { get; private set; }

    /// <summary>ドラッグ開始時のマウス位置</summary>
    public Point DragStartPoint { get; private set; }

    /// <summary>D&amp;D操作がアクティブかどうか</summary>
    public bool IsActive => DragEntry is not null;

    /// <summary>
    /// D&amp;D操作を開始する。
    /// </summary>
    public void Start(ButtonEntry entry, ButtonTab tab, Point startPoint)
    {
        DragEntry = entry;
        SourceTab = tab;
        DragStartPoint = startPoint;
    }

    /// <summary>
    /// マウス移動量がドラッグ閾値を超えたか判定する。
    /// </summary>
    public bool ShouldBeginDrag(Point current, Size dragSize)
    {
        return Math.Abs(current.X - DragStartPoint.X) > dragSize.Width / 2 ||
               Math.Abs(current.Y - DragStartPoint.Y) > dragSize.Height / 2;
    }

    /// <summary>
    /// D&amp;D状態をリセットする。
    /// </summary>
    public void Reset()
    {
        DragEntry = null;
        SourceTab = null;
        DragStartPoint = Point.Empty;
    }
}
