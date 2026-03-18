#nullable disable
using System.Runtime.InteropServices;

namespace Launcher.Win32 {
    /// <summary>
    /// 共通のP/Invoke宣言
    /// </summary>
    internal static class NativeMethods {
        /// <summary>
        /// 現在のユーザーがAdminかどうか
        /// </summary>
        public static bool IsUserAnAdmin() {
            try {
                return IsUserAnAdminNative();
            } catch (EntryPointNotFoundException) {
            }
            return false;
        }

        [DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdminNative();
    }
}
