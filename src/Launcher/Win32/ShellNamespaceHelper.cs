using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Launcher.Win32;

/// <summary>
/// パスから親IShellFolder・子PIDL・絶対PIDLを取り出すヘルパー。
/// IContextMenu取得のための共通処理を集約する。
/// </summary>
public static class ShellNamespaceHelper
{
    /// <summary>
    /// pathから親IShellFolder・子PIDL・絶対PIDLを取り出す。
    /// PIDL解放規約は.claude/rules/win32-interop.md「PIDL解放規約」節に従う。
    /// 呼び出し側は成功時に parent を Marshal.ReleaseComObject で解放する。
    /// SHParseDisplayNameはE_INVALIDARG・ERROR_FILE_NOT_FOUND等でWin32Exceptionを送出する。
    /// SHBindToParent失敗時は本メソッド内でfullPidlを解放し、例外を送出する。
    /// </summary>
    /// <exception cref="Win32Exception">SHParseDisplayNameまたはSHBindToParentの失敗</exception>
    public static void BindToParent(
        string path, out IShellFolder parent, out IntPtr childPidl, out IntPtr fullPidl)
    {
        int hr = SHParseDisplayName(path, IntPtr.Zero, out fullPidl, 0, out _);
        if (hr != 0 || fullPidl == IntPtr.Zero)
        {
            throw new Win32Exception(hr, "SHParseDisplayName failed");
        }
        var iid = typeof(IShellFolder).GUID;
        hr = SHBindToParent(fullPidl, ref iid, out parent, out childPidl);
        if (hr != 0)
        {
            Marshal.FreeCoTaskMem(fullPidl);
            fullPidl = IntPtr.Zero;
            throw new Win32Exception(hr, "SHBindToParent failed");
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHParseDisplayName(
        string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

    [DllImport("shell32.dll")]
    static extern int SHBindToParent(
        IntPtr pidl, ref Guid riid, out IShellFolder ppv, out IntPtr ppidlLast);
}

/// <summary>
/// Shell名前空間フォルダオブジェクトが実装するインターフェース。
/// 参照先: Microsoft Learn
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishellfolder
/// 簡略化: GetUIObjectOf以外のメソッドの複雑な型（STRRET・IEnumIDList等）をIntPtrで宣言しVTable順のみ厳密に保つ。
/// 既知の限界: GetUIObjectOf以外を呼び出す場合は当該メソッドの型を公式仕様どおりに置き換える必要がある。
/// 見直し契機: GetUIObjectOf以外を実際に呼び出す実装が加わった時点。
/// </summary>
[ComImport, Guid("000214E6-0000-0000-C000-000000000046"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellFolder
{
    [PreserveSig]
    int ParseDisplayName(
        IntPtr hwnd, IntPtr pbc, string pszDisplayName,
        out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);

    [PreserveSig]
    int EnumObjects(IntPtr hwnd, uint grfFlags, out IntPtr ppenumIDList);

    [PreserveSig]
    int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);

    [PreserveSig]
    int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int GetAttributesOf(
        uint cidl,
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl,
        ref uint rgfInOut);

    [PreserveSig]
    int GetUIObjectOf(
        IntPtr hwndOwner, uint cidl,
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl,
        ref Guid riid,
        IntPtr rgfReserved, out IntPtr ppv);

    [PreserveSig]
    int GetDisplayNameOf(IntPtr pidl, uint uFlags, out IntPtr pName);

    [PreserveSig]
    int SetNameOf(
        IntPtr hwnd, IntPtr pidl, string pszName, uint uFlags, out IntPtr ppidlOut);
}
