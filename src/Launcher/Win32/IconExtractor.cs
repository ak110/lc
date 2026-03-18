using System.IO;
using System.Runtime.InteropServices;

namespace Launcher.Win32;

/// <summary>
/// Icon.ExtractAssociatedIcon()の代替。
/// </summary>
public static class IconExtractor
{
    /// <summary>
    /// Icon.ExtractAssociatedIcon()が今一歩なので、実質SHGetFileInfo()のラッパー。
    /// </summary>
    /// <param name="path">取得するファイルやフォルダのパス</param>
    /// <param name="small">小さいアイコンかどうか。</param>
    /// <returns>失敗時null</returns>
    /// <exception cref="FileLoadException">読み込み失敗</exception>
    public static System.Drawing.Icon ExtractAssociatedIcon(string path, bool small)
    {
        int uFlags = SHGFI_ICON | SHGFI_SYSICONINDEX |
            (small ? SHGFI_SMALLICON : SHGFI_LARGEICON);
        SHFILEINFO shinfo = new SHFILEINFO();
        IntPtr hSuccess = SHGetFileInfo(path, 0, ref shinfo,
            Marshal.SizeOf(shinfo), uFlags);
        if (hSuccess == IntPtr.Zero)
        {
            throw new FileLoadException(path + " のアイコンの取得に失敗しました");
        }
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

    const int SHGFI_ICON = 0x000000100;             // get icon
    const int SHGFI_DISPLAYNAME = 0x000000200;      // get display name
    const int SHGFI_TYPENAME = 0x000000400;         // get type name
    const int SHGFI_ATTRIBUTES = 0x000000800;       // get attributes
    const int SHGFI_ICONLOCATION = 0x000001000;     // get icon location
    const int SHGFI_EXETYPE = 0x000002000;          // return exe type
    const int SHGFI_SYSICONINDEX = 0x000004000;     // get system icon index
    const int SHGFI_LINKOVERLAY = 0x000008000;      // put a link overlay on icon
    const int SHGFI_SELECTED = 0x000010000;         // show icon in selected state
                                                    //#if (NTDDI_VERSION >= NTDDI_WIN2K)
    const int SHGFI_ATTR_SPECIFIED = 0x000020000;   // get only specified attributes
                                                    //#endif // (NTDDI_VERSION >= NTDDI_WIN2K)
    const int SHGFI_LARGEICON = 0x000000000;        // get large icon
    const int SHGFI_SMALLICON = 0x000000001;        // get small icon
    const int SHGFI_OPENICON = 0x000000002;         // get open icon
    const int SHGFI_SHELLICONSIZE = 0x000000004;    // get shell size icon
    const int SHGFI_PIDL = 0x000000008;             // pszPath is a pidl
    const int SHGFI_USEFILEATTRIBUTES = 0x000000010;// use passed dwFileAttribute
                                                    //#if (_WIN32_IE >= = 0x0500)
    const int SHGFI_ADDOVERLAYS = 0x000000020;      // apply the appropriate overlays
    const int SHGFI_OVERLAYINDEX = 0x000000040;     // Get the index of the overlay
                                                    // in the upper 8 bits of the iIcon
                                                    //#endif

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbSizeFileInfo, int uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    extern static bool DestroyIcon(IntPtr handle);
    #endregion
}
