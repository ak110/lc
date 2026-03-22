using System.Runtime.InteropServices;
using System.Text;

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

    /// <summary>
    /// PATH環境変数に沿ってファイルを検索する
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SearchPath(
        string? lpPath,
        string lpFileName,
        string? lpExtension,
        int nBufferLength,
        StringBuilder lpBuffer,
        out IntPtr lpFilePart);
}
