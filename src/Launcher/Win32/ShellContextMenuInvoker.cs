using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Launcher.Infrastructure;

namespace Launcher.Win32;

/// <summary>
/// エクスプローラの「その他のオプションを表示」相当（従来Win32シェル拡張メニュー）を表示する。
/// Windows 11の新Shellメニュー（IExplorerCommand + app identityベース）は対象外。
/// 呼び出しはUIスレッド（STA）上で完結する。
/// </summary>
public static class ShellContextMenuInvoker
{
    const uint CMD_FIRST = 1;
    const uint CMF_NORMAL = 0x00000000;
    const uint CMF_EXPLORE = 0x00000004;
    const uint TPM_RETURNCMD = 0x0100;

    // AV発生ステージ特定後にtrueへ切り替えて代替経路を有効化する。
    // static readonlyとしJITのコンパイル時定数畳み込みを避け、CS0162（到達不能コード）を発生させない。
    // フィールド初期化子の既定値(false)はフォーマッターが冗長として除去するため、静的コンストラクターで代入する。
    // 詳細は.claude/rules/win32-interop.md「AccessViolationクラッシュの診断」節を参照。
    static readonly bool UseShellItemPath;

    static ShellContextMenuInvoker()
    {
        UseShellItemPath = false;
    }

    /// <summary>
    /// pathのShellコンテキストメニューをscreenLocationに表示し、選択項目を実行する。
    /// 呼び出し前に、親のContextMenuStrip等のWinFormsメニューモーダルループを
    /// 閉じた状態にしておく必要がある。
    /// 詳細は.claude/rules/win32-interop.md
    /// 「ContextMenuStrip項目からShellモーダルUIを呼ぶ場合の親メニュークローズ」節を参照。
    /// </summary>
    /// <exception cref="Win32Exception">Shell API呼び出しの失敗</exception>
    /// <exception cref="COMException">Shell拡張実装の失敗</exception>
    /// <exception cref="ExternalException">Shell拡張実装の失敗</exception>
    public static void Show(string path, IntPtr ownerHwnd, Point screenLocation)
    {
        if (UseShellItemPath)
        {
            ShellItemContextMenuInvoker.Show(path, ownerHwnd, screenLocation);
            return;
        }
        DiagnosticLog.Trace("Shell.Show", $"before BindToParent path={path}");
        ShellNamespaceHelper.BindToParent(path, out var parent, out var childPidl, out var fullPidl);
        DiagnosticLog.Trace("Shell.Show", "after BindToParent");
        object? contextMenuObj = null;
        try
        {
            var apidl = new[] { childPidl };
            var iidContextMenu = typeof(IContextMenu).GUID;
            DiagnosticLog.Trace("Shell.Show", "before GetUIObjectOf");
            int hr = parent.GetUIObjectOf(
                ownerHwnd, 1, apidl, ref iidContextMenu, IntPtr.Zero, out IntPtr ppv);
            DiagnosticLog.Trace("Shell.Show", $"after GetUIObjectOf hr=0x{hr:x8}");
            if (hr != 0 || ppv == IntPtr.Zero)
            {
                throw new Win32Exception(hr, $"GetUIObjectOf failed for {path}");
            }
            // Marshal.GetObjectForIUnknownが例外を送出してもppvのIUnknown参照を解放するため、
            // try/finallyで囲む。
            // 詳細は.claude/rules/win32-interop.md「IUnknown生ポインタとRCWの同時保持」節を参照。
            try
            {
                DiagnosticLog.Trace("Shell.Show", "before GetObjectForIUnknown");
                contextMenuObj = Marshal.GetObjectForIUnknown(ppv);
                DiagnosticLog.Trace("Shell.Show", "after GetObjectForIUnknown");
            }
            finally
            {
                Marshal.Release(ppv);
            }
            ShowContextMenu(contextMenuObj, ownerHwnd, screenLocation, "Shell.Show");
        }
        finally
        {
            if (contextMenuObj is not null) Marshal.ReleaseComObject(contextMenuObj);
            Marshal.ReleaseComObject(parent);
            Marshal.FreeCoTaskMem(fullPidl); // childPidlはfullPidl内部を指すため独立解放しない
        }
    }

