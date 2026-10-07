namespace Launcher.Core;

/// <summary>左右を区別して追跡する修飾キー。</summary>
[Flags]
public enum PhysicalModifierKey
{
    None = 0,
    LeftShift = 1,
    RightShift = 2,
    LeftControl = 4,
    RightControl = 8,
    LeftAlt = 16,
    RightAlt = 32,
    LeftWindows = 64,
    RightWindows = 128,
}

/// <summary>フック入力の状態遷移。OS照会とイベント配送は呼び出し側が担う。</summary>
public sealed class HookInputState
{
    PhysicalModifierKey physicalKeys;
    int suppressedKeyUp;
    bool leftDown;
    bool rightDown;
    bool suppressLeftUp;
    bool suppressRightUp;

    public InputModifiers Modifiers
    {
        get
        {
            var value = InputModifiers.None;
            if ((physicalKeys & (PhysicalModifierKey.LeftShift | PhysicalModifierKey.RightShift)) != 0)
                value |= InputModifiers.Shift;
            if ((physicalKeys & (PhysicalModifierKey.LeftControl | PhysicalModifierKey.RightControl)) != 0)
                value |= InputModifiers.Control;
            if ((physicalKeys & (PhysicalModifierKey.LeftAlt | PhysicalModifierKey.RightAlt)) != 0)
                value |= InputModifiers.Alt;
            if ((physicalKeys & (PhysicalModifierKey.LeftWindows | PhysicalModifierKey.RightWindows)) != 0)
                value |= InputModifiers.Windows;
            return value;
        }
    }

    public void UpdateModifier(PhysicalModifierKey key, bool down, bool selfInjected)
    {
        if (selfInjected) return;
        if (down) physicalKeys |= key;
        else physicalKeys &= ~key;
    }

    public bool MatchesHotkey(int keyCode, int configuredKeyCode, InputModifiers configuredModifiers) =>
        keyCode == configuredKeyCode && Modifiers == configuredModifiers;

    /// <summary>一致したホットキーの初回押下だけを起動対象にする。</summary>
    public bool TryBeginHotkey(int keyCode)
    {
        if (suppressedKeyUp != 0) return false;
        suppressedKeyUp = keyCode;
        return true;
    }

    public bool ConsumeKeyUp(int keyCode)
    {
        if (suppressedKeyUp == 0 || suppressedKeyUp != keyCode) return false;
        suppressedKeyUp = 0;
        return true;
    }

    /// <summary>左右の押下順序から、イベント抑止とランチャー表示を判定する。</summary>
    public (bool Handled, bool Activate) ProcessMouse(bool left, bool down, ButtonLauncherActivation activation)
    {
        if (activation == ButtonLauncherActivation.Disabled) return (false, false);
        if (left)
        {
            leftDown = down;
            if (down && rightDown && activation == ButtonLauncherActivation.RightThenLeft)
            {
                suppressLeftUp = true;
                return (true, true);
            }
            if (!down && suppressLeftUp)
            {
                suppressLeftUp = false;
                return (true, false);
            }
        }
        else
        {
            rightDown = down;
            if (down && leftDown && activation == ButtonLauncherActivation.LeftThenRight)
            {
                suppressRightUp = true;
                return (true, true);
            }
            if (!down && suppressRightUp)
            {
                suppressRightUp = false;
                return (true, false);
            }
        }
        return (false, false);
    }

    public void Reset()
    {
        physicalKeys = PhysicalModifierKey.None;
        suppressedKeyUp = 0;
        leftDown = rightDown = suppressLeftUp = suppressRightUp = false;
    }
}
