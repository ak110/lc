using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// グローバルキーボード/マウスフックの管理。
/// ホットキー判定とボタンランチャー起動トリガーの状態管理を担当。
/// </summary>
sealed class HookManager
{
    readonly Func<Config> getConfig;
    readonly Func<IntPtr> getHandle;
    readonly Action<Action> beginInvoke;

    /// <summary>
    /// ホットキー1組の設定。一致時に表示指示 (<c>Message</c>) を送る。
    /// </summary>
    readonly struct HotkeyBinding
    {
        public Keys VKey { get; init; }
        public KeyTable.Modifiers Modifiers { get; init; }

        /// <summary>一致時に送る表示指示 (WM_APPMSG の lParam)</summary>
        public IntPtr Message { get; init; }
    }

    // ホットキー設定 (ランチャー用・メモパッド用などの複数組)
    List<HotkeyBinding> hotkeys = [];

    // マウスボタンの押下状態
    bool lbuttonDown;
    bool rbuttonDown;

    // トリガーボタンのUPイベント抑制用フラグ
    bool suppressNextLButtonUp;
    bool suppressNextRButtonUp;

    // ホットキーのKEYUPイベント抑制用
    int suppressKeyUpVK;

    /// <summary>
    /// 自プロセスが<see cref="keybd_event"/>で注入するキー入力を識別するマーカー値。
    /// <see cref="OnKeyHook"/>は<see cref="Hook.KBDLLHOOKSTRUCT.dwExtraInfo"/>と比較して
    /// 自注入だけを<see cref="UpdatePhysicalModifiers"/>から除外する。
    /// </summary>
    const nint HookManagerInjectionMarker = 0x4C43_0001;

    /// <summary>
    /// 物理修飾キー状態は、フック側判定用に注入と独立して追跡する。
    /// <see cref="InjectHotkeyModifierKeyUps"/>が呼ぶ<see cref="keybd_event"/>はOSの修飾キー論理状態を
    /// 「解放」に更新するため、<see cref="KeyTable.GetModifiers"/>や<see cref="KeyTable.GetModifiersAsync"/>由来の判定では
    /// Ctrl+Shift+Mなどのホットキーを修飾キー保持のまま連打した際に2回目以降が不発となる。
    /// 低レベルフックが受け取るイベントのうち自プロセスの注入（<see cref="HookManagerInjectionMarker"/>）だけを
    /// 除外して<see cref="physicalKeys"/>・<see cref="physicalModifiers"/>へ反映し、ホットキー判定はこの物理状態を用いる。
    /// 他プロセスの注入（AutoHotkey・KVM・RDP・IME等）はマーカーが立たないため物理入力と同等に扱う。
    /// </summary>
    [Flags]
    enum PhysicalModifierKey
    {
        None = 0,
        LShift = 1 << 0,
        RShift = 1 << 1,
        LCtrl = 1 << 2,
        RCtrl = 1 << 3,
        LAlt = 1 << 4,
        RAlt = 1 << 5,
        LWin = 1 << 6,
        RWin = 1 << 7,
    }

    PhysicalModifierKey physicalKeys;
    KeyTable.Modifiers physicalModifiers;

    static PhysicalModifierKey PhysicalKeyFor(int vkCode) => vkCode switch
    {
        0xA0 => PhysicalModifierKey.LShift,
        0xA1 => PhysicalModifierKey.RShift,
        0xA2 => PhysicalModifierKey.LCtrl,
        0xA3 => PhysicalModifierKey.RCtrl,
        0xA4 => PhysicalModifierKey.LAlt,
        0xA5 => PhysicalModifierKey.RAlt,
        0x5B => PhysicalModifierKey.LWin,
        0x5C => PhysicalModifierKey.RWin,
        _ => PhysicalModifierKey.None,
    };

    static KeyTable.Modifiers ToModifiers(PhysicalModifierKey keys)
    {
        KeyTable.Modifiers m = 0;
        if ((keys & (PhysicalModifierKey.LShift | PhysicalModifierKey.RShift)) != 0) m |= KeyTable.Modifiers.Shift;
        if ((keys & (PhysicalModifierKey.LCtrl | PhysicalModifierKey.RCtrl)) != 0) m |= KeyTable.Modifiers.Ctrl;
        if ((keys & (PhysicalModifierKey.LAlt | PhysicalModifierKey.RAlt)) != 0) m |= KeyTable.Modifiers.Alt;
        if ((keys & (PhysicalModifierKey.LWin | PhysicalModifierKey.RWin)) != 0) m |= KeyTable.Modifiers.Win;
        return m;
    }

