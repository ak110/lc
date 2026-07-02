using System.Windows.Forms;

namespace Launcher.Infrastructure;

/// <summary>
/// UIスレッドへ<see cref="Control.BeginInvoke(System.Delegate)"/>で非同期ポストするヘルパー。
/// 渡した処理内で発生した未捕捉例外は<see cref="ErrorReporter.Instance"/>へ回送し、
/// <see cref="System.Windows.Forms.Application.ThreadException"/>と同等の扱いに揃える。
/// <see cref="Control.IsHandleCreated"/>・<see cref="Control.IsDisposed"/>のガードを含む。
/// 詳細は.claude/rules/threading.md「UIスレッドBeginInvoke内例外の回送」節を参照。
/// </summary>
public static class UiThreadDispatcher
{
    /// <summary>
    /// <paramref name="control"/>のUIスレッドへ<paramref name="action"/>をポストする。
    /// controlが破棄済みまたはハンドル未作成の場合は<paramref name="onSkipped"/>を同期呼び出しする。
    /// <paramref name="onSkipped"/>がnullなら何もしない。
    /// action内の未捕捉例外は<see cref="ErrorReporter.OnException(System.Exception)"/>へ回送する。
    /// リソース解放を伴うactionを渡す場合は、ガード発火時に同処理を実行する<paramref name="onSkipped"/>を渡す。
    /// </summary>
    public static void SafeBeginInvoke(Control control, Action action, Action? onSkipped = null)
    {
        if (control.IsDisposed || !control.IsHandleCreated)
        {
            onSkipped?.Invoke();
            return;
        }
        try
        {
            control.BeginInvoke(new MethodInvoker(() =>
            {
                try
                {
                    action();
                }
#pragma warning disable CA1031 // UI境界: 未捕捉例外はErrorReporterへ回送し、Application.ThreadExceptionと同扱いにする
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    ErrorReporter.Instance.OnException(ex);
                }
            }));
        }
        catch (InvalidOperationException)
        {
            // ガード後にハンドルが破棄された場合のレース。onSkippedへフォールバックする
            onSkipped?.Invoke();
        }
    }
}
