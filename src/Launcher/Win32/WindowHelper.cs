using System.Runtime.InteropServices;
using Launcher.Infrastructure;

namespace Launcher.Win32;

public static class WM
{
    public const int WM_CLOSE = 0x0010;
    public const int WM_WININICHANGE = 0x001A;
    public const int WM_SETTINGCHANGE = WM_WININICHANGE;
    public const int WM_COPY = 0x0301;
    public const int WM_PASTE = 0x0302;
    public const int WM_APP = 0x8000;
    public const int WM_USER = 0x0400;

    public const int EM_SETUNDOLIMIT = WM_USER + 82;

    public const int EM_SETLANGOPTIONS = WM_USER + 120;

    public const int EM_GETLANGOPTIONS = WM_USER + 121;
}

public sealed class WindowHelper
{
    #region 閉じるボタンの無効化/有効化

    public static void DisableCloseButton(Control form)
    {
        form.Resize += new EventHandler(FormResize);
        EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
    }

    public static void EnableCloseButton(Control form)
    {
        form.Resize -= new EventHandler(FormResize);
        EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_ENABLED);
    }

    static void FormResize(object? sender, EventArgs e)
    {
        Control? form = sender as Control;
        if (form is not null)
            EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

    const uint SC_CLOSE = 0x0000F060u;
    const uint MF_BYCOMMAND = 0x00000000u;
    const uint MF_ENABLED = 0x00000000u;
    const uint MF_GRAYED = 0x00000001u;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

    #endregion

    #region 強制アクティブ化

    static int activationInProgress;

    public static void ActivateForce(Form form)
    {
        if (Interlocked.CompareExchange(ref activationInProgress, 1, 0) != 0) return;
        try
        {
            ActivateCore(form);
        }
        finally
        {
            Volatile.Write(ref activationInProgress, 0);
        }
    }

    private static void ActivateCore(Form form)
    {
        using var ati = new AttachThreadInput();
        ReleaseCapture();
        int time = 0;
        if (!SystemParametersInfo(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref time, 0))
        {
            LogSystemParametersInfoFailure("GET");
        }
        if (!SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, 0))
        {
            LogSystemParametersInfoFailure("SET");
        }

        bool topMost = form.TopMost;
        try
        {
            form.Visible = true;
            form.TopMost = true;
            form.BringToFront();
            form.Focus();
            form.Activate();
        }
        finally
        {
            if (!form.IsDisposed) form.TopMost = topMost;
            if (time != 0 && !SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref time, 0))
            {
                LogSystemParametersInfoFailure("SET");
            }
        }
    }

    static void LogSystemParametersInfoFailure(string operation)
    {
        var ex = new System.ComponentModel.Win32Exception();
        DiagnosticLog.Warn(
            "Window.SetForegroundLockTimeout",
            $"SystemParametersInfo({operation}) failed: {ex.GetType().Name}: {ex.Message}",
            "対応不要（ウィンドウの前面化を継続した）");
    }

    public static IntPtr GetForegroundWindowHandle() => NativeMethods.GetForegroundWindow();

    public static void RestoreForegroundWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;
        if (!IsWindow(hWnd)) return;

        _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == (uint)Environment.ProcessId) return;

        _ = NativeMethods.SetForegroundWindow(hWnd);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ReleaseCapture();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(int uiAction, int uiParam, ref int pvParam, int fWinIni);



    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindow(IntPtr hWnd);


    const int SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
    const int SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;

    #endregion

    IntPtr hwnd;

    public WindowHelper(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    public bool PostMessage(int Msg, IntPtr wParam, IntPtr lParam)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new NullReferenceException("送信先ウィンドウが存在しない");
        }
        return PostMessage(hwnd, Msg, wParam, lParam);
    }

    public int SendMessage(int Msg, IntPtr wParam, IntPtr lParam)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new NullReferenceException("送信先ウィンドウが存在しない");
        }
        return unchecked((int)NativeMethods.SendMessage(hwnd, Msg, wParam, lParam));
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

}
