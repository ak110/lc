using System.Drawing;

namespace Launcher.Core;

/// <summary>
/// ButtonLauncherFormのビジネスロジックを担当するPresenter。
/// グリッドサイズ計算、D&D状態管理、ボタンスワップロジックを集約する。
/// </summary>
public static class ButtonLauncherPresenter
{
    /// <summary>グリッドサイズ（列数・行数）</summary>
    public record GridSize(int Columns, int Rows);

    /// <summary>
    /// クライアント領域とボタンサイズからグリッドの列数・行数を計算する。
    /// </summary>
    /// <param name="clientWidth">クライアント領域の幅</param>
    /// <param name="clientHeight">クライアント領域の高さ</param>
    /// <param name="toolStripHeight">ツールストリップの高さ</param>
    /// <param name="tabHeaderHeight">タブヘッダーの高さ</param>
    /// <param name="buttonWidth">ボタンの幅</param>
    /// <param name="buttonHeight">ボタンの高さ</param>
    public static GridSize CalculateGridSize(
        int clientWidth, int clientHeight,
        int toolStripHeight, int tabHeaderHeight,
        int buttonWidth, int buttonHeight)
    {
        int cols = Math.Max(1, clientWidth / buttonWidth);
        int rows = Math.Max(1, (clientHeight - toolStripHeight - tabHeaderHeight) / buttonHeight);
        return new GridSize(cols, rows);
    }

    /// <summary>
    /// D&Dでボタンをスワップする。
    /// ドラッグ元にはドロップ先の既存エントリを配置し、ドロップ先にはドラッグ中のエントリを配置する。
    /// ドロップ先が空の場合、ドラッグ元は空になる。
    /// </summary>
    /// <param name="sourceTab">ドラッグ元のタブ</param>
    /// <param name="srcRow">ドラッグ元の行</param>
    /// <param name="srcCol">ドラッグ元の列</param>
    /// <param name="destTab">ドロップ先のタブ</param>
    /// <param name="destRow">ドロップ先の行</param>
    /// <param name="destCol">ドロップ先の列</param>
    /// <param name="dragEntry">ドラッグ中のエントリ</param>
    public static void SwapButtons(
        ButtonTab sourceTab, int srcRow, int srcCol,
        ButtonTab destTab, int destRow, int destCol,
        ButtonEntry dragEntry)
    {
        // 移動先の既存エントリ
        var destEntry = destTab.GetButton(destRow, destCol);

        // ドロップ先にドラッグ中のエントリを配置
        destTab.SetButton(destRow, destCol, ButtonEntry.FromCommand(dragEntry, destRow, destCol));

        // ドラッグ元にドロップ先の既存エントリを配置（空なら削除）
        if (destEntry != null && !destEntry.IsEmpty)
        {
            sourceTab.SetButton(srcRow, srcCol, ButtonEntry.FromCommand(destEntry, srcRow, srcCol));
        }
        else
        {
            sourceTab.SetButton(srcRow, srcCol, null);
        }
    }
}

/// <summary>
/// D&D操作の状態を管理するクラス。
/// 4つの個別フィールドを1つのクラスに凝集する。
/// </summary>
public class DragDropState
{
    /// <summary>ドラッグ中のButtonEntry</summary>
    public ButtonEntry? DragEntry { get; private set; }

    /// <summary>ドラッグ元のタブ</summary>
    public ButtonTab? SourceTab { get; private set; }

    /// <summary>ドラッグ開始時のマウス位置</summary>
    public Point DragStartPoint { get; private set; }

    /// <summary>D&D操作がアクティブかどうか</summary>
    public bool IsActive => DragEntry != null;

    /// <summary>
    /// D&D操作を開始する。
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
    /// D&D状態をリセットする。
    /// </summary>
    public void Reset()
    {
        DragEntry = null;
        SourceTab = null;
        DragStartPoint = Point.Empty;
    }
}
