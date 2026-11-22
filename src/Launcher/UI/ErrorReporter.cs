using System.Text;
using Launcher.Infrastructure;

namespace Launcher.UI;

public sealed class ErrorReporter
{
    static readonly ErrorReporter instance = new();

    internal const string ContinueGuidance =
        "続行すると、保存していない編集や実行中の操作が失われることがあります。"
        + "続行した場合も、早めに「終了」または「再起動」を選んでください。";

    internal const string ReportGuidance =
        "繰り返し発生する場合は https://github.com/ak110/lc/issues へ報告し、"
        + "らんちゃのフォルダーのlogsにある診断ログを添付してください。"
        + "診断ログには実行したコマンドの表示名が記録されるため、公開してよい内容か確認してから添付してください。";

    public static ErrorReporter Instance => instance;

    Control? owner;
    Func<IWin32Window?>? getVisibleOwner;

    object lockObject = new();
    bool localLock;

    private ErrorReporter()
    {
    }

    public void SetOwner(Control form, Func<IWin32Window?>? getVisibleOwner = null)
    {
        if (owner is not null) owner.Disposed -= form_Disposed;
        owner = form;
        this.getVisibleOwner = getVisibleOwner;
        form.Disposed += form_Disposed;
    }

    public event EventHandler? RestartApplication;

    public event EventHandler? ExitApplication;

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

    void Application_ThreadException(object? sender, ThreadExceptionEventArgs e)
    {
        OnException(e.Exception);
    }

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
                case DialogResult.Abort:
                    ExitApplication?.Invoke(this, EventArgs.Empty);
                    break;
                case DialogResult.Retry:
                    RestartApplication?.Invoke(this, EventArgs.Empty);
                    break;
                case DialogResult.None:
                case DialogResult.Ignore:
                    break;
            }
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Fail(ex.ToString());
        }
#pragma warning restore CA1031
        finally
        {
            localLock = false;
        }
    }

    private DialogResult ShowReporterForm(Exception ex)
    {
        using var form = new ErrorReporterForm(ex);
        var visibleOwner = getVisibleOwner is not null ? getVisibleOwner() : owner;
        return form.ShowDialogOver(visibleOwner);
    }

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
