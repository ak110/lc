using System.Runtime.InteropServices;

namespace Launcher.Win32;

public static class IconExtractor
{
    public static System.Drawing.Icon ExtractAssociatedIcon(string path, bool small)
    {
        int uFlags = SHGFI_ICON | SHGFI_SYSICONINDEX |
            (small ? SHGFI_SMALLICON : SHGFI_LARGEICON);
        var shinfo = new SHFILEINFO();
        IntPtr hSuccess = SHGetFileInfo(path, 0, ref shinfo,
            Marshal.SizeOf(shinfo), uFlags);
        if (hSuccess == IntPtr.Zero)
        {
            throw new FileLoadException(path + " のアイコンの取得に失敗した");
        }
        return IconFromShFileInfo(shinfo);
    }

    public static System.Drawing.Icon ExtractIconByShellNamespace(string displayName, bool small)
    {
        int hr = NativeMethods.SHParseDisplayName(displayName, IntPtr.Zero, out IntPtr pidl, 0, out _);
        if (hr != 0 || pidl == IntPtr.Zero)
        {
            throw new FileLoadException(displayName + " の PIDL 取得に失敗した");
        }
        try
        {
            int uFlags = SHGFI_ICON | SHGFI_SYSICONINDEX | SHGFI_PIDL |
                (small ? SHGFI_SMALLICON : SHGFI_LARGEICON);
            var shinfo = new SHFILEINFO();
            IntPtr hSuccess = SHGetFileInfoPidl(pidl, 0, ref shinfo,
                Marshal.SizeOf(shinfo), uFlags);
            if (hSuccess == IntPtr.Zero)
            {
                throw new FileLoadException(displayName + " のアイコンの取得に失敗した");
            }
            return IconFromShFileInfo(shinfo);
        }
        finally
        {
            CoTaskMemFree(pidl);
        }
    }

    static System.Drawing.Icon IconFromShFileInfo(SHFILEINFO shinfo)
    {
        // Icon.FromHandle()はハンドルを所有しないため、Clone()で独立コピーを作成し
        // 元のハンドルはDestroyIcon()で明示的に解放する
        try
        {
            using var tempIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
            return (System.Drawing.Icon)tempIcon.Clone();
        }
        finally
        {
            DestroyIcon(shinfo.hIcon);
        }
    }

    #region SHGetFileInfo()関係

    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public int dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string? szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string? szTypeName;
    };

    const int SHGFI_ICON = 0x000000100;
    const int SHGFI_DISPLAYNAME = 0x000000200;
    const int SHGFI_TYPENAME = 0x000000400;
    const int SHGFI_ATTRIBUTES = 0x000000800;
    const int SHGFI_ICONLOCATION = 0x000001000;
    const int SHGFI_EXETYPE = 0x000002000;
    const int SHGFI_SYSICONINDEX = 0x000004000;
    const int SHGFI_LINKOVERLAY = 0x000008000;
    const int SHGFI_SELECTED = 0x000010000;
    const int SHGFI_ATTR_SPECIFIED = 0x000020000;
    const int SHGFI_LARGEICON = 0x000000000;
    const int SHGFI_SMALLICON = 0x000000001;
    const int SHGFI_OPENICON = 0x000000002;
    const int SHGFI_SHELLICONSIZE = 0x000000004;
    const int SHGFI_PIDL = 0x000000008;
    const int SHGFI_USEFILEATTRIBUTES = 0x000000010;
    const int SHGFI_ADDOVERLAYS = 0x000000020;
    const int SHGFI_OVERLAYINDEX = 0x000000040;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbSizeFileInfo, int uFlags);

    // SHGFI_PIDL使用時はpszPathにPIDLを渡す (IntPtrオーバーロード)
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfo")]
    static extern IntPtr SHGetFileInfoPidl(IntPtr pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbSizeFileInfo, int uFlags);


    [DllImport("ole32.dll")]
    static extern void CoTaskMemFree(IntPtr pv);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    extern static bool DestroyIcon(IntPtr handle);
    #endregion
}
