namespace Launcher.Infrastructure;

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
