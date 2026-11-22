namespace Launcher.Core;

public sealed class DragDropState
{
    sealed record ActiveDrag(ButtonEntry Entry, ButtonTab SourceTab, Point StartPoint);

    ActiveDrag? active;

    public ButtonEntry? DragEntry => active?.Entry;
    public ButtonTab? SourceTab => active?.SourceTab;
    public bool IsActive => active is not null;

    public void Start(ButtonEntry entry, ButtonTab tab, Point startPoint)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(tab);
        active = new ActiveDrag(entry, tab, startPoint);
    }

    public bool ShouldBeginDrag(Point current, Size dragSize) =>
        active is { StartPoint: var start } &&
        (Math.Abs((long)current.X - start.X) > dragSize.Width / 2 ||
         Math.Abs((long)current.Y - start.Y) > dragSize.Height / 2);

    public void Reset() => active = null;
}
