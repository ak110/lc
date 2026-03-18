using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Toolkit.Windows {
    public class KeyHookEventArgs : EventArgs {
        int nCode;
        IntPtr wParam;
        Hook.KBDLLHOOKSTRUCT s;
        bool handled = false;
        public KeyHookEventArgs(int nCode, IntPtr wParam, Hook.KBDLLHOOKSTRUCT s) {
            this.nCode = nCode;
            this.wParam = wParam;
            this.s = s;
        }
        public int HookCode {
            get { return nCode; }
        }
        public IntPtr WParam {
            get { return wParam; }
        }
        public bool Handled {
            get { return handled; }
            set { handled = value; }
        }
        public Hook.KBDLLHOOKSTRUCT HookStruct {
            get { return s; }
        }
    }

    public class MouseHookEventArgs : EventArgs {
        int nCode;
        IntPtr wParam;
        Hook.MSLLHOOKSTRUCT s;
        bool handled = false;
        public MouseHookEventArgs(int nCode, IntPtr wParam, Hook.MSLLHOOKSTRUCT s) {
            this.nCode = nCode;
            this.wParam = wParam;
            this.s = s;
        }
        public int HookCode {
            get { return nCode; }
        }
        public IntPtr WParam {
            get { return wParam; }
        }
        public bool Handled {
            get { return handled; }
            set { handled = value; }
        }
        public Hook.MSLLHOOKSTRUCT HookStruct {
            get { return s; }
        }
    }

    /// <summary>
    /// マウスフック・キーボードフック。
    /// </summary>
    public static class Hook {
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
        public static readonly IntPtr WM_MOUSEMOVE = new IntPtr(0x0200);
        public static readonly IntPtr WM_MOUSEWHEEL = new IntPtr(0x020a);
        public static readonly IntPtr WM_RBUTTONDOWN = new IntPtr(0x0204);
        public static readonly IntPtr WM_RBUTTONUP = new IntPtr(0x0205);
        public static readonly IntPtr WM_KEYDOWN = new IntPtr(0x0100);
        public static readonly IntPtr WM_KEYUP = new IntPtr(0x0101);
        public static readonly IntPtr WM_CHAR = new IntPtr(0x0102);
        public static readonly IntPtr WM_DEADCHAR = new IntPtr(0x0103);
        public static readonly IntPtr WM_SYSKEYDOWN = new IntPtr(0x0104);
        public static readonly IntPtr WM_SYSKEYUP = new IntPtr(0x0105);
        public static readonly IntPtr WM_SYSCHAR = new IntPtr(0x0106);
        public static readonly IntPtr WM_SYSDEADCHAR = new IntPtr(0x0107);
        public static readonly IntPtr WM_UNICHAR = new IntPtr(0x0109);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo; // ULONG_PTR
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo; // ULONG_PTR
        }

        /// <summary>
        /// KeyHookなイベント
        /// </summary>
        public static event EventHandler<KeyHookEventArgs> KeyHook;
        /// <summary>
        /// MouseHookなイベント
        /// </summary>
        public static event EventHandler<MouseHookEventArgs> MouseHook;

        static LowLevelKeyboardProc keyProc; // GC対策に持っておく必要がある
        static LowLevelMouseProc mouseProc; // GC対策に持っておく必要がある
        static IntPtr keyHook = IntPtr.Zero;
        static IntPtr mouseHook = IntPtr.Zero;

        public static void SetKeyHook() {
            UnsetKeyHook();
            keyProc = new LowLevelKeyboardProc(delegate(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam) {
                try {
                    EventHandler<KeyHookEventArgs> KeyHook = Hook.KeyHook;
                    KeyHookEventArgs e = new KeyHookEventArgs(nCode, wParam, lParam);
                    if (KeyHook != null) {
                        KeyHook(null, e);
                    }
                    return e.Handled ?
                        (IntPtr)1 :
                        CallNextHookEx(keyHook, nCode, wParam, ref lParam);
                } catch {
                    return CallNextHookEx(keyHook, nCode, wParam, ref lParam);
                }
            });
            IntPtr hModule = GetModuleHandle(null);
            keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyProc, hModule, 0);
            if (keyHook == IntPtr.Zero) {
                throw new Win32Exception();
            }
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
        }

        public static void SetMouseHook() {
            UnsetMouseHook();
            mouseProc = new LowLevelMouseProc(delegate(int nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam) {
                try {
                    EventHandler<MouseHookEventArgs> MouseHook = Hook.MouseHook;
                    MouseHookEventArgs e = new MouseHookEventArgs(nCode, wParam, lParam);
                    if (MouseHook != null) {
                        MouseHook(null, e);
                    }
                    return e.Handled ?
                        (IntPtr)1 :
                        CallNextHookEx(mouseHook, nCode, wParam, ref lParam);
                } catch {
                    return CallNextHookEx(mouseHook, nCode, wParam, ref lParam);
                }
            });
            IntPtr hModule = GetModuleHandle(null);
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, hModule, 0);
            if (mouseHook == IntPtr.Zero) {
                throw new Win32Exception();
            }
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
        }

        static void CurrentDomain_DomainUnload(object sender, EventArgs e) {
            try { UnsetKeyHook(); } catch (Win32Exception ex) { System.Diagnostics.Debug.Fail(ex.ToString()); }
            try { UnsetMouseHook(); } catch (Win32Exception ex) { System.Diagnostics.Debug.Fail(ex.ToString()); }
        }

        public static void UnsetKeyHook() {
            if (keyHook != IntPtr.Zero) {
                if (!UnhookWindowsHookEx(keyHook)) {
                    throw new Win32Exception();
                }
            }
        }

        public static void UnsetMouseHook() {
            if (mouseHook != IntPtr.Zero) {
                if (!UnhookWindowsHookEx(mouseHook)) {
                    throw new Win32Exception();
                }
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, ref MSLLHOOKSTRUCT lParam);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
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
}

