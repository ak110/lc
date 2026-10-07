using Launcher.Win32;

namespace Launcher.UI;

/// <summary>取得したアイコンをUIへ配送し、採用・破棄のどちらでも所有するアイコンを解放する。</summary>
public static class IconReceiver
{
    /// <summary>現在の世代のアイコンだけを適用する。適用側はアイコンを保持せず画像へコピーする。</summary>
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