    void UpdatePhysicalModifiers(KeyHookEventArgs e)
    {
        if ((nint)e.HookStruct.dwExtraInfo == HookManagerInjectionMarker) return;
        var key = PhysicalKeyFor(e.HookStruct.vkCode);
        if (key == PhysicalModifierKey.None) return;
        if (e.WParam == Hook.WM_KEYDOWN || e.WParam == Hook.WM_SYSKEYDOWN)
        {
            physicalKeys |= key;
        }
        else if (e.WParam == Hook.WM_KEYUP || e.WParam == Hook.WM_SYSKEYUP)
        {
            physicalKeys &= ~key;
        }
        physicalModifiers = ToModifiers(physicalKeys);
    }

    /// <param name="getConfig">現在のConfig取得デリゲート</param>
    /// <param name="getHandle">ウィンドウハンドル取得デリゲート</param>
    /// <param name="beginInvoke">UIスレッドへの非同期ディスパッチ</param>
    public HookManager(Func<Config> getConfig, Func<IntPtr> getHandle, Action<Action> beginInvoke)
    {
        this.getConfig = getConfig;
        this.getHandle = getHandle;
        this.beginInvoke = beginInvoke;
    }

    /// <summary>
    /// ホットキー設定を更新する。
    /// 各組はホットキー文字列と一致時に送る表示指示の対で渡す。
    /// 解析できない組 (未割り当て・不正文字列) は登録しない。
    /// </summary>
    public void UpdateHotkeys(params (string HotKey, IntPtr Message)[] bindings)
    {
        var list = new List<HotkeyBinding>();
        foreach (var (hotKey, message) in bindings)
        {
            var hk = KeyTable.GetKeyWithModifiers(hotKey);
            if (hk.Key is null) continue;
            var vkey = KeyTable.KeysToVKey(hk.Key.Value);
            if (vkey == Keys.None) continue;
            list.Add(new HotkeyBinding
            {
                VKey = vkey,
                Modifiers = hk.Modifiers,
                Message = message,
            });
        }
        hotkeys = list;
    }

    /// <summary>
    /// フックを登録する
    /// </summary>
    public void Register()
    {
        Hook.KeyHook += OnKeyHook;
        Hook.MouseHook += OnMouseHook;
        // 登録時点で押下中の修飾キーをL/R個別に取り込み、続く物理イベントで追従する
        physicalKeys = PhysicalModifierKey.None;
        if (GetAsyncKeyState(VK_LSHIFT) < 0) physicalKeys |= PhysicalModifierKey.LShift;
        if (GetAsyncKeyState(VK_RSHIFT) < 0) physicalKeys |= PhysicalModifierKey.RShift;
        if (GetAsyncKeyState(VK_LCONTROL) < 0) physicalKeys |= PhysicalModifierKey.LCtrl;
        if (GetAsyncKeyState(VK_RCONTROL) < 0) physicalKeys |= PhysicalModifierKey.RCtrl;
        if (GetAsyncKeyState(VK_LMENU) < 0) physicalKeys |= PhysicalModifierKey.LAlt;
        if (GetAsyncKeyState(VK_RMENU) < 0) physicalKeys |= PhysicalModifierKey.RAlt;
        if (GetAsyncKeyState(VK_LWIN) < 0) physicalKeys |= PhysicalModifierKey.LWin;
        if (GetAsyncKeyState(VK_RWIN) < 0) physicalKeys |= PhysicalModifierKey.RWin;
        physicalModifiers = ToModifiers(physicalKeys);
        Hook.SetKeyHook();
        Hook.SetMouseHook();
    }

    /// <summary>
    /// フックを解除する
    /// </summary>
    public void Unregister()
    {
        Hook.KeyHook -= OnKeyHook;
        Hook.MouseHook -= OnMouseHook;
        Hook.UnsetKeyHook();
        Hook.UnsetMouseHook();
        // 解除後に残留するフラグをリセットする
        lbuttonDown = false;
        rbuttonDown = false;
        suppressNextLButtonUp = false;
        suppressNextRButtonUp = false;
        suppressKeyUpVK = 0;
        physicalKeys = PhysicalModifierKey.None;
        physicalModifiers = 0;
    }

