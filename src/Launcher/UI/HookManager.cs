using System.ComponentModel;
using System.Runtime.ExceptionServices;
using Launcher.Core;
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

    readonly HookInputState inputState = new();

    // 自注入だけを物理修飾キー状態の追跡から除外する。
    const nint HookManagerInjectionMarker = 0x4C43_0001;

    static PhysicalModifierKey PhysicalKeyFor(int keyCode) => keyCode switch
    {
        0xA0 => PhysicalModifierKey.LeftShift,
        0xA1 => PhysicalModifierKey.RightShift,
        0xA2 => PhysicalModifierKey.LeftControl,
        0xA3 => PhysicalModifierKey.RightControl,
        0xA4 => PhysicalModifierKey.LeftAlt,
        0xA5 => PhysicalModifierKey.RightAlt,
        0x5B => PhysicalModifierKey.LeftWindows,
        0x5C => PhysicalModifierKey.RightWindows,
        _ => PhysicalModifierKey.None,
    };

    static InputModifiers ToModifiers(KeyTable.Modifiers value)
    {
        InputModifiers result = 0;
        if (value.HasFlag(KeyTable.Modifiers.Shift)) result |= InputModifiers.Shift;
        if (value.HasFlag(KeyTable.Modifiers.Ctrl)) result |= InputModifiers.Control;
        if (value.HasFlag(KeyTable.Modifiers.Alt)) result |= InputModifiers.Alt;
        if (value.HasFlag(KeyTable.Modifiers.Win)) result |= InputModifiers.Windows;
        return result;
    }

    void UpdatePhysicalModifiers(KeyHookEventArgs e)
    {
        bool down = e.WParam == Hook.WM_KEYDOWN || e.WParam == Hook.WM_SYSKEYDOWN;
        bool up = e.WParam == Hook.WM_KEYUP || e.WParam == Hook.WM_SYSKEYUP;
        if (down || up)
            inputState.UpdateModifier(PhysicalKeyFor(e.HookStruct.vkCode), down,
                (nint)e.HookStruct.dwExtraInfo == HookManagerInjectionMarker);
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
        inputState.Reset();
        int[] modifierKeys = [VK_LSHIFT, VK_RSHIFT, VK_LCONTROL, VK_RCONTROL, VK_LMENU, VK_RMENU, VK_LWIN, VK_RWIN];
        foreach (int key in modifierKeys)
            inputState.UpdateModifier(PhysicalKeyFor(key), NativeMethods.GetAsyncKeyState(key) < 0, false);
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
        Win32Exception? keyFailure = null;
        try { Hook.UnsetKeyHook(); }
        catch (Win32Exception ex) { keyFailure = ex; }
        try
        {
            try { Hook.UnsetMouseHook(); }
            catch (Win32Exception ex) when (keyFailure is not null)
            {
                throw new AggregateException("キーボードとマウスのフックを解除できませんでした。", keyFailure, ex);
            }
            if (keyFailure is not null) ExceptionDispatchInfo.Capture(keyFailure).Throw();
        }
        finally
        {
            // 一方の解除に失敗しても、残る解除と入力状態の回収を行う。
            inputState.Reset();
        }
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
                    foreach (var hk in hotkeys)
                    {
                        if (!inputState.MatchesHotkey(e.HookStruct.vkCode, (int)hk.VKey, ToModifiers(hk.Modifiers)))
                        {
                            continue;
                        }
                        e.Handled = true;
                        // キーリピート時は初回押下のみ処理する (多重発火防止)
                        if (inputState.TryBeginHotkey(e.HookStruct.vkCode))
                        {
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
                            ResidentMessages.Post(getHandle(), hk.Message);
                        }
                        // 一致したら他の組は判定しない
                        break;
                    }
                }
                else if (e.WParam == Hook.WM_KEYUP || e.WParam == Hook.WM_SYSKEYUP)
                {
                    if (inputState.ConsumeKeyUp(e.HookStruct.vkCode))
                    {
                        e.Handled = true;
                    }
                }
            }
        }
        // フックコールバック内では例外を外に漏らすとフックチェーンが破綻するため、全例外をキャッチする
