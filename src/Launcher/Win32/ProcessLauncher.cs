using System.Runtime.InteropServices;

namespace Launcher.Win32;

public enum ShellProcessWindowStyle
{
    Normal,
    Minimized,
    Maximized,
    NoActivate,             // 非アクティブ
    MinimizedNoActivate,    // 最小化非アクティブ
    Hidden,
}

public class ShellProcessStartInfo
{
    public string? Arguments { get; set; }
    public string? FileName { get; set; }
    public string? Verb { get; set; }
    public string? WorkingDirectory { get; set; }
    public ShellProcessWindowStyle WindowStyle { get; set; } = ShellProcessWindowStyle.Normal;
    public bool CreateNoWindow { get; set; }
    public bool ErrorDialog { get; set; } = true;
    public IntPtr ErrorDialogParentHandle { get; set; }

    public ShellProcessStartInfo()
    {
    }
    public ShellProcessStartInfo(string fileName)
    {
        FileName = fileName;
    }
    public ShellProcessStartInfo(string fileName, string arguments)
    {
        FileName = fileName;
        Arguments = arguments;
    }
}

/// <summary>
/// .NETのShellExecuteEx()のラッパーはWindowStyleの辺りがビミョーなので自前用に実装。
/// 基本的に.NETのインターフェースに大体準拠したが、拡張機能はとりあえず省略。
/// </summary>
public static class ProcessLauncher
{
    public static void Start(ShellProcessStartInfo info)
    {
        IntPtr hProcess = InnerStart(info);
        CloseHandle(hProcess);
    }

    public static void Start(ShellProcessStartInfo info, System.Diagnostics.ProcessPriorityClass priority)
    {
        IntPtr hProcess = InnerStart(info);
        try
        {
            uint priorityValue = priority switch
            {
                System.Diagnostics.ProcessPriorityClass.RealTime => REALTIME_PRIORITY_CLASS,
                System.Diagnostics.ProcessPriorityClass.High => HIGH_PRIORITY_CLASS,
                System.Diagnostics.ProcessPriorityClass.AboveNormal => ABOVE_NORMAL_PRIORITY_CLASS,
                System.Diagnostics.ProcessPriorityClass.Normal => NORMAL_PRIORITY_CLASS,
                System.Diagnostics.ProcessPriorityClass.BelowNormal => BELOW_NORMAL_PRIORITY_CLASS,
                System.Diagnostics.ProcessPriorityClass.Idle => IDLE_PRIORITY_CLASS,
                _ => NORMAL_PRIORITY_CLASS,
            };
            SetPriorityClass(hProcess, priorityValue);
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
            ShellProcessWindowStyle.Normal => SW_SHOWNORMAL,
            ShellProcessWindowStyle.Minimized => SW_SHOWMINIMIZED,
            ShellProcessWindowStyle.Maximized => SW_SHOWMAXIMIZED,
            ShellProcessWindowStyle.NoActivate => SW_SHOWNOACTIVATE,
            ShellProcessWindowStyle.MinimizedNoActivate => SW_SHOWMINNOACTIVE,
            ShellProcessWindowStyle.Hidden => SW_HIDE,
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
            throw new System.ComponentModel.Win32Exception("ファイルの実行に失敗しました。");
        }
        return shinfo.hProcess;
    }

    #region ShellExecuteExとか

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

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO shinfo);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    static extern int GetProcessId(IntPtr Process);

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
