using System.Runtime.InteropServices;

namespace Launcher.Win32;

/// <summary>
/// 共通のP/Invoke宣言
/// </summary>
internal static class NativeMethods
{
    /// <summary>
    /// 現在のユーザーがAdminかどうか
    /// </summary>
    public static bool IsUserAnAdmin()
    {
        try
        {
            return IsUserAnAdminNative();
        }
        catch (EntryPointNotFoundException)
        {
        }
        return false;
    }

    [DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsUserAnAdminNative();

    /// <summary>
    /// ハードリンクの作成
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
}
