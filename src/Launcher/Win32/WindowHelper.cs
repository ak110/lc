using System.Runtime.InteropServices;
using Launcher.Infrastructure;

namespace Launcher.Win32;

/// <summary>
/// WM_*定数一覧。
/// </summary>
public static class WM
{
    public const int WM_CLOSE = 0x0010;
    public const int WM_WININICHANGE = 0x001A;
    public const int WM_SETTINGCHANGE = WM_WININICHANGE;
    public const int WM_COPY = 0x0301;
    public const int WM_PASTE = 0x0302;
    public const int WM_APP = 0x8000;
    public const int WM_USER = 0x0400;

    /// <summary>リッチエディットの取り消し上限を設定するメッセージ (EM_SETUNDOLIMIT = WM_USER + 82)。</summary>
    public const int EM_SETUNDOLIMIT = WM_USER + 82;

    /// <summary>リッチエディットの言語オプションを設定するメッセージ (EM_SETLANGOPTIONS = WM_USER + 120)。</summary>
    public const int EM_SETLANGOPTIONS = WM_USER + 120;

    /// <summary>リッチエディットの言語オプションを取得するメッセージ (EM_GETLANGOPTIONS = WM_USER + 121)。</summary>
    public const int EM_GETLANGOPTIONS = WM_USER + 121;
}

/// <summary>
/// HWNDのラッパー + Win32 APIを使うウィンドウ操作のstaticヘルパー。
/// </summary>
public sealed class WindowHelper
{
    #region 閉じるボタンの無効化/有効化

    /// <summary>
    /// フォームの閉じるボタンを無効にする
    /// </summary>
    public static void DisableCloseButton(Control form)
    {
        form.Resize += new EventHandler(FormResize);
        EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
    }

    /// <summary>
    /// フォームの閉じるボタンを有効に戻す
    /// </summary>
    public static void EnableCloseButton(Control form)
    {
        form.Resize -= new EventHandler(FormResize);
        EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_ENABLED);
    }

    // 最大化などでシステムメニューがリセットされるため、Resizeで再設定
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

    /// <summary>
    /// 強制的にアクティブにする
    /// </summary>
    public static void ActivateForce(Form form)
    {
        using var ati = new AttachThreadInput();
        // AttachThreadInput 後にキャプチャを解放する。
        // アクティブ化時に前景スレッドがマウスキャプチャを保持していると、
        // SetForegroundWindow によって WM_CAPTURECHANGED のみ届き WM_LBUTTONUP が届かず、
        // 他アプリのボタンが押しっぱなしになるため。
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

        Application.DoEvents();
        form.Visible = true;
        bool topMost = form.TopMost;
        form.TopMost = true;
        form.BringToFront();
        Application.DoEvents();
        form.Focus();
        form.Activate();
        Application.DoEvents();
        // アクティブ化のために一時的に TopMost=true にしたので元の値に戻す
        form.TopMost = topMost;

        if (time != 0)
        {
            if (!SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref time, 0))
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
            $"SystemParametersInfo({operation}) failed: {ex.GetType().Name}: {ex.Message}");
    }

    /// <summary>
    /// 現在の前景ウィンドウのハンドルを返す。
    /// 通知表示前の前景を記録し、通知を閉じたときに復元する用途で使う。
    /// </summary>
    public static IntPtr GetForegroundWindowHandle() => GetForegroundWindow();

    /// <summary>
    /// 指定ハンドルを前景ウィンドウに戻す。
    /// 呼び出し元プロセスがまだ foreground 権を持っているタイミングで呼ぶこと。
    /// 無効なハンドル、および自プロセス所有のウィンドウに対しては何もしない。
    /// </summary>
    public static void RestoreForegroundWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;
        if (!IsWindow(hWnd)) return;

        // 自プロセス所有のウィンドウには復元しない。
        // launcher 内のフォームへのフォーカス切替は WinForms の標準挙動に任せる。
        _ = GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == (uint)Environment.ProcessId) return;

        _ = SetForegroundWindow(hWnd);
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
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    const int SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
    const int SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;

    #endregion

    IntPtr hwnd;

    public WindowHelper(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    /// <summary>
    /// PostMessage を呼び出す。
    /// </summary>
    /// <exception cref="NullReferenceException">hwnd が IntPtr.Zero の場合の例外</exception>
    public bool PostMessage(int Msg, IntPtr wParam, IntPtr lParam)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new NullReferenceException("送信先ウィンドウが存在しない");
        }
        return PostMessage(hwnd, Msg, wParam, lParam);
    }

    /// <summary>
    /// SendMessage を呼び出す。
    /// </summary>
    /// <exception cref="NullReferenceException">hwnd が IntPtr.Zero の場合の例外</exception>
    public int SendMessage(int Msg, IntPtr wParam, IntPtr lParam)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new NullReferenceException("送信先ウィンドウが存在しない");
        }
        return SendMessage(hwnd, Msg, wParam, lParam);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
}
