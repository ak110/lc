using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Launcher.Infrastructure;

/// <summary>
/// コピーの最中に変更されたぞ例外
/// </summary>
[Serializable]
public class FileChangedOnCopyException : IOException
{
    public FileChangedOnCopyException()
        : base() { }

    public FileChangedOnCopyException(string fileName)
        : base($"ファイル '{fileName}' がコピーの最中に変更されました") { }

    public FileChangedOnCopyException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// BackupReadとかのらっぱー。
/// </summary>
public sealed class BackupFile : IDisposable
{
    SafeFileHandle? handle;
    bool processSecurity;

    /// <summary>
    /// ファイルを開く
    /// </summary>
    /// <param name="path">ファイルのパス</param>
    /// <param name="write">書き込みを行うのかどうか</param>
    /// <exception cref="IOException">エラー</exception>
    public BackupFile(string path, bool write)
    {
        uint desiredAccess;
        uint creationDisposition;
        uint shareMode;

        if (write)
        {
            desiredAccess = GENERIC_WRITE | WRITE_OWNER | WRITE_DAC;
            shareMode = FILE_SHARE_READ;
            creationDisposition = CREATE_ALWAYS;
        }
        else
        {
            desiredAccess = GENERIC_READ;
            shareMode = FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE;
            creationDisposition = OPEN_EXISTING;
        }

        handle = CreateFile(path, desiredAccess, shareMode, IntPtr.Zero,
            creationDisposition, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            throw new IOException(path + " のオープンに失敗", new Win32Exception());
        }
    }

    /// <summary>
    /// 後始末。
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (handle != null)
        {
            handle.Dispose();
            handle = null;
        }
    }

    /// <summary>
    /// Securityなフラグとかを処理るかどうか。
    /// </summary>
    public bool ProcessSecurity
    {
        get { return processSecurity; }
        set { processSecurity = value; }
    }

    /// <summary>
    /// 読み込み
    /// </summary>
    /// <exception cref="IOException">エラー</exception>
    public void ReadTo(Stream destination)
    {
        byte[] buffer = new byte[0x10000];

        uint bytesRead = 0;
        IntPtr context = IntPtr.Zero;

        while (true)
        {
            if (!BackupRead(handle!, buffer,
                (uint)buffer.Length, ref bytesRead,
                false, processSecurity, ref context))
            {
                throw new IOException("ファイルの読み込みに失敗", new Win32Exception());
            }
            if (bytesRead <= 0)
            {
                break;
            }
            destination.Write(buffer, 0, (int)bytesRead);
        }

        if (!BackupRead(IntPtr.Zero, null!,
            0, ref bytesRead, true, processSecurity,
            ref context))
        {
            throw new IOException("ファイルの読み込みに失敗", new Win32Exception());
        }
    }

    /// <summary>
    /// 書き込み
    /// </summary>
    /// <exception cref="IOException">エラー</exception>
    public void WriteFrom(Stream source)
    {
        byte[] buffer = new byte[0x10000];

        uint bytesRead = 0;
        uint bytesWritten = 0;
        IntPtr context = IntPtr.Zero;

        while (true)
        {
            bytesRead = (uint)source.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
            {
                break;
            }
            if (!BackupWrite(handle!, buffer,
                bytesRead, ref bytesWritten,
                false, processSecurity, ref context) ||
                bytesRead != bytesWritten)
            {
                throw new IOException("ファイルへの書き込みに失敗", new Win32Exception());
            }
        }

        if (!BackupWrite(IntPtr.Zero,
            null!, 0, ref bytesWritten,
            true, processSecurity, ref context))
        {
            throw new IOException("ファイルへの書き込みに失敗", new Win32Exception());
        }
    }

