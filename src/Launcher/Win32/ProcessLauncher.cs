using System.Runtime.InteropServices;
using Launcher.Core;
using Launcher.Infrastructure;

namespace Launcher.Win32;

/// <summary>
/// .NET の ShellExecuteEx() ラッパーは WindowStyle 周辺の挙動が要件に合わないため、独自に実装する。
/// .NET のインターフェースにおおむね準拠するが、拡張機能は省略している。
/// </summary>
public static class ProcessLauncher
{
    internal static System.Diagnostics.ProcessPriorityClass ToPriorityClass(ProcessPriorityLevel level) => level switch
    {
        ProcessPriorityLevel.RealTime => System.Diagnostics.ProcessPriorityClass.RealTime,
        ProcessPriorityLevel.High => System.Diagnostics.ProcessPriorityClass.High,
        ProcessPriorityLevel.AboveNormal => System.Diagnostics.ProcessPriorityClass.AboveNormal,
        ProcessPriorityLevel.Normal => System.Diagnostics.ProcessPriorityClass.Normal,
        ProcessPriorityLevel.BelowNormal => System.Diagnostics.ProcessPriorityClass.BelowNormal,
        ProcessPriorityLevel.Idle => System.Diagnostics.ProcessPriorityClass.Idle,
        _ => System.Diagnostics.ProcessPriorityClass.Normal,
    };

    internal static void Start(ShellProcessStartInfo info)
    {
        IntPtr hProcess = InnerStart(info);
        try
        {
            if (hProcess == IntPtr.Zero) return;
            var priority = ToPriorityClass(info.Priority);
            uint priorityValue = (uint)priority;
            if (!SetPriorityClass(hProcess, priorityValue))
            {
                // プロセスは起動済みのため優先度設定失敗を例外扱いにしない。
                // GetLastError を保存してからログ記録する。
                var ex = new System.ComponentModel.Win32Exception();
                DiagnosticLog.Warn(
                    "Process.SetPriority",
                    $"SetPriorityClass failed (priority={priority}): {ex.GetType().Name}: {ex.Message}");
            }
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    private static IntPtr InnerStart(ShellProcessStartInfo info)
    {
        var shinfo = new SHELLEXECUTEINFO();
        shinfo.cbSize = Marshal.SizeOf<SHELLEXECUTEINFO>();
        shinfo.fMask = SEE_MASK_NOCLOSEPROCESS;
        if (info.CreateNoWindow)
        {
            shinfo.fMask |= SEE_MASK_NO_CONSOLE;
        }
        if (!info.ErrorDialog)
        {
            shinfo.fMask |= SEE_MASK_FLAG_NO_UI;
        }
        shinfo.hwnd = info.ErrorDialogParentHandle;
        shinfo.lpVerb = info.Verb;
        shinfo.lpFile = info.FileName;
        shinfo.lpParameters = info.Arguments;
        shinfo.lpDirectory = info.WorkingDirectory;
        shinfo.nShow = info.WindowStyle switch
        {
            WindowStyle.Normal => SW_SHOWNORMAL,
            WindowStyle.Minimized => SW_SHOWMINIMIZED,
            WindowStyle.Maximized => SW_SHOWMAXIMIZED,
            WindowStyle.NoActivate => SW_SHOWNOACTIVATE,
            WindowStyle.MinimizedNoActivate => SW_SHOWMINNOACTIVE,
            WindowStyle.Hidden => SW_HIDE,
            _ => SW_SHOWNORMAL,
        };
        shinfo.hInstApp = IntPtr.Zero;
        shinfo.lpIDList = IntPtr.Zero;
        shinfo.lpClass = null;
        shinfo.hkeyClass = IntPtr.Zero;
        shinfo.dwHotKey = 0;
        shinfo.hIcon = IntPtr.Zero;
        shinfo.hProcess = IntPtr.Zero;

        if (!ShellExecuteEx(ref shinfo))
        {
            // Win32Exception 構築を先に行い直前のエラー情報を保存する。
            // SEE_MASK_NOCLOSEPROCESS指定時は失敗経路でもhProcessが非ゼロで返る場合があるため、
            // ハンドルリーク回避のためCloseHandleする。
            // 詳細は.claude/rules/win32-interop.md「ShellExecuteEx失敗時のhProcess解放」節を参照。
            var ex = new System.ComponentModel.Win32Exception();
            if (shinfo.hProcess != IntPtr.Zero)
            {
                CloseHandle(shinfo.hProcess);
            }
            throw ex;
        }
        return shinfo.hProcess;
    }

    #region ShellExecuteEx 関連の P/Invoke 定義

    const int SW_HIDE = 0;
    const int SW_SHOWNORMAL = 1;
    const int SW_SHOWMINIMIZED = 2;
    const int SW_SHOWMAXIMIZED = 3;
    const int SW_SHOWNOACTIVATE = 4;
    const int SW_SHOW = 5;
    const int SW_MINIMIZE = 6;
    const int SW_SHOWMINNOACTIVE = 7;
    const int SW_SHOWNA = 8;
    const int SW_RESTORE = 9;
    const int SW_SHOWDEFAULT = 10;
    const int SW_FORCEMINIMIZE = 11;

    const uint SEE_MASK_CLASSNAME = 0x00000001;
    const uint SEE_MASK_CLASSKEY = 0x00000003;
    const uint SEE_MASK_IDLIST = 0x00000004;
    const uint SEE_MASK_INVOKEIDLIST = 0x0000000c;
    const uint SEE_MASK_ICON = 0x00000010;
    const uint SEE_MASK_HOTKEY = 0x00000020;
    const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
    const uint SEE_MASK_CONNECTNETDRV = 0x00000080;
    const uint SEE_MASK_FLAG_DDEWAIT = 0x00000100;
    const uint SEE_MASK_DOENVSUBST = 0x00000200;
    const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
    const uint SEE_MASK_UNICODE = 0x00004000;
    const uint SEE_MASK_NO_CONSOLE = 0x00008000;
    const uint SEE_MASK_HMONITOR = 0x00200000;
    const uint SEE_MASK_FLAG_LOG_USAGE = 0x04000000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPTStr)] public string? lpVerb;
        [MarshalAs(UnmanagedType.LPTStr)] public string? lpFile;
        [MarshalAs(UnmanagedType.LPTStr)] public string? lpParameters;
        [MarshalAs(UnmanagedType.LPTStr)] public string? lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPTStr)] public string? lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon; // hMonitor
        public IntPtr hProcess;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO shinfo);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    public const uint ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000;
    public const uint BELOW_NORMAL_PRIORITY_CLASS = 0x00004000;
    public const uint HIGH_PRIORITY_CLASS = 0x00000080;
    public const uint IDLE_PRIORITY_CLASS = 0x00000040;
    public const uint NORMAL_PRIORITY_CLASS = 0x00000020;
    public const uint REALTIME_PRIORITY_CLASS = 0x00000100;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

    #endregion
}
