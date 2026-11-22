using System.Diagnostics;

namespace Launcher.UI;

public static class AppBase
{
    static bool started;
    static bool restart;

    public static void Initialize()
    {
        Debug.Assert(!started);
        ErrorReporter errorReporter = ErrorReporter.Instance;
        errorReporter.ExitApplication += errorReporter_ExitApplication;
        errorReporter.RestartApplication += errorReporter_RestartApplication;
        errorReporter.Register();

        started = true;
    }

    public static void SetRestart()
    {
        Debug.Assert(started);
        restart = true;
    }

    public static void OnExit()
    {
        Debug.Assert(started);
        started = false;
        if (restart)
        {
            Process.Start(Environment.ProcessPath!);
        }
    }

    public sealed class Initializer : IDisposable
    {
        public Initializer()
        {
            AppBase.Initialize();
        }

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
