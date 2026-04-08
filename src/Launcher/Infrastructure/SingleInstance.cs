using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Launcher.Infrastructure;

/// <summary>
/// 多重起動対策などを行う。
/// </summary>
/// <example>
/// using (SingleInstance singleInstance = new SingleInstance()) {
///		if (!singleInstance.FirstRun) {
///			singleInstance.SetActive();
///			return;
///		}
///		メインの処理を記述
/// }
/// </example>
public sealed class SingleInstance : IDisposable
{
    Mutex? mutex;
    bool firstRun;

    /// <summary>
    /// コンストラクタ。
    /// mutex 名は自身へのパスを元に作成する。
    /// </summary>
    public SingleInstance() : this(GetMutexName()) { }

    /// <summary>
    /// 自身へのパスを元に mutex 名を作成する。
    /// </summary>
    private static string GetMutexName()
    {
        //string moduleFileName = Application.ExecutablePath;
        string moduleFileName = Environment.ProcessPath!;
        //moduleFileName = Path.GetFullPath(Environment.ExpandEnvironmentVariables(moduleFileName));
        string mutexName = moduleFileName.ToLower().Replace('\\', '/');
        return mutexName;
    }

    /// <summary>
    /// コンストラクタ。
    /// 多重起動の排他処理と初回起動かどうかの判定をここで行う。
    /// </summary>
    /// <param name="mutexName">mutex 名</param>
    public SingleInstance(string mutexName)
    {
        try
        {
            mutex = new Mutex(false, mutexName);
            firstRun = mutex.WaitOne(0, false);
        }
        catch (IOException)
        {
            // 多重起動として扱う。
            firstRun = false;
        }
        catch (UnauthorizedAccessException)
        {
            // 多重起動として扱う。
            firstRun = false;
        }
        catch (AbandonedMutexException)
        {
            // 前回のプロセスが異常終了した場合。Mutex は取得済みのため初回起動として扱う。
            firstRun = true;
        }
    }

    /// <summary>
    /// 後始末を行う。
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mutex?.Close();
    }

    /// <summary>
    /// 初回起動なのかどうか。
    /// </summary>
    public bool FirstRun
    {
        get { return firstRun; }
    }

    /// <summary>
    /// 既に起動済みのウィンドウをアクティブにする。
    /// </summary>
    public static void SetActive()
    {
        foreach (Process p in GetProcesses())
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    BringWindowToTop(p.MainWindowHandle);
                }
            }
            finally
            {
                p.Dispose();
            }
        }
    }

    #region DllImport

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool BringWindowToTop(IntPtr hWnd);

    #endregion

    /// <summary>
    /// 自身と同じモジュールファイル名のプロセスを列挙する (通常は 0〜1 個となる)。
    /// </summary>
    public static List<Process> GetProcesses()
    {
        List<Process> list = [];
        using Process current = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(current.ProcessName);
        foreach (Process p in processes)
        {
            // 自分自身は無視する。
            if (p.Id == current.Id)
            {
                p.Dispose();
                continue;
            }
            // ファイル名が異なる場合は無視する。
            if (!string.Equals(p.MainModule?.FileName,
                current.MainModule?.FileName, StringComparison.OrdinalIgnoreCase))
            {
                p.Dispose();
                continue;
            }
            // 追加 (呼び出し元でDisposeする責務)
            list.Add(p);
        }
        return list;
    }
}
