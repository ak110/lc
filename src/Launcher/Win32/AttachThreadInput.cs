using System.Runtime.InteropServices;

namespace Launcher.Win32;

/// <summary>
/// WinAPI AttachThreadInput()のラッパー
/// </summary>
public sealed class AttachThreadInput : IDisposable
{
    /// <summary>
    /// スレッドID
    /// </summary>
    uint foreground, current;
    bool attached;

    /// <summary>
    /// アタッチ。
    /// </summary>
    public AttachThreadInput()
    {
        foreground = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
        current = GetCurrentThreadId();
        // 前景が切り替わる途中にはウィンドウが無く、接続先のスレッドIDも0になる。
        if (foreground != 0 && current != foreground)
        {
            attached = AttachThreadInput_(current, foreground, true);
            // 別desktopや入力キューの終了などで接続できない場合も通常の表示処理を続ける。
            if (!attached) System.Diagnostics.Debug.WriteLine(new System.ComponentModel.Win32Exception().Message);
        }
    }

    /// <summary>
    /// デタッチ。
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (attached)
        {
            attached = false;
            bool result = AttachThreadInput_(current, foreground, false);
            if (!result) System.Diagnostics.Debug.WriteLine(new System.ComponentModel.Win32Exception().Message);
        }
    }

    #region WinAPI

    [DllImport("kernel32.dll")]
    extern static uint GetCurrentThreadId();



    [DllImport("user32.dll", EntryPoint = "AttachThreadInput", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    extern static bool AttachThreadInput_(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    #endregion
}
