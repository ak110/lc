using System.Diagnostics;
using System.Windows.Forms;

namespace Launcher.Infrastructure;

/// <summary>
/// アプリケーション的な処理。
/// </summary>
public static class AppBase
{
    static bool started;
    static bool restart;

    /// <summary>
    /// Program.Main()の開始時に呼ぶ処理。
    /// </summary>
    public static void Initialize()
    {
        Debug.Assert(!started);
        ErrorReporter errorReporter = ErrorReporter.Instance;
        errorReporter.ExitApplication += errorReporter_ExitApplication;
        errorReporter.RestartApplication += errorReporter_RestartApplication;
        errorReporter.Register();

        started = true;
    }

    /// <summary>
    /// 終了時に再起動させるようにする時に呼ぶ。
    /// </summary>
    public static void SetRestart()
    {
        Debug.Assert(started);
        restart = true;
    }

    /// <summary>
    /// 終了時の処理
    /// </summary>
    public static void OnExit()
    {
        Debug.Assert(started);
        started = false;
        if (restart)
        {
            Process.Start(Environment.ProcessPath!);
        }
    }

    /// <summary>
    /// Initialize(), OnExit()を呼ぶクラス。
    /// </summary>
    public sealed class Initializer : IDisposable
    {
        /// <summary>
        /// 初期化
        /// </summary>
        public Initializer()
        {
            AppBase.Initialize();
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            AppBase.OnExit();
        }
    }

    static void errorReporter_ExitApplication(object? sender, EventArgs e)
    {
        Application.Exit();
    }

    static void errorReporter_RestartApplication(object? sender, EventArgs e)
    {
        restart = true;
        Application.Exit();
    }

}
