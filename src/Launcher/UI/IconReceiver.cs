using Launcher.Win32;

namespace Launcher.UI;

public static class IconReceiver
{
    public static void Receive(Control control, AsyncIconLoader loader, IconLoadedEventArgs result, Action<Icon> apply)
    {
        if (result.Generation != loader.Generation || loader.IsDisposed)
        {
            result.Icon?.Dispose();
            return;
        }
        UiThreadDispatcher.SafeBeginInvoke(control, () =>
        {
            using var icon = result.Icon;
            if (icon is null || loader.IsDisposed || result.Generation != loader.Generation) return;
            apply(icon);
        }, () => result.Icon?.Dispose());
    }
}
