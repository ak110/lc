namespace Launcher.Core;

/// <summary>UI入力の判断に使う修飾キー。OSのキーコードから独立する。</summary>
[Flags]
public enum InputModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Windows = 8,
}
