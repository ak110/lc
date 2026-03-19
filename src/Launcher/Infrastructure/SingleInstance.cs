using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Launcher.Infrastructure;

/// <summary>
/// 多重起動対策などを行う
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
    /// mutex名は自身へのパスを元に作成。
    /// </summary>
    public SingleInstance() : this(GetMutexName()) { }

    /// <summary>
    /// 自身へのパスを元にmutex名を作成
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
    /// ここで多重起動の排他処理や初回起動なのかの判定が行われる。
    /// </summary>
    /// <param name="mutexName">mutex名</param>
    public SingleInstance(string mutexName)
    {
        try
        {
            mutex = new Mutex(false, mutexName);
            firstRun = mutex.WaitOne(0, false);
        }
        catch (IOException)
        {
            // 多重起動な事にしてしまう。
            firstRun = false;
        }
        catch (UnauthorizedAccessException)
        {
            // 多重起動な事にしてしまう。
            firstRun = false;
        }
        catch (AbandonedMutexException)
        {
            // 前回のプロセスが異常終了した場合。Mutexは取得済みなので初回起動扱い。
            firstRun = true;
        }
    }

    /// <summary>
    /// あとしまつ。
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
    /// 既に起動済みなウィンドウをアクティブにする
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
    /// 自分のモジュールファイル名と同じファイル名なプロセスを列挙（多分０～１個のはず）
    /// </summary>
    public static List<Process> GetProcesses()
    {
        List<Process> list = [];
        using Process current = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(current.ProcessName);
        foreach (Process p in processes)
        {
            // 自分なら無視
            if (p.Id == current.Id)
            {
                p.Dispose();
                continue;
            }
            // ファイル名違うなら無視
            if (!string.Equals(p.MainModule?.FileName,
                current.MainModule?.FileName, StringComparison.OrdinalIgnoreCase))
            {
                p.Dispose();
                continue;
            }
            // 追加（呼び出し元でDisposeする責務）
            list.Add(p);
        }
        return list;
    }
}