#pragma warning disable CA1031 // フックコールバック内の最終防御ライン
        catch (Exception ex)
        {
            // ログと表示をともにUIへ配送し、フック内では同期I/Oを行わない。
            beginInvoke(() => ErrorReporter.Instance.OnException(ex));
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

            bool left = e.WParam == Hook.WM_LBUTTONDOWN || e.WParam == Hook.WM_LBUTTONUP;
            bool right = e.WParam == Hook.WM_RBUTTONDOWN || e.WParam == Hook.WM_RBUTTONUP;
            if (!left && !right) return;
            bool down = e.WParam == Hook.WM_LBUTTONDOWN || e.WParam == Hook.WM_RBUTTONDOWN;
            var result = inputState.ProcessMouse(left, down, config.ButtonLauncherActivation);
            e.Handled = result.Handled;
            if (result.Activate)
                ResidentMessages.Post(getHandle(), ResidentMessages.WM_APPMSG_SHOWBUTTONLAUNCHER);

        }
        // フックコールバック内では例外を外に漏らすとフックチェーンが破綻するため、全例外をキャッチする
#pragma warning disable CA1031 // フックコールバック内の最終防御ライン
        catch (Exception ex)
        {
            // ログと表示をともにUIへ配送し、フック内では同期I/Oを行わない。
            beginInvoke(() => ErrorReporter.Instance.OnException(ex));
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
    /// <see cref="HookInputState.Modifiers"/>を別途追跡していないと、続く同ホットキー押下の
    /// 判定でCtrl/Shift等が0扱いとなり不発する。本関数の注入イベントは
    /// <see cref="Launcher.Win32.NativeMethods.keybd_event(byte, byte, uint, IntPtr)"/>第4引数へ<see cref="HookManagerInjectionMarker"/>を埋め込み、
    /// フック側の<see cref="UpdatePhysicalModifiers"/>が物理状態から除外する。
    /// </summary>
    static void InjectHotkeyModifierKeyUps(KeyTable.Modifiers modifiers)
    {
        var marker = (IntPtr)HookManagerInjectionMarker;
        if ((modifiers & KeyTable.Modifiers.Ctrl) != 0)
        {
            if (NativeMethods.GetAsyncKeyState(VK_LCONTROL) < 0) NativeMethods.keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, marker);
            if (NativeMethods.GetAsyncKeyState(VK_RCONTROL) < 0) NativeMethods.keybd_event(VK_RCONTROL, 0, KEYEVENTF_KEYUP, marker);
        }
        if ((modifiers & KeyTable.Modifiers.Alt) != 0)
        {
            if (NativeMethods.GetAsyncKeyState(VK_LMENU) < 0) NativeMethods.keybd_event(VK_LMENU, 0, KEYEVENTF_KEYUP, marker);
            if (NativeMethods.GetAsyncKeyState(VK_RMENU) < 0) NativeMethods.keybd_event(VK_RMENU, 0, KEYEVENTF_KEYUP, marker);
        }
        if ((modifiers & KeyTable.Modifiers.Shift) != 0)
        {
            if (NativeMethods.GetAsyncKeyState(VK_LSHIFT) < 0) NativeMethods.keybd_event(VK_LSHIFT, 0, KEYEVENTF_KEYUP, marker);
            if (NativeMethods.GetAsyncKeyState(VK_RSHIFT) < 0) NativeMethods.keybd_event(VK_RSHIFT, 0, KEYEVENTF_KEYUP, marker);
        }
        if ((modifiers & KeyTable.Modifiers.Win) != 0)
        {
            if (NativeMethods.GetAsyncKeyState(VK_LWIN) < 0) NativeMethods.keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, marker);
            if (NativeMethods.GetAsyncKeyState(VK_RWIN) < 0) NativeMethods.keybd_event(VK_RWIN, 0, KEYEVENTF_KEYUP, marker);
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
        NativeMethods.keybd_event(VK_F24, 0, 0, marker);
        NativeMethods.keybd_event(VK_F24, 0, KEYEVENTF_KEYUP, marker);
    }

}
