using System.ComponentModel;
using System.Runtime.InteropServices;
using Launcher.Infrastructure;

namespace Launcher.Win32;

public sealed class KeyHookEventArgs : EventArgs
{
    int nCode;
    IntPtr wParam;
    Hook.KBDLLHOOKSTRUCT s;
    bool handled;
    public KeyHookEventArgs(int nCode, IntPtr wParam, Hook.KBDLLHOOKSTRUCT s)
    {
        this.nCode = nCode;
        this.wParam = wParam;
        this.s = s;
    }
    public int HookCode
    {
        get { return nCode; }
    }
    public IntPtr WParam
    {
        get { return wParam; }
    }
    public bool Handled
    {
        get { return handled; }
        set { handled = value; }
    }
    public Hook.KBDLLHOOKSTRUCT HookStruct
    {
        get { return s; }
    }
}

public sealed class MouseHookEventArgs : EventArgs
{
    int nCode;
    IntPtr wParam;
    Hook.MSLLHOOKSTRUCT s;
    bool handled;
    public MouseHookEventArgs(int nCode, IntPtr wParam, Hook.MSLLHOOKSTRUCT s)
    {
        this.nCode = nCode;
        this.wParam = wParam;
        this.s = s;
    }
    public int HookCode
    {
        get { return nCode; }
    }
    public IntPtr WParam
    {
        get { return wParam; }
    }
    public bool Handled
    {
        get { return handled; }
        set { handled = value; }
    }
    public Hook.MSLLHOOKSTRUCT HookStruct
    {
        get { return s; }
    }
}

/// <summary>
/// マウスフック・キーボードフック。
/// </summary>
public static class Hook
{
    public const int HC_ACTION = 0;
    public const int HC_GETNEXT = 1;
    public const int HC_SKIP = 2;
    public const int HC_NOREMOVE = 3;
    public const int HC_SYSMODALON = 4;
    public const int HC_SYSMODALOFF = 5;

    public const int KF_EXTENDED = 0x0100;
    public const int KF_DLGMODE = 0x0800;
    public const int KF_MENUMODE = 0x1000;
    public const int KF_ALTDOWN = 0x2000;
    public const int KF_REPEAT = 0x4000;
    public const int KF_UP = 0x8000;

    public const int LLKHF_EXTENDED = (KF_EXTENDED >> 8);
    public const int LLKHF_INJECTED = 0x00000010;
    public const int LLKHF_ALTDOWN = (KF_ALTDOWN >> 8);
    public const int LLKHF_UP = (KF_UP >> 8);

    public const int LLMHF_INJECTED = 0x00000001;

