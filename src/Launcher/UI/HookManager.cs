using System.Diagnostics;
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

    // ホットキー設定
    Keys hotkeyVK;
    KeyTable.Modifiers modifiers;

    // マウスボタンの押下状態
    bool lbuttonDown;
    bool rbuttonDown;

    // トリガーボタンのUPイベント抑制用フラグ
    bool suppressNextLButtonUp;
    bool suppressNextRButtonUp;

    // ホットキーのKEYUPイベント抑制用
    int suppressKeyUpVK;

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
    /// ホットキー設定を更新する
    /// </summary>
    public void UpdateHotkey(string hotKeyString)
    {
        var hk = KeyTable.GetKeyWithModifiers(hotKeyString);
        hotkeyVK = KeyTable.KeysToVKey(hk.First);
        modifiers = hk.Second;
    }

    /// <summary>
    /// フックを登録する
    /// </summary>
    public void Register()
    {
        Hook.KeyHook += OnKeyHook;
        Hook.MouseHook += OnMouseHook;
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
    }

    void OnKeyHook(object? sender, KeyHookEventArgs e)
    {
        try
        {
            if (e.HookCode == Hook.HC_ACTION)
            {
                if (e.WParam == Hook.WM_KEYDOWN || e.WParam == Hook.WM_SYSKEYDOWN)
                {
                    if (e.HookStruct.vkCode == (int)hotkeyVK &&
                        KeyTable.GetModifiers() == modifiers)
                    {
                        WindowHelper window = new WindowHelper(getHandle());
                        window.SendMessage(WM.WM_APP,
                            Program.WM_APPMSG_WPARAM,
                            Program.WM_APPMSG_SHOWHIDE);
                        e.Handled = true;
                        // 対応するKEYUPを1回だけ抑制
                        suppressKeyUpVK = e.HookStruct.vkCode;
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
        catch (Exception ex)
        {
            Debug.WriteLine($"OnKeyHookで例外: {ex}");
            // フックコールバック内ではMessageBoxを直接表示するとフックがタイムアウトするため、
            // BeginInvokeで非同期表示
            beginInvoke(() => MessageBox.Show(
                $"キーボードフック処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
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
        catch (Exception ex)
        {
            Debug.WriteLine($"OnMouseHookで例外: {ex}");
            beginInvoke(() => MessageBox.Show(
                $"マウスフック処理中にエラーが発生しました:\n{ex.Message}\n\n{ex.StackTrace}",
                "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }
}