    /// <summary>
    /// コピー。
    /// </summary>
    /// <param name="destination">コピー先</param>
    /// <exception cref="IOException">エラー</exception>
    public void CopyTo(BackupFile destination)
    {
        byte[] buffer = new byte[65536];

        uint bytesRead = 0;
        uint bytesWritten = 0;
        IntPtr readContext = IntPtr.Zero;
        IntPtr writeContext = IntPtr.Zero;

        while (true)
        {
            if (!BackupRead(handle!, buffer,
                (uint)buffer.Length, ref bytesRead,
                false, processSecurity, ref readContext))
            {
                throw new IOException("ファイルのコピーに失敗", new Win32Exception());
            }
            if (bytesRead <= 0)
            {
                break;
            }
            if (!BackupWrite(destination.handle!, buffer,
                bytesRead, ref bytesWritten,
                false, processSecurity, ref writeContext) ||
                bytesRead != bytesWritten)
            {
                throw new IOException("ファイルのコピーに失敗", new Win32Exception());
            }
        }

        if (!BackupRead(IntPtr.Zero, null!,
            0, ref bytesRead, true, processSecurity,
            ref readContext))
        {
            throw new IOException("ファイルのコピーに失敗", new Win32Exception());
        }
        if (!BackupWrite(IntPtr.Zero,
            null!, 0, ref bytesWritten,
            true, processSecurity, ref writeContext))
        {
            throw new IOException("ファイルのコピーに失敗", new Win32Exception());
        }
    }

    /// <summary>
    /// ファイルのコピー。
    /// </summary>
    /// <param name="sourceName">コピー元</param>
    /// <param name="destFileName">コピー先</param>
    /// <exception cref="IOException">エラー</exception>
    /// <exception cref="FileChangedOnCopyException"></exception>
    public static void CopyFile(string sourceName, string destFileName)
    {
        var srcInfo = new FileInfo(sourceName);
        var dstInfo = new FileInfo(destFileName);
        DateTime srcLastWrite = srcInfo.LastWriteTime;
        long srcLength = srcInfo.Length;

        using (BackupFile dst = new BackupFile(destFileName, true))
        using (BackupFile src = new BackupFile(sourceName, false))
        {
            src.CopyTo(dst);
        }

        srcInfo.Refresh();
        if (srcInfo.LastWriteTime != srcLastWrite ||
            srcInfo.Length != srcLength)
        {
            // ↑どーも頼りない検出方法だが。。
            dstInfo.Delete();
            throw new FileChangedOnCopyException(srcInfo.FullName);
        }
    }

    #region APIとか。

    const uint GENERIC_READ = 0x80000000;
    const uint GENERIC_WRITE = 0x40000000;
    const uint GENERIC_EXECUTE = 0x20000000;
    const uint GENERIC_ALL = 0x10000000;

    const uint DELETE = 0x00010000;
    const uint READ_CONTROL = 0x00020000;
    const uint WRITE_DAC = 0x00040000;
    const uint WRITE_OWNER = 0x00080000;
    const uint SYNCHRONIZE = 0x00100000;

    const int FILE_SHARE_READ = 0x00000001;
    const int FILE_SHARE_WRITE = 0x00000002;
    const int FILE_SHARE_DELETE = 0x00000004;

    const uint CREATE_NEW = 1;
    const uint CREATE_ALWAYS = 2;
    const uint OPEN_EXISTING = 3;
    const uint OPEN_ALWAYS = 4;
    const uint TRUNCATE_EXISTING = 5;

    const uint ACCESS_SYSTEM_SECURITY = 0x01000000;

    const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
    const uint FILE_FLAG_OVERLAPPED = 0x40000000;
    const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
    const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
    const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
    const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
    const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    const uint FILE_FLAG_POSIX_SEMANTICS = 0x01000000;
    const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    const uint FILE_FLAG_OPEN_NO_RECALL = 0x00100000;
    const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFile(string lpFileName,
        uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
        uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool BackupRead(SafeFileHandle hFile, byte[] lpBuffer,
        uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, bool bAbort,
        bool bProcessSecurity, ref IntPtr lpContext);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool BackupWrite(SafeFileHandle hFile, byte[] lpBuffer,
        uint nNumberOfBytesToWrite, ref uint lpNumberOfBytesWritten,
        bool bAbort, bool bProcessSecurity, ref IntPtr lpContext);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool BackupRead(IntPtr hFile, byte[] lpBuffer,
        uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, bool bAbort,
        bool bProcessSecurity, ref IntPtr lpContext);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool BackupWrite(IntPtr hFile, byte[] lpBuffer,
        uint nNumberOfBytesToWrite, ref uint lpNumberOfBytesWritten,
        bool bAbort, bool bProcessSecurity, ref IntPtr lpContext);

    #endregion
}
