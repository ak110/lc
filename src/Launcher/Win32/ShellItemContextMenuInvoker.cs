using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Launcher.Win32;

/// <summary>
/// SHCreateItemFromParsingName + IShellItem.BindToHandler(BHID_SFUIObject) 経由で
/// IContextMenu を取得しコンテキストメニューを表示する代替経路。
/// IShellFolder チェーンを回避することで VTable スロットずれのリスクを低減する。
/// </summary>
internal static class ShellItemContextMenuInvoker
{
    static readonly Guid BHID_SFUIObject = new("3981E224-F559-11D3-8E3A-00C04F6837D5");

    /// <summary>
    /// path の Shell コンテキストメニューを screenLocation に表示し、選択項目を実行する。
    /// CreatePopupMenu以降の共通シーケンスは<see cref="ShellContextMenuInvoker.ShowContextMenu"/>に委譲する。
    /// </summary>
    /// <exception cref="Win32Exception">Shell API 呼び出しの失敗</exception>
    /// <exception cref="COMException">Shell 拡張実装の失敗</exception>
    /// <exception cref="ExternalException">Shell 拡張実装の失敗</exception>
    public static void Show(string path, IntPtr ownerHwnd, Point screenLocation)
    {
        var iidShellItem = typeof(IShellItem).GUID;
        int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref iidShellItem, out var shellItem);
        if (hr != 0 || shellItem is null)
        {
            throw new Win32Exception(hr, "SHCreateItemFromParsingName failed");
        }

        object? contextMenuObj = null;
        try
        {
            var iidContextMenu = typeof(IContextMenu).GUID;
            var bhid = BHID_SFUIObject;
            hr = shellItem.BindToHandler(IntPtr.Zero, ref bhid, ref iidContextMenu, out IntPtr ppv);
            if (hr != 0 || ppv == IntPtr.Zero)
            {
                throw new Win32Exception(hr, "BindToHandler failed");
            }
            try
            {
                contextMenuObj = Marshal.GetObjectForIUnknown(ppv);
            }
            finally
            {
                Marshal.Release(ppv);
            }
            ShellContextMenuInvoker.ShowContextMenu(contextMenuObj, ownerHwnd, screenLocation);
        }
        finally
        {
            if (contextMenuObj is not null) Marshal.ReleaseComObject(contextMenuObj);
            Marshal.ReleaseComObject(shellItem);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, ref Guid riid, out IShellItem ppv);
}

/// <summary>
/// IShellItem インターフェース。
/// 参照先: Microsoft Learn
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishellitem
/// </summary>
[ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    [PreserveSig]
    int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int GetParent(out IShellItem ppsi);

    [PreserveSig]
    int GetDisplayName(uint sigdnName, out IntPtr ppszName);

    [PreserveSig]
    int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

    [PreserveSig]
    int Compare(IShellItem psi, uint hint, out int piOrder);
}
