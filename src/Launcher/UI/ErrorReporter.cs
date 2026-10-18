using System.Text;
using Launcher.Infrastructure;

namespace Launcher.UI;

/// <summary>
/// エラー報告処理。
/// </summary>
public sealed class ErrorReporter
{
    static readonly ErrorReporter instance = new();

    /// <summary>想定外の例外の後に続行してよいかの案内。報告画面に常に表示する</summary>
    internal const string ContinueGuidance =
        "続行すると、保存していない編集や実行中の操作が失われることがあります。"
        + "続行した場合も、早めに「終了」または「再起動」を選んでください。";

    /// <summary>不具合の報告先と添付する診断ログの場所・注意。報告画面と致命的なエラーの画面に表示する</summary>
    internal const string ReportGuidance =
        "繰り返し発生する場合は https://github.com/ak110/lc/issues へ報告し、"
        + "らんちゃのフォルダーのlogsにある診断ログを添付してください。"
        + "診断ログには実行したコマンドの表示名が記録されるため、公開してよい内容か確認してから添付してください。";

    /// <summary>
    /// シングルトンインスタンスを取得する。
    /// </summary>
    public static ErrorReporter Instance => instance;

    Control? owner;
    Func<IWin32Window?>? getVisibleOwner;

    object lockObject = new();
    bool localLock;

    private ErrorReporter()
    {
    }

    /// <summary>
    /// Ownerを設定し、破棄時の自動解除を登録する。
    /// </summary>
    public void SetOwner(Control form, Func<IWin32Window?>? getVisibleOwner = null)
    {
        if (owner is not null) owner.Disposed -= form_Disposed;
        owner = form;
        this.getVisibleOwner = getVisibleOwner;
        form.Disposed += form_Disposed;
    }

    /// <summary>
    /// アプリケーションの再起動イベント。必ずハンドラを登録すること。
    /// </summary>
    public event EventHandler? RestartApplication;

    /// <summary>
    /// アプリケーションの終了イベント。必ずハンドラを登録すること。
    /// 通常はApplication.Exit()を呼び出す実装でよい。
    /// </summary>
    public event EventHandler? ExitApplication;

    /// <summary>
    /// 例外ハンドラを登録する。
    /// </summary>
    public void Register()
    {
        Application.ThreadException += Application_ThreadException;
    }

    void form_Disposed(object? sender, EventArgs e)
    {
        if (sender is Control control) control.Disposed -= form_Disposed;
        owner = null;
        getVisibleOwner = null;
    }

    /// <summary>
    /// トラップされなかった例外が発生すると呼び出されるイベント。
    /// </summary>
    void Application_ThreadException(object? sender, ThreadExceptionEventArgs e)
    {
        OnException(e.Exception);
    }

    /// <summary>
    /// 例外を処理する。
    /// </summary>
    /// <param name="e">例外オブジェクト</param>
    public void OnException(Exception e)
    {
        if (owner is { IsDisposed: false, InvokeRequired: true } dispatcher)
        {
            UiThreadDispatcher.SafeBeginInvoke(dispatcher, () => OnException(e));
            return;
        }
        DiagnosticLog.Error("UI.UnexpectedException", e, "画面の案内に従い、報告画面で終了・再起動・続行のいずれかを選ぶ");
        lock (lockObject)
        {
            if (localLock)
            {
                return;
            }
            localLock = true;
        }
        try
        {
            switch (ShowReporterForm(e))
            {
                case DialogResult.Abort: // 終了
                    ExitApplication?.Invoke(this, EventArgs.Empty);
                    break;
                case DialogResult.Retry: // 再起動
                    RestartApplication?.Invoke(this, EventArgs.Empty);
                    break;
                case DialogResult.None:
                case DialogResult.Ignore: // 続行
                    break;
            }
        }
        // エラーレポーター自体の例外ハンドリング (最終防御ライン)
#pragma warning disable CA1031 // エラーレポーターは最終防御ラインのため全例外をキャッチする
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Fail(ex.ToString());
            // 再帰呼び出し防止のため例外を無視する。
        }
#pragma warning restore CA1031
        finally
        {
            localLock = false;
        }
    }

    /// <summary>
    /// ErrorReporterForm を表示する。
    /// </summary>
    private DialogResult ShowReporterForm(Exception ex)
    {
        using var form = new ErrorReporterForm(ex);
        var visibleOwner = getVisibleOwner is not null ? getVisibleOwner() : owner;
        return form.ShowDialogOver(visibleOwner);
    }

    /// <summary>
    /// 例外の詳細メッセージを組み立てて返す。
    /// </summary>
    public static string GetDetailMessage(Exception e)
    {
        var builder = new StringBuilder();
        AppendExceptionString(builder, e);
        return builder.ToString();
    }

    private static void AppendExceptionString(StringBuilder builder, Exception e)
    {
        builder.Append(e.GetType().ToString());
        builder.AppendLine(":");
        if (e.Message is not null)
        {
            builder.AppendLine(e.Message.TrimEnd());
            builder.AppendLine();
        }
        if (e.StackTrace is not null)
        {
            builder.AppendLine("スタックトレース:");
            builder.AppendLine(e.StackTrace.TrimEnd());
            builder.AppendLine();
        }
        if (e.Source is not null)
        {
            builder.AppendLine("Source:");
            builder.AppendLine(e.Source);
            builder.AppendLine();
        }
        Exception? ie = e.InnerException;
        if (ie is not null)
        {
            builder.Append("InnerException -> ");
            AppendExceptionString(builder, ie);
            builder.AppendLine(" <- InnerException");
        }
        Exception? be = e.GetBaseException();
        if (be is not null && be != ie && !object.Equals(be, e))
        {
            builder.Append("BaseException -> ");
            AppendExceptionString(builder, be);
            builder.Append(" <- BaseException");
        }
    }
}