    /// <summary>
    /// contextMenuObjから取得したIContextMenuに対し、
    /// CreatePopupMenu→QueryContextMenu→MenuMessageForwarder→TrackPopupMenuEx→InvokeCommandの
    /// 共通シーケンスを実行する。
    /// IShellFolderチェーン経由（<see cref="Show"/>）とIShellItem経由
    /// （<see cref="ShellItemContextMenuInvoker.Show"/>）の双方から呼ばれるSSOT実装。
    /// 呼び出し元はcontextMenuObjの解放（<see cref="Marshal.ReleaseComObject(object)"/>）を
    /// 自身のfinallyブロックで行う。
    /// </summary>
    /// <param name="diagnosticCategory">診断ログのカテゴリー識別子（"Shell.Show"または"ShellItem.Show"）</param>
    internal static void ShowContextMenu(
        object contextMenuObj, IntPtr ownerHwnd, Point screenLocation, string diagnosticCategory)
    {
        var contextMenu = (IContextMenu)contextMenuObj;
        IntPtr hMenu = IntPtr.Zero;
        MenuMessageForwarder? forwarder = null;
        try
        {
            DiagnosticLog.Trace(diagnosticCategory, "before CreatePopupMenu");
            hMenu = CreatePopupMenu();
            DiagnosticLog.Trace(diagnosticCategory, $"after CreatePopupMenu hMenu=0x{hMenu.ToInt64():x}");
            if (hMenu == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreatePopupMenu failed");
            }
            DiagnosticLog.Trace(diagnosticCategory, "before QueryContextMenu");
            int hr = contextMenu.QueryContextMenu(hMenu, 0, CMD_FIRST, uint.MaxValue, CMF_NORMAL | CMF_EXPLORE);
            DiagnosticLog.Trace(diagnosticCategory, $"after QueryContextMenu hr=0x{hr:x8}");
            if (hr < 0)
            {
                throw new Win32Exception(hr, "QueryContextMenu failed");
            }

            DiagnosticLog.Trace(diagnosticCategory, "before MenuMessageForwarder");
            forwarder = new MenuMessageForwarder(contextMenuObj, ownerHwnd);
            DiagnosticLog.Trace(diagnosticCategory, $"before TrackPopupMenuEx hwnd=0x{ownerHwnd.ToInt64():x}");
            // TPM_RIGHTBUTTONは付与しない。付与すると呼び出し元の右クリック残留
            // （WM_RBUTTONUP直後のTrackPopupMenuEx表示）が最初の項目選択として認識され、
            // ユーザー未操作のまま「開く」などのInvokeCommandが実行される事例がある。
            // 左ボタンのみでの項目選択に限定する。
            int cmd = TrackPopupMenuEx(
                hMenu, TPM_RETURNCMD,
                screenLocation.X, screenLocation.Y, ownerHwnd, IntPtr.Zero);
            DiagnosticLog.Trace(diagnosticCategory, $"after TrackPopupMenuEx cmd={cmd}");
            if (cmd <= 0) return;

            var invokeInfo = new CMINVOKECOMMANDINFO
            {
                cbSize = Marshal.SizeOf<CMINVOKECOMMANDINFO>(),
                fMask = 0,
                hwnd = ownerHwnd,
                lpVerb = checked((IntPtr)(cmd - CMD_FIRST)),
                lpParameters = null,
                lpDirectory = null,
                nShow = SW_SHOWNORMAL,
                dwHotKey = 0,
                hIcon = IntPtr.Zero,
            };
            DiagnosticLog.Trace(diagnosticCategory, "before InvokeCommand");
            hr = contextMenu.InvokeCommand(ref invokeInfo);
            DiagnosticLog.Trace(diagnosticCategory, $"after InvokeCommand hr=0x{hr:x8}");
            if (hr != 0)
            {
                throw new Win32Exception(hr, "InvokeCommand failed");
            }
        }
        finally
        {
            forwarder?.Dispose();
            if (hMenu != IntPtr.Zero) DestroyMenu(hMenu);
        }
    }

    const int SW_SHOWNORMAL = 1;

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    static extern int TrackPopupMenuEx(
        IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);
}

/// <summary>
/// TrackPopupMenuEx表示中にWM_INITMENUPOPUP・WM_MENUCHAR・WM_DRAWITEM・WM_MEASUREITEMを
/// IContextMenu3.HandleMenuMsg2（優先）またはIContextMenu2.HandleMenuMsgへ転送する。
/// 参照先: .claude/rules/win32-interop.md「Shell IContextMenuのメッセージ転送」節
/// AssignHandle(ownerHwnd)によるサブクラス化のため、同一ownerHwndに対する多重生成を禁止する。
/// 生存区間中に別のMenuMessageForwarderをAssignHandleするとWNDPROCチェーンが破損する。
/// </summary>
internal sealed class MenuMessageForwarder : NativeWindow, IDisposable
{
    const int WM_INITMENUPOPUP = 0x0117;
    const int WM_MENUCHAR = 0x0120;
    const int WM_DRAWITEM = 0x002B;
    const int WM_MEASUREITEM = 0x002C;

