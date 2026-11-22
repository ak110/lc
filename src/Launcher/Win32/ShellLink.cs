using System.Runtime.InteropServices;
using System.Text;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using UCOMIPersistFile = System.Runtime.InteropServices.ComTypes.IPersistFile;

namespace Launcher.Win32;

#region "COM Interop"

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
[ClassInterface(ClassInterfaceType.None)]
internal class ShellLinkObject { }

[ComImport]
[Guid("000214F9-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellLinkW
{
    void GetPath(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
        int cch,
        [MarshalAs(UnmanagedType.Struct)] ref WIN32_FIND_DATAW pfd,
        uint fFlags
        );

    void GetIDList(out IntPtr ppidl);

    void SetIDList(IntPtr pidl);

    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);

    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);

    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);

    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

    void GetHotkey(out ushort pwHotkey);

    void SetHotkey(ushort wHotkey);

    void GetShowCmd(out int piShowCmd);

    void SetShowCmd(int iShowCmd);

    void GetIconLocation(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
        int cch,
        out int piIcon
        );

    void SetIconLocation(
        [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
        int iIcon
        );

    void SetRelativePath(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,
        uint dwReserved
        );

    void Resolve(
        IntPtr hwnd,
        uint fFlags
        );

    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
struct WIN32_FIND_DATAW
{
    public const int MAX_PATH = 260;

    public uint dwFileAttributes;
    public FILETIME ftCreationTime;
    public FILETIME ftLastAccessTime;
    public FILETIME ftLastWriteTime;
    public uint nFileSizeHigh;
    public uint nFileSizeLow;
    public uint dwReserved0;
    public uint dwReserved1;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
    public string? cFileName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public string? cAlternateFileName;
}

#endregion

public sealed class ShellLink : IDisposable
{
    private IShellLinkW? shellLinkW;

    private bool disposed;

    internal const int MAX_PATH = 260;

    internal const uint SLGP_SHORTPATH = 0x0001;
    internal const uint SLGP_UNCPRIORITY = 0x0002;
    internal const uint SLGP_RAWPATH = 0x0004;

    #region "[型] ShellLinkDisplayMode列挙型"

    public enum ShellLinkDisplayMode : int
    {
        Normal = 1,

        Maximized = 3,

        Minimized = 7,
    }

    #endregion

    #region "[型] ShellLinkResolveFlags列挙型"

    [Flags]
    public enum ShellLinkResolveFlags : int
    {
        SLR_ANY_MATCH = 0x2,

        SLR_INVOKE_MSI = 0x80,

        SLR_NOLINKINFO = 0x40,

        SLR_NO_UI = 0x1,

        SLR_NO_UI_WITH_MSG_PUMP = 0x101,

        SLR_NOUPDATE = 0x8,

        SLR_NOSEARCH = 0x10,

        SLR_NOTRACK = 0x20,

        SLR_UPDATE = 0x4
    }

    #endregion

    #region "コンストラクション・デストラクション"

    public ShellLink()
    {
        try
        {
            shellLinkW = (IShellLinkW)(new ShellLinkObject());
        }
        catch
        {
            throw new COMException("IShellLink インターフェイスを取得できなかった");
        }
    }

    public ShellLink(string linkFile)
        : this()
    {
        Load(linkFile);
    }

    ~ShellLink()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        if (shellLinkW is not null)
        {
            Marshal.ReleaseComObject(shellLinkW);
            shellLinkW = null;
        }
    }

    #endregion

    #region "プロパティ"

    public string TargetPath
    {
        get
        {
            StringBuilder targetPath = new StringBuilder(MAX_PATH, MAX_PATH);
            var data = new WIN32_FIND_DATAW();
            shellLinkW!.GetPath(targetPath, targetPath.Capacity, ref data, SLGP_UNCPRIORITY);
            return targetPath.ToString();
        }
        set
        {
            shellLinkW!.SetPath(value);
        }
    }

    public string WorkingDirectory
    {
        get
        {
            StringBuilder workingDirectory = new StringBuilder(MAX_PATH, MAX_PATH);
            shellLinkW!.GetWorkingDirectory(workingDirectory, workingDirectory.Capacity);
            return workingDirectory.ToString();
        }
        set
        {
            shellLinkW!.SetWorkingDirectory(value);
        }
    }

    public string Arguments
    {
        get
        {
            StringBuilder arguments = new StringBuilder(MAX_PATH, MAX_PATH);
            shellLinkW!.GetArguments(arguments, arguments.Capacity);
            return arguments.ToString();
        }
        set
        {
            shellLinkW!.SetArguments(value);
        }
    }

    public string Description
    {
        get
        {
            StringBuilder description = new StringBuilder(MAX_PATH, MAX_PATH);
            shellLinkW!.GetDescription(description, description.Capacity);
            return description.ToString();
        }
        set
        {
            shellLinkW!.SetDescription(value);
        }
    }

    public ShellLinkDisplayMode DisplayMode
    {
        get
        {
            shellLinkW!.GetShowCmd(out int showCmd);
            return (ShellLinkDisplayMode)showCmd;
        }
        set
        {
            shellLinkW!.SetShowCmd((int)value);
        }
    }

    #endregion

    #region "保存と読み込み"

    private UCOMIPersistFile? GetIPersistFile()
    {
        return shellLinkW as UCOMIPersistFile;
    }

    public void Save(string linkFile)
    {
        UCOMIPersistFile? persistFile = GetIPersistFile();

        if (persistFile is null) throw new COMException("IPersistFile インターフェイスを取得できなかった");

        persistFile.Save(linkFile, true);
    }

    public void Load(string linkFile)
    {
        Load(linkFile, IntPtr.Zero, ShellLinkResolveFlags.SLR_ANY_MATCH | ShellLinkResolveFlags.SLR_NO_UI, 1);
    }

    public void Load(string linkFile, IntPtr hWnd, ShellLinkResolveFlags resolveFlags, int timeOutMilliseconds)
    {
        if (!File.Exists(linkFile)) throw new FileNotFoundException("ファイルが見つからなかった", linkFile);

        UCOMIPersistFile? persistFile = GetIPersistFile();

        if (persistFile is null) throw new COMException("IPersistFile インターフェイスを取得できなかった");

        persistFile.Load(linkFile, 0x00000000);

        uint flags = (uint)resolveFlags;

        if ((resolveFlags & ShellLinkResolveFlags.SLR_NO_UI) == ShellLinkResolveFlags.SLR_NO_UI)
        {
            flags |= (uint)(timeOutMilliseconds << 16);
        }

        shellLinkW!.Resolve(hWnd, flags);
    }

    #endregion
}
