namespace Launcher.UI;

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
    /// フォームのハンドル再生成に依存せず、WinFormsのUIコンテキストへ配送する。
    /// 終了後は実行せず、UIスレッドの終了により配送できない例外はログへ残す。
    /// </summary>
    public static void SafeBeginInvoke(WindowsFormsSynchronizationContext context, Control lifetime, Action action)
    {
        if (lifetime.IsDisposed || lifetime.Disposing) return;
        try
        {
            context.Post(_ =>
            {
                if (lifetime.IsDisposed || lifetime.Disposing) return;
#pragma warning disable CA1031
                try { action(); }
                catch (Exception ex) { ErrorReporter.Instance.OnException(ex); }
#pragma warning restore CA1031
            }, null);
        }
        catch (InvalidOperationException ex)
        {
            ThreadPool.QueueUserWorkItem(_ => Launcher.Infrastructure.DiagnosticLog.Error("UI.DispatchFailed", ex,
                "らんちゃの終了中に記録された場合は対応不要。起動中に繰り返し記録され、操作が反映されない場合はらんちゃを再起動する"));
        }
    }

    public static void SafeBeginInvoke(Control control, Action action, Action? onSkipped = null)
    {
        int completed = 0;
        EventHandler? handleDestroyed = null;
        bool Complete()
        {
            if (Interlocked.Exchange(ref completed, 1) != 0) return false;
            control.HandleDestroyed -= handleDestroyed;
            return true;
        }
        void Skip()
        {
            if (Complete()) onSkipped?.Invoke();
        }
        handleDestroyed = (_, _) => Skip();
        // ポストが成功してもハンドル破棄で実行されない場合があるため、完了まで追跡する。
        control.HandleDestroyed += handleDestroyed;
        if (control.IsDisposed || control.Disposing || !control.IsHandleCreated)
        {
            Skip();
            return;
        }
        try
        {
            control.BeginInvoke(new MethodInvoker(() =>
            {
                if (!Complete()) return;
                try
                {
                    if (control.IsDisposed || control.Disposing || !control.IsHandleCreated)
                    {
                        onSkipped?.Invoke();
                    }
                    else
                    {
                        action();
                    }
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    ErrorReporter.Instance.OnException(ex);
                }
            }));
        }
        catch (InvalidOperationException)
        {
            Skip();
        }
    }
}