    readonly IContextMenu2? contextMenu2;
    readonly IContextMenu3? contextMenu3;

    public MenuMessageForwarder(object contextMenu, IntPtr ownerHwnd)
    {
        contextMenu3 = contextMenu as IContextMenu3;
        contextMenu2 = contextMenu3 is null ? contextMenu as IContextMenu2 : null;
        // ownerHwndを直接サブクラス化して4種メッセージを転送する。
        // 従来の CreateHandle(Parent=ownerHwnd) はStyle=0のためowned top-level windowになり、
        // 一部Shell拡張が親チェーン走査時に誤解する可能性があった。
        // 詳細は.claude/rules/win32-interop.md「AccessViolationクラッシュの診断」節を参照。
        AssignHandle(ownerHwnd);
    }

    protected override void WndProc(ref Message m)
    {
        if (IsMenuMessage(m.Msg))
        {
            if (contextMenu3 is not null)
            {
                IntPtr lResult;
                contextMenu3.HandleMenuMsg2((uint)m.Msg, m.WParam, m.LParam, out lResult);
                m.Result = lResult;
                return;
            }
            if (contextMenu2 is not null)
            {
                contextMenu2.HandleMenuMsg((uint)m.Msg, m.WParam, m.LParam);
                return;
            }
        }
        base.WndProc(ref m);
    }

    static bool IsMenuMessage(int msg) =>
        msg is WM_INITMENUPOPUP or WM_MENUCHAR or WM_DRAWITEM or WM_MEASUREITEM;

    // AssignHandleしたハンドルはownerHwndの所有物のためReleaseHandleのみ行う。
    public void Dispose() => ReleaseHandle();
}

/// <summary>
/// ショートカットメニューに項目を追加・実行するためのインターフェース。
/// 参照先: Microsoft Learn
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-icontextmenu
/// </summary>
[ComImport, Guid("000214E4-0000-0000-C000-000000000046"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IContextMenu
{
    [PreserveSig]
    int QueryContextMenu(
        IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);

    [PreserveSig]
    int InvokeCommand(ref CMINVOKECOMMANDINFO pici);

    [PreserveSig]
    int GetCommandString(
        IntPtr idCmd, uint uType, IntPtr pReserved, IntPtr pszName, uint cchMax);
}

/// <summary>
/// 所有者描画メニュー項目に関連するメッセージ処理を追加するインターフェース。
/// 参照先: Microsoft Learn
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-icontextmenu2
/// COM継承はVTable継承のため、IContextMenuのメソッドをこちらへ再宣言する。
/// </summary>
[ComImport, Guid("000214F4-0000-0000-C000-000000000046"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IContextMenu2
{
    [PreserveSig]
    int QueryContextMenu(
        IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);

    [PreserveSig]
    int InvokeCommand(ref CMINVOKECOMMANDINFO pici);

    [PreserveSig]
    int GetCommandString(
        IntPtr idCmd, uint uType, IntPtr pReserved, IntPtr pszName, uint cchMax);

    [PreserveSig]
    int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
}

/// <summary>
/// IContextMenu2を拡張し、メッセージ処理結果のLRESULTを受け取れるインターフェース。
/// 参照先: Microsoft Learn
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-icontextmenu3
/// COM継承はVTable継承のため、IContextMenu・IContextMenu2のメソッドをこちらへ再宣言する。
/// </summary>
[ComImport, Guid("BCFCE0A0-EC17-11d0-8D10-00A0C90F2719"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IContextMenu3
{
    [PreserveSig]
    int QueryContextMenu(
        IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);

    [PreserveSig]
    int InvokeCommand(ref CMINVOKECOMMANDINFO pici);

    [PreserveSig]
    int GetCommandString(
        IntPtr idCmd, uint uType, IntPtr pReserved, IntPtr pszName, uint cchMax);

    [PreserveSig]
    int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);

    [PreserveSig]
    int HandleMenuMsg2(uint uMsg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);
}

/// <summary>
/// IContextMenu.InvokeCommandへ渡すコマンド情報。
/// lpVerbはANSI文字列またはMAKEINTRESOURCE(コマンドオフセット)を格納するため、
/// stringではなくIntPtrで宣言する。
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct CMINVOKECOMMANDINFO
{
    public int cbSize;
    public uint fMask;
    public IntPtr hwnd;
    public IntPtr lpVerb;
    [MarshalAs(UnmanagedType.LPStr)] public string? lpParameters;
    [MarshalAs(UnmanagedType.LPStr)] public string? lpDirectory;
    public int nShow;
    public uint dwHotKey;
    public IntPtr hIcon;
}