    void OnKeyHook(object? sender, KeyHookEventArgs e)
    {
        try
        {
            if (e.HookCode == Hook.HC_ACTION)
            {
                UpdatePhysicalModifiers(e);
                if (e.WParam == Hook.WM_KEYDOWN || e.WParam == Hook.WM_SYSKEYDOWN)
                {
                    var currentModifiers = physicalModifiers;
                    foreach (var hk in hotkeys)
                    {
                        if (e.HookStruct.vkCode != (int)hk.VKey || currentModifiers != hk.Modifiers)
                        {
                            continue;
                        }
                        e.Handled = true;
                        // キーリピート時は初回押下のみ処理する (多重発火防止)
                        if (suppressKeyUpVK == 0)
                        {
                            // 対応するKEYUPを1回だけ抑制
                            suppressKeyUpVK = e.HookStruct.vkCode;
                            // Alt修飾時はダミーキー入力を注入してAlt単独リリースによる
                            // システムメニュー表示を防止。KEYUP注入より前にF24を挿入することで
                            // 「Alt→F24→Alt解放」の順序を保証する
                            if ((hk.Modifiers & KeyTable.Modifiers.Alt) != 0)
                            {
                                BreakAltSequence();
                            }
                            // ホットキー修飾キーを前景アプリ向けに解放する
                            // (フォーカス移行前に注入することで前景アプリにKEYUPが届く)
                            InjectHotkeyModifierKeyUps(hk.Modifiers);
                            // フック内からSendMessageを呼ぶとWndProc→ActivateForce→DoEventsの連鎖で
                            // フックコールバックが再入するため、マウスフックと同様にPostMessageを使う
                            new WindowHelper(getHandle()).PostMessage(
                                Program.WM_APPMSG,
                                Program.WM_APPMSG_WPARAM,
                                hk.Message);
                        }
                        // 一致したら他の組は判定しない
                        break;
                    }
                }
                else if (e.WParam == Hook.WM_KEYUP || e.WParam == Hook.WM_SYSKEYUP)
                {
                    if (suppressKeyUpVK != 0 && e.HookStruct.vkCode == suppressKeyUpVK)
                    {
                        suppressKeyUpVK = 0;
                        e.Handled = true;
                    }
                }
            }
        }
        // フックコールバック内では例外を外に漏らすとフックチェーンが破綻するため、全例外をキャッチする
#pragma warning disable CA1031 // フックコールバック内の最終防御ライン
        catch (Exception ex)
        {
            // フックコールバック内は同期I/O禁止のため非同期で書き込む。
            // 詳細は.claude/rules/win32-interop.md「Win32フックコールバック」節を参照。
            _ = Task.Run(() => DiagnosticLog.Error("Hook.Key", ex));
            // MessageBoxもBeginInvokeで非同期表示（フックタイムアウト回避）
            beginInvoke(() => MessageBox.Show(
                $"キーボードフック処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
#pragma warning restore CA1031
    }

    void OnMouseHook(object? sender, MouseHookEventArgs e)
    {
        try
        {
            var config = getConfig();
            if (config.ButtonLauncherActivation == ButtonLauncherActivation.Disabled) return;
            if (e.HookCode != Hook.HC_ACTION) return;

            if (e.WParam == Hook.WM_LBUTTONDOWN)
            {
                lbuttonDown = true;
                // 右→左: 右ボタン押下中に左クリック
                if (config.ButtonLauncherActivation == ButtonLauncherActivation.RightThenLeft && rbuttonDown)
                {
                    // フック内から直接ShowLauncher()を呼ぶとSetForegroundWindowが拒否されるため、
                    // PostMessageで間接的に呼び出す
                    new WindowHelper(getHandle()).PostMessage(
                        Program.WM_APPMSG, Program.WM_APPMSG_WPARAM, Program.WM_APPMSG_SHOWBUTTONLAUNCHER);
                    e.Handled = true;
                    suppressNextLButtonUp = true;
                }
            }
            else if (e.WParam == Hook.WM_LBUTTONUP)
            {
                lbuttonDown = false;
                if (suppressNextLButtonUp)
                {
                    suppressNextLButtonUp = false;
                    e.Handled = true;
                }
            }
            else if (e.WParam == Hook.WM_RBUTTONDOWN)
            {
                rbuttonDown = true;
                // 左→右: 左ボタン押下中に右クリック
                if (config.ButtonLauncherActivation == ButtonLauncherActivation.LeftThenRight && lbuttonDown)
                {
                    new WindowHelper(getHandle()).PostMessage(
                        Program.WM_APPMSG, Program.WM_APPMSG_WPARAM, Program.WM_APPMSG_SHOWBUTTONLAUNCHER);
                    e.Handled = true;
                    suppressNextRButtonUp = true;
                }
            }
            else if (e.WParam == Hook.WM_RBUTTONUP)
            {
                rbuttonDown = false;
                if (suppressNextRButtonUp)
                {
                    suppressNextRButtonUp = false;
                    e.Handled = true;
                }
            }
        }
        // フックコールバック内では例外を外に漏らすとフックチェーンが破綻するため、全例外をキャッチする
#pragma warning disable CA1031 // フックコールバック内の最終防御ライン
        catch (Exception ex)
        {
            // フックコールバック内は同期I/O禁止のため非同期で書き込む。
            // 詳細は.claude/rules/win32-interop.md「Win32フックコールバック」節を参照。
            _ = Task.Run(() => DiagnosticLog.Error("Hook.Mouse", ex));
            beginInvoke(() => MessageBox.Show(
                $"マウスフック処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
#pragma warning restore CA1031
    }

    const uint KEYEVENTF_KEYUP = 0x0002;
    const byte VK_LSHIFT = 0xA0;
    const byte VK_RSHIFT = 0xA1;
    const byte VK_LCONTROL = 0xA2;
    const byte VK_RCONTROL = 0xA3;
    const byte VK_LMENU = 0xA4;
    const byte VK_RMENU = 0xA5;
    const byte VK_LWIN = 0x5B;
    const byte VK_RWIN = 0x5C;
    const byte VK_F24 = 0x87;

    /// <summary>
    /// ホットキーを構成する修飾キーのうち現在押下中のものに対してKEYUPを注入する。
    /// ランチャーのフォーカス取得前に前景アプリへ修飾キー解放を通知するために使う。
    /// 修飾キーKEYUPを注入することでOSの論理状態が「解放」となり、フック側で
    /// <see cref="physicalModifiers"/>を別途追跡していないと、続く同ホットキー押下の
    /// 判定でCtrl/Shift等が0扱いとなり不発する。本関数の注入イベントは
    /// <see cref="keybd_event"/>第4引数へ<see cref="HookManagerInjectionMarker"/>を埋め込み、
    /// フック側の<see cref="UpdatePhysicalModifiers"/>が物理状態から除外する。
    /// </summary>
    static void InjectHotkeyModifierKeyUps(KeyTable.Modifiers modifiers)
    {
        var marker = (IntPtr)HookManagerInjectionMarker;
        if ((modifiers & KeyTable.Modifiers.Ctrl) != 0)
        {
            if (GetAsyncKeyState(VK_LCONTROL) < 0) keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, marker);
            if (GetAsyncKeyState(VK_RCONTROL) < 0) keybd_event(VK_RCONTROL, 0, KEYEVENTF_KEYUP, marker);
        }
        if ((modifiers & KeyTable.Modifiers.Alt) != 0)
        {
            if (GetAsyncKeyState(VK_LMENU) < 0) keybd_event(VK_LMENU, 0, KEYEVENTF_KEYUP, marker);
            if (GetAsyncKeyState(VK_RMENU) < 0) keybd_event(VK_RMENU, 0, KEYEVENTF_KEYUP, marker);
        }
        if ((modifiers & KeyTable.Modifiers.Shift) != 0)
        {
            if (GetAsyncKeyState(VK_LSHIFT) < 0) keybd_event(VK_LSHIFT, 0, KEYEVENTF_KEYUP, marker);
            if (GetAsyncKeyState(VK_RSHIFT) < 0) keybd_event(VK_RSHIFT, 0, KEYEVENTF_KEYUP, marker);
        }
        if ((modifiers & KeyTable.Modifiers.Win) != 0)
        {
            if (GetAsyncKeyState(VK_LWIN) < 0) keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, marker);
            if (GetAsyncKeyState(VK_RWIN) < 0) keybd_event(VK_RWIN, 0, KEYEVENTF_KEYUP, marker);
        }
    }

    /// <summary>
    /// ダミーキーイベントを注入してWindowsのAltメニュー起動シーケンスを中断する。
    /// Alt KEYUPを抑制するとAltが押しっぱなしになるため、代わりにこの手法を使う。
    /// </summary>
    static void BreakAltSequence()
    {
        // 未使用キー(VK_F24)のdown+upを注入し、Alt単独リリースと認識されることを防止
        var marker = (IntPtr)HookManagerInjectionMarker;
        keybd_event(VK_F24, 0, 0, marker);
        keybd_event(VK_F24, 0, KEYEVENTF_KEYUP, marker);
    }

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);
}
