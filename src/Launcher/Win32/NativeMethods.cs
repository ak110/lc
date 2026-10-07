using System.Runtime.InteropServices;
using System.Text;

namespace Launcher.Win32;

/// <summary>
/// 共通のP/Invoke宣言
/// </summary>
internal static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHParseDisplayName(
        string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    public static void SendMessageRect(IntPtr hWnd, int message, IntPtr wParam, ref RECT rect)
    {
        IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf<RECT>());
        try
        {
            Marshal.StructureToPtr(rect, buffer, false);
            SendMessage(hWnd, message, wParam, buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// 現在のユーザーがAdminかどうか
    /// </summary>
    public static bool IsUserAnAdmin()
    {
        try
        {
            return IsUserAnAdminNative();
        }
        catch (EntryPointNotFoundException)
        {
        }
        return false;
    }

    [DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsUserAnAdminNative();

    /// <summary>
    /// 親プロセスのコンソールにアタッチする (WinExeアプリからの標準出力に必要)
    /// </summary>
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachConsole(int dwProcessId);

    public const int ATTACH_PARENT_PROCESS = -1;

    /// <summary>
    /// PATH環境変数に沿ってファイルを検索する
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SearchPath(
        string? lpPath,
        string lpFileName,
        string? lpExtension,
        int nBufferLength,
        StringBuilder lpBuffer,
        out IntPtr lpFilePart);
}
