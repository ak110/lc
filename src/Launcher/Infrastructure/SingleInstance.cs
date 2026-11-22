using System.Diagnostics;
using Launcher.Win32;

namespace Launcher.Infrastructure;

public sealed class SingleInstance : IDisposable
{
    Mutex? mutex;
    bool firstRun;

    public SingleInstance() : this(GetMutexName()) { }

    private static string GetMutexName()
    {
        string moduleFileName = Environment.ProcessPath!;
        string mutexName = moduleFileName.ToLowerInvariant().Replace('\\', '/');
        return mutexName;
    }

    public SingleInstance(string mutexName)
    {
        try
        {
            mutex = new Mutex(false, mutexName);
            firstRun = mutex.WaitOne(0, false);
        }
        catch (IOException)
        {
            firstRun = false;
        }
        catch (UnauthorizedAccessException)
        {
            firstRun = false;
        }
        catch (AbandonedMutexException)
        {
            firstRun = true;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mutex?.Close();
    }

    public bool FirstRun
    {
        get { return firstRun; }
    }

    public static void SetActive()
    {
        foreach (Process p in GetProcesses())
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(p.MainWindowHandle);
                    NativeMethods.BringWindowToTop(p.MainWindowHandle);
                }
            }
            finally
            {
                p.Dispose();
            }
        }
    }

    #region DllImport



    #endregion

    public static List<Process> GetProcesses()
    {
        List<Process> list = [];
        using Process current = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(current.ProcessName);
        foreach (Process p in processes)
        {
            if (p.Id == current.Id)
            {
                p.Dispose();
                continue;
            }
            if (!string.Equals(p.MainModule?.FileName,
                current.MainModule?.FileName, StringComparison.OrdinalIgnoreCase))
            {
                p.Dispose();
                continue;
            }
            list.Add(p);
        }
        return list;
    }
}
