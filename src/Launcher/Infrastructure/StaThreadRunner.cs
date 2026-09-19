namespace Launcher.Infrastructure;

/// <summary>
/// バックグラウンドSTAスレッドで処理を開始する。
/// </summary>
public static class StaThreadRunner
{
    public static Thread Start(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var thread = new Thread(() => action())
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return thread;
    }
}
