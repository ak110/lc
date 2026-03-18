using System.Text;
using System.Windows.Forms;
using Launcher.UI;

namespace Launcher.Infrastructure;

/// <summary>
/// エラー報告処理
/// </summary>
public class ErrorReporter
{
    static readonly ErrorReporter instance = new ErrorReporter();

    /// <summary>
    /// Singletonなインスタンスの取得
    /// </summary>
    public static ErrorReporter Instance => instance;

    Control? owner;

    object lockObject = new object();
    bool localLock;

    private ErrorReporter()
    {
    }

    /// <summary>
    /// オーナーウィンドウ
    /// </summary>
    public Control? Owner
    {
        get { return owner; }
        set { owner = value; }
    }

    /// <summary>
    /// formをOwnerに登録。
    /// </summary>
    public void SetOwner(Control form)
    {
        owner = form;
        form.Disposed += new EventHandler(form_Disposed);
    }

    /// <summary>
    /// アプリケーションの再起動を行う。
    /// 必ず実装すべし。
    /// </summary>
    public event EventHandler? RestartApplication;

    /// <summary>
    /// アプリケーションの終了を行う。
    /// 必ず実装すべし。
    /// </summary>
    /// <remarks>
    /// Application.Exit()するだけでいい気がする…。
    /// </remarks>
    public event EventHandler? ExitApplication;

    /// <summary>
    /// ハンドラを登録する。
    /// </summary>
    public void Register()
    {
        Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
    }

    /// <summary>
    /// ハンドラを登録解除する。
    /// </summary>
    public void UnRegister()
    {
        Application.ThreadException -= new ThreadExceptionEventHandler(Application_ThreadException);
    }

    void form_Disposed(object? sender, EventArgs e)
    {
        owner = null;
    }

    /// <summary>
    /// トラップされなかった例外が発生すると呼び出されるイベント。
    /// </summary>
    void Application_ThreadException(object? sender, ThreadExceptionEventArgs e)
    {
        OnException(e.Exception);
    }

    /// <summary>
    /// 例外に対する処理
    /// </summary>
    /// <param name="e">例外オブジェクト</param>
    public void OnException(Exception e)
    {
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
        // エラーレポーター自体の例外ハンドリング（最終防御ライン）
#pragma warning disable CA1031 // エラーレポーターは最終防御ラインのため全例外をキャッチする
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Fail(ex.ToString());
            // ここに再帰すると面倒なので。。
        }
#pragma warning restore CA1031
        finally
        {
            localLock = false;
        }
    }

    /// <summary>
    /// ErrorReporterFormを表示
    /// </summary>
    private DialogResult ShowReporterForm(Exception ex)
    {
        using (ErrorReporterForm form = new ErrorReporterForm(ex))
        {
            if (owner != null)
            {
                if (owner.Visible)
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                }
                return form.ShowDialog(owner);
            }
            return form.ShowDialog();
        }
    }

    /// <summary>
    /// 例外の詳細メッセージを組み立てて返す
    /// </summary>
    public static string GetDetailMessage(Exception e)
    {
        StringBuilder builder = new StringBuilder();
        AppendExceptionString(builder, e);
        return builder.ToString();
    }

    private static void AppendExceptionString(StringBuilder builder, Exception e)
    {
        builder.Append(e.GetType().ToString());
        builder.AppendLine(":");
        if (e.Message != null)
        {
            builder.AppendLine(e.Message.TrimEnd());
            builder.AppendLine();
        }
        if (e.StackTrace != null)
        {
            builder.AppendLine("スタックトレース:");
            builder.AppendLine(e.StackTrace.TrimEnd());
            builder.AppendLine();
        }
        if (e.Source != null)
        {
            builder.AppendLine("Source:");
            builder.AppendLine(e.Source);
            builder.AppendLine();
        }
        Exception? ie = e.InnerException;
        if (ie != null)
        {
            builder.Append("InnerException -> ");
            AppendExceptionString(builder, ie);
            builder.AppendLine(" <- InnerException");
        }
        Exception? be = e.GetBaseException();
        if (be != null && be != ie && !object.Equals(be, e))
        {
            builder.Append("BaseException -> ");
            AppendExceptionString(builder, be);
            builder.Append(" <- BaseException");
        }
    }
}
