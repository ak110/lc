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
#pragma warning disable CA1031 // UI境界: 配送した処理の未捕捉例外を通常の報告処理へ渡す
                try { action(); }
                catch (Exception ex) { ErrorReporter.Instance.OnException(ex); }
#pragma warning restore CA1031
            }, null);
        }
        catch (InvalidOperationException ex)
        {
            // UIスレッド終了時には同期通知へ戻さない（フック内のモーダル表示を避ける）。
            ThreadPool.QueueUserWorkItem(_ => Launcher.Infrastructure.DiagnosticLog.Error("UI.DispatchFailed", ex));
        }
    }

    /// <summary>
    /// <paramref name="control"/>のUIスレッドへ<paramref name="action"/>をポストする。
    /// controlが破棄済みまたはハンドル未作成の場合は<paramref name="onSkipped"/>を同期呼び出しする。
    /// <paramref name="onSkipped"/>がnullなら何もしない。
    /// action内の未捕捉例外は<see cref="ErrorReporter.OnException(System.Exception)"/>へ回送する。
    /// リソース解放を伴うactionを渡す場合は、ガード発火時に同処理を実行する<paramref name="onSkipped"/>を渡す。
    /// </summary>
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
            Skip();
        }
    }
}