    public static readonly IntPtr WM_LBUTTONDOWN = new IntPtr(0x0201);
    public static readonly IntPtr WM_LBUTTONUP = new IntPtr(0x0202);
    public static readonly IntPtr WM_RBUTTONDOWN = new IntPtr(0x0204);
    public static readonly IntPtr WM_RBUTTONUP = new IntPtr(0x0205);
    public static readonly IntPtr WM_KEYDOWN = new IntPtr(0x0100);
    public static readonly IntPtr WM_KEYUP = new IntPtr(0x0101);
    public static readonly IntPtr WM_SYSKEYDOWN = new IntPtr(0x0104);
    public static readonly IntPtr WM_SYSKEYUP = new IntPtr(0x0105);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo; // ULONG_PTR
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo; // ULONG_PTR
    }

    /// <summary>
    /// KeyHookなイベント
    /// </summary>
    public static event EventHandler<KeyHookEventArgs>? KeyHook;
    /// <summary>
    /// MouseHookなイベント
    /// </summary>
    public static event EventHandler<MouseHookEventArgs>? MouseHook;

    static LowLevelKeyboardProc? keyProc; // GC対策に持っておく必要がある
    static LowLevelMouseProc? mouseProc; // GC対策に持っておく必要がある
    static IntPtr keyHook = IntPtr.Zero;
    static IntPtr mouseHook = IntPtr.Zero;

    public static void SetKeyHook()
    {
        UnsetKeyHook();
        // フックプロシージャ内では例外を外に漏らすとシステム全体に影響するため、全例外をキャッチする
#pragma warning disable CA1031 // フックプロシージャは全例外をキャッチしてCallNextHookExを呼ぶ必要がある
        keyProc = new LowLevelKeyboardProc(delegate (int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            try
            {
                EventHandler<KeyHookEventArgs>? KeyHook = Hook.KeyHook;
                KeyHookEventArgs e = new KeyHookEventArgs(nCode, wParam, lParam);
                if (KeyHook is not null)
                {
                    KeyHook(null, e);
                }
                return e.Handled ?
                    (IntPtr)1 :
                    CallNextHookEx(keyHook, nCode, wParam, ref lParam);
            }
            catch (Exception ex)
            {
                // フックコールバック内は同期I/O禁止のため非同期で書き込む。
                // 詳細は.claude/rules/win32-interop.md「Win32フックコールバック」節を参照。
                _ = Task.Run(() => DiagnosticLog.Error("Hook.Key", ex));
                return CallNextHookEx(keyHook, nCode, wParam, ref lParam);
            }
        });
#pragma warning restore CA1031
        IntPtr hModule = GetModuleHandle(null);
        keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyProc, hModule, 0);
        if (keyHook == IntPtr.Zero)
        {
            throw new Win32Exception();
        }
        System.Diagnostics.Trace.WriteLine($"Hook.Register keyboard handle={keyHook} thread={Environment.CurrentManagedThreadId}");
        // 多重登録を防ぐため一度解除してから再登録
        AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;
        AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
    }

    public static void SetMouseHook()
    {
        UnsetMouseHook();
        // フックプロシージャ内では例外を外に漏らすとシステム全体に影響するため、全例外をキャッチする
#pragma warning disable CA1031 // フックプロシージャは全例外をキャッチしてCallNextHookExを呼ぶ必要がある
        mouseProc = new LowLevelMouseProc(delegate (int nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam)
        {
            try
            {
                EventHandler<MouseHookEventArgs>? MouseHook = Hook.MouseHook;
                MouseHookEventArgs e = new MouseHookEventArgs(nCode, wParam, lParam);
                if (MouseHook is not null)
                {
                    MouseHook(null, e);
                }
                return e.Handled ?
                    (IntPtr)1 :
                    CallNextHookEx(mouseHook, nCode, wParam, ref lParam);
            }
            catch (Exception ex)
            {
                // フックコールバック内は同期I/O禁止のため非同期で書き込む。
                // 詳細は.claude/rules/win32-interop.md「Win32フックコールバック」節を参照。
                _ = Task.Run(() => DiagnosticLog.Error("Hook.Mouse", ex));
                return CallNextHookEx(mouseHook, nCode, wParam, ref lParam);
            }
        });
#pragma warning restore CA1031
        IntPtr hModule = GetModuleHandle(null);
        mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, hModule, 0);
        if (mouseHook == IntPtr.Zero)
        {
            throw new Win32Exception();
        }
        System.Diagnostics.Trace.WriteLine($"Hook.Register mouse handle={mouseHook} thread={Environment.CurrentManagedThreadId}");
        // 多重登録を防ぐため一度解除してから再登録
        AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;
        AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
    }

    static void CurrentDomain_DomainUnload(object? sender, EventArgs e)
    {
        try { UnsetKeyHook(); } catch (Win32Exception ex) { System.Diagnostics.Debug.Fail(ex.ToString()); }
        try { UnsetMouseHook(); } catch (Win32Exception ex) { System.Diagnostics.Debug.Fail(ex.ToString()); }
    }

    public static void UnsetKeyHook()
    {
        UnsetHook(ref keyHook, "keyboard");
        keyProc = null;
    }

    public static void UnsetMouseHook()
    {
        UnsetHook(ref mouseHook, "mouse");
        mouseProc = null;
    }

    static void UnsetHook(ref IntPtr handle, string kind)
    {
        if (handle == IntPtr.Zero) return;
        if (!UnhookWindowsHookEx(handle))
        {
            int error = Marshal.GetLastWin32Error();
            // OSが低レベルフックを削除した後の1404は、既に解除済みの状態を表す。
            // その他の失敗ではハンドルとdelegateを保持し、呼出し側へ例外を返す。
            if (error != 1404) throw new Win32Exception(error);
            DiagnosticLog.Debug("Hook.Unregister", $"{kind} handle={handle} alreadyRemoved=true error={error}");
            System.Diagnostics.Trace.WriteLine($"Hook.Unregister {kind} handle={handle} alreadyRemoved=true error={error}");
        }
        else System.Diagnostics.Trace.WriteLine($"Hook.Unregister {kind} handle={handle} alreadyRemoved=false");
        handle = IntPtr.Zero;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr GetModuleHandle(string? lpModuleName);
    [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, int dwThreadId);
    [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);

    [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam);
    [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

    [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool UnhookWindowsHookEx(IntPtr hHook);

    const int WH_KEYBOARD_LL = 13;
    const int WH_MOUSE_LL = 14;
}
