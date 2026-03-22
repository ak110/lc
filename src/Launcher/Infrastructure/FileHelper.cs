using System.Security.AccessControl;
using System.Text;
using Microsoft.Win32;

namespace Launcher.Infrastructure;

/// <summary>
/// ファイル・ディレクトリ操作のユーティリティ
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// bare name（パス区切りを含まない名前）をアイコン取得用にパス解決する。
    /// ShellExecuteExの解決順（App Paths → PATH検索）に合わせる。
    /// </summary>
    /// <param name="path">ファイル名またはパス</param>
    /// <returns>解決されたフルパス、または元のpath</returns>
    public static string ResolveExecutable(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path ?? string.Empty;
        }

        // パス区切りを含む場合、またはシェル特殊パス（"::{CLSID}"や"shell:xxx"）は解決不要
        if (path.Contains('\\') || path.Contains('/')
            || path.StartsWith("::", StringComparison.Ordinal)
            || path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        // App Paths レジストリ検索（ShellExecuteExと同じく最優先）
        string? resolved = SearchAppPaths(path);
        if (resolved != null)
        {
            return resolved;
        }

        // SearchPath APIによるPATH検索
        resolved = SearchPathApi(path);
        if (resolved != null)
        {
            return resolved;
        }

        return path;
    }

    /// <summary>
    /// App Pathsレジストリからbare nameを検索する。HKCU → HKLMの順。
    /// </summary>
    static string? SearchAppPaths(string name)
    {
        // 名前そのもの + PATHEXTの各拡張子を付けた名前を試す
        var namesToTry = new List<string> { name };
        if (!Path.HasExtension(name))
        {
            foreach (string ext in GetPathExtExtensions())
            {
                namesToTry.Add(name + ext);
            }
        }

        RegistryKey[] roots = [Registry.CurrentUser, Registry.LocalMachine];
        foreach (string keyName in namesToTry)
        {
            foreach (var root in roots)
            {
                string subKeyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{keyName}";
                using var key = root.OpenSubKey(subKeyPath);
                if (key?.GetValue(null) is string value && !string.IsNullOrEmpty(value))
                {
                    // 引用符を除去し、環境変数を展開
                    string expanded = Environment.ExpandEnvironmentVariables(
                        value.Trim('"'));
                    if (File.Exists(expanded))
                    {
                        return expanded;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// SearchPath APIでPATH上のファイルを検索する。
    /// </summary>
    static string? SearchPathApi(string name)
    {
        if (Path.HasExtension(name))
        {
            return SearchPathSingle(name, null);
        }

        // 拡張子なしの場合、PATHEXTの各拡張子で試す
        foreach (string ext in GetPathExtExtensions())
        {
            string? found = SearchPathSingle(name, ext);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// SearchPath API単発呼び出し
    /// </summary>
    static string? SearchPathSingle(string fileName, string? extension)
    {
        var buffer = new StringBuilder(260);
        int result = Win32.NativeMethods.SearchPath(
            null, fileName, extension, buffer.Capacity, buffer, out _);
        if (result > 0 && result <= buffer.Capacity)
        {
            return buffer.ToString();
        }
        // バッファ不足の場合はリトライ
        if (result > buffer.Capacity)
        {
            buffer.EnsureCapacity(result);
            result = Win32.NativeMethods.SearchPath(
                null, fileName, extension, buffer.Capacity, buffer, out _);
            if (result > 0 && result <= buffer.Capacity)
            {
                return buffer.ToString();
            }
        }
        return null;
    }

    /// <summary>
    /// PATHEXT環境変数から拡張子リストを取得する
    /// </summary>
    static string[] GetPathExtExtensions()
    {
        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrEmpty(pathExt))
        {
            return [".COM", ".EXE", ".BAT", ".CMD"];
        }
        return pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// 空なディレクトリならtrueを返す
    /// </summary>
    /// <param name="path">調べるディレクトリへのパス</param>
    /// <returns>空かどうか</returns>
    /// <exception cref="DirectoryNotFoundException">ディレクトリ見つからなかった例外</exception>
    public static bool IsDirectoryEmpty(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"ディレクトリ '{path}' が存在しません");
        }
        // 全件取得を避け、1件でもあればfalseを返す
        return !Directory.EnumerateFileSystemEntries(path).Any();
    }

    /// <summary>
    /// ファイルとディレクトリを再帰的に列挙
    /// </summary>
    public static string[] GetFileSystemEntries(string path)
    {
        return Directory.GetFileSystemEntries(path, "*", SearchOption.AllDirectories);
    }

    /// <summary>
    /// ファイルの移動。
    /// どうも読み取り専用属性付いてたりすると移動出来ないぽいので…。
    /// </summary>
    /// <param name="sourceName">移動するファイルの名前。</param>
    /// <param name="destName">ファイルの新しいパス。</param>
    public static void MoveFileForce(string sourceName, string destName)
    {
        var src = new FileInfo(sourceName);
        FileAttributes? attributes = null;
        try
        {
            attributes = src.Attributes;
            src.Attributes = FileAttributes.Normal;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        try
        {
            File.Move(sourceName, destName);
        }
        finally
        {
            var dst = new FileInfo(destName);
            if (dst.Exists)
            {
                if (attributes.HasValue)
                {
                    dst.Attributes = attributes.Value;
                }
            }
        }
    }

    /// <summary>
    /// 出来るだけ安全になコピー。属性とかも出来るだけコピる。
    /// </summary>
    /// <param name="sourceName">コピー元</param>
    /// <param name="destName">コピー先</param>
    /// <exception cref="FileChangedOnCopyException"></exception>
    public static void CopyFileSafeWithAttributes(string sourceName, string destName)
    {
        // 属性とかを適当に取得(コピーに時間かかるかもしれないので先に取っておく)
        var src = new FileInfo(sourceName);
        FileAttributes? attributes = null;
        FileSecurity? security = null;
        DateTime? creationTime = null;
        DateTime? lastAccessTime = null;
        DateTime? lastWriteTime = null;
        try { attributes = src.Attributes; } catch (IOException) { } catch (UnauthorizedAccessException) { }
        try { security = src.GetAccessControl(); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        try { creationTime = src.CreationTime; } catch (IOException) { } catch (UnauthorizedAccessException) { }
        try { lastAccessTime = src.LastAccessTime; } catch (IOException) { } catch (UnauthorizedAccessException) { }
        try { lastWriteTime = src.LastWriteTime; } catch (IOException) { } catch (UnauthorizedAccessException) { }

        try
        {
            // コピー
            CopyFileSafe(sourceName, destName);
        }
        finally
        {
            // 属性とかを適当に設定
            var dst = new FileInfo(destName);
            if (dst.Exists)
            {
                if (lastWriteTime.HasValue) try { dst.LastWriteTime = lastWriteTime.Value; } catch (IOException) { } catch (UnauthorizedAccessException) { }
                if (lastAccessTime.HasValue) try { dst.LastAccessTime = lastAccessTime.Value; } catch (IOException) { } catch (UnauthorizedAccessException) { }
                if (creationTime.HasValue) try { dst.CreationTime = creationTime.Value; } catch (IOException) { } catch (UnauthorizedAccessException) { }
                if (security != null) try { dst.SetAccessControl(security); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                if (attributes.HasValue) try { dst.Attributes = attributes.Value; } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
    }

    /// <summary>
    /// 出来るだけ安全になコピー。
    /// </summary>
    /// <param name="sourceName">コピー元</param>
    /// <param name="destName">コピー先</param>
    public static void CopyFileSafe(string sourceName, string destName)
    {
        string destTmpName1 = destName + ".{6F5EC475-CA08-485c-B782-AEC4466FE3E1}.tmp";
        string destTmpName2 = destName + ".{57C24E50-4686-4905-8610-6A19DE5FE906}.tmp";

        try
        {
            // とりあえずテンポラリなファイル名でコピー。
            //File.Copy(sourceName, destFileName, true); // overwrite = true
            CopyFileWithoutLock(sourceName, destTmpName1, true); // overwrite = true
            // コピー先が存在してたらリネーム
            try { MoveFileForce(destName, destTmpName2); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try
            {
                // コピー先へリネーム
                MoveFileForce(destTmpName1, destName);
            }
            finally
            {
                // 元コピー先を削除
                try { DeleteFileForce(destTmpName2); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
        // クリーンアップ後に再throwするため、広くキャッチする必要がある
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            try
            {
                if (File.Exists(destTmpName1))
                {
                    DeleteFileForce(destTmpName2);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    /// <summary>
    /// コピー元/先をロックせずにファイルをコピーします
    /// </summary>
    /// <param name="sourceName">コピーするファイル。</param>
    /// <param name="destFileName">コピー先ファイルの名前。このパラメータには、ディレクトリは指定できません。</param>
    /// <param name="overwrite">コピー先ファイルが上書きできる場合は true。それ以外の場合は false。</param>
    /// <exception cref="IOException">コピー失敗時の例外とか</exception>
    /// <exception cref="FileChangedOnCopyException"></exception>
    public static void CopyFileWithoutLock(string sourceName, string destFileName, bool overwrite)
    {
        // BackupAPIによるコピーを試してみる
        try
        {
            BackupFile.CopyFile(sourceName, destFileName);
            return;
        }
        catch (FileChangedOnCopyException)
        {
            throw;
        }
        catch (IOException e)
        {
            System.Diagnostics.Debug.Fail(e.ToString());
            // エラーったらそのまま続行
        }

        var srcInfo = new FileInfo(sourceName);
        var dstInfo = new FileInfo(destFileName);
        try
        {
            DateTime srcLastWrite = srcInfo.LastWriteTime;
            long srcLength = srcInfo.Length;

            FileShare fs = FileShare.ReadWrite | FileShare.Delete; // ここがキモ
            FileMode dstFM = overwrite ? FileMode.Create : FileMode.CreateNew;
            using (FileStream src = srcInfo.Open(FileMode.Open, FileAccess.Read, fs))
            using (FileStream dst = dstInfo.Open(dstFM, FileAccess.Write, fs))
            {
                byte[] buffer = new byte[65536]; // 最大64kbずつコピる
                while (true)
                {
                    int n = src.Read(buffer, 0, buffer.Length);
                    if (n == 0) break;
                    dst.Write(buffer, 0, n);
                }
            }

            srcInfo.Refresh();
            if (srcInfo.LastWriteTime != srcLastWrite ||
                srcInfo.Length != srcLength)
            {
                dstInfo.Delete();
                throw new FileChangedOnCopyException(srcInfo.FullName);
            }
        }
        // クリーンアップ後に再throwするため、広くキャッチする必要がある
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            try
            {
                dstInfo.Refresh();
                if (dstInfo.Exists)
                {
                    dstInfo.Delete();
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    /// <summary>
    /// 強制的に削除。
    /// ファイルが存在しなくてもエラーにはならない。
    /// </summary>
    /// <param name="path">削除するファイル</param>
    public static void DeleteFileForce(string path)
    {
        DeleteFileForce(new FileInfo(path));
    }

    /// <summary>
    /// 強制的に削除。
    /// ファイルが存在しなくてもエラーにはならない。
    /// </summary>
    /// <param name="info">削除するファイル</param>
    public static void DeleteFileForce(FileInfo info)
    {
        if (info.Exists)
        {
            try
            {
                info.Attributes &= ~(FileAttributes.ReadOnly |
                    FileAttributes.System | FileAttributes.Hidden); // 削除の邪魔になりそうなのは解除しといてみる
            }
            catch (IOException e)
            {
                System.Diagnostics.Debug.Fail(e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                System.Diagnostics.Debug.Fail(e.Message);
            }
            info.Delete();
        }
    }

    /// <summary>
    /// 強制的に削除。
    /// 存在しなくてもエラーにはならない。
    /// </summary>
    /// <param name="path">削除するパス</param>
    /// <param name="recursive">再帰的に削除するかどうか</param>
    public static void DeleteForce(string path, bool recursive)
    {
        if (Directory.Exists(path))
        {
            DeleteDirectoryForce(path, recursive);
        }
        else if (File.Exists(path))
        {
            DeleteFileForce(path);
        }
    }

    /// <summary>
    /// 強制的に削除。
    /// ディレクトリが存在しなくてもエラーにはならない。
    /// </summary>
    /// <param name="path">削除するディレクトリ</param>
    /// <param name="recursive">再帰的に削除するかどうか</param>
    public static void DeleteDirectoryForce(string path, bool recursive)
    {
        DeleteDirectoryForce(new DirectoryInfo(path), recursive);
    }

    /// <summary>
    /// 強制的に削除。
    /// ディレクトリが存在しなくてもエラーにはならない。
    /// </summary>
    /// <param name="info">削除するディレクトリ</param>
    /// <param name="recursive">再帰的に削除するかどうか</param>
    private static void DeleteDirectoryForce(DirectoryInfo info, bool recursive)
    {
        if (info.Exists)
        {
            if (recursive)
            {
                foreach (FileSystemInfo fsi in info.GetFileSystemInfos())
                {
                    DeleteForce(fsi, recursive);
                }
            }
            info.Attributes &= ~(FileAttributes.ReadOnly |
                FileAttributes.System | FileAttributes.Hidden); // 削除の邪魔になりそうなのは解除しといてみる
            info.Delete(false);
        }
    }

    /// <summary>
    /// 強制的に削除。
    /// </summary>
    /// <param name="fsi">削除するファイルまたはディレクトリ</param>
    /// <param name="recursive">再帰的に削除するかどうか</param>
    public static void DeleteForce(FileSystemInfo fsi, bool recursive)
    {
        if (fsi is FileInfo fi)
        {
            DeleteFileForce(fi);
        }
        else if (fsi is DirectoryInfo di)
        {
            DeleteDirectoryForce(di, recursive);
        }
    }

    /// <summary>
    /// 短いファイル名から長いファイル名を取得する
    /// </summary>
    /// <param name="path">大文字小文字が変なファイル名とか短いファイル名とか相対パスとか。</param>
    /// <returns>ちゃんとしたファイル名</returns>
    public static string GetCorrectPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string root = Path.GetPathRoot(fullPath) ?? string.Empty;
        string[] dirs = path[root.Length..].Split(Path.DirectorySeparatorChar);
        string ret = root;
        foreach (string name in dirs)
        {
            var di = new DirectoryInfo(ret);
            if (di.Exists)
            {
                try
                {
                    FileSystemInfo[] fsi = di.GetFileSystemInfos(name);
                    if (fsi.Length == 1)
                    {
                        ret = fsi[0].FullName;
                    }
                    else
                    {
                        ret = Path.Combine(ret, name);
                    }
                }
                catch (IOException e)
                {
                    System.Diagnostics.Debug.Fail(e.Message);
                    ret = Path.Combine(ret, name);
                }
                catch (UnauthorizedAccessException e)
                {
                    System.Diagnostics.Debug.Fail(e.Message);
                    ret = Path.Combine(ret, name);
                }
            }
            else
            {
                ret = Path.Combine(ret, name);
            }
        }
        return ret;
    }

    /// <summary>
    /// ハードリンクの作成
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="existingFileName"></param>
    /// <exception cref="IOException"></exception>
    public static void CreateHardLink(string fileName, string existingFileName)
    {
        try
        {
            if (!Win32.NativeMethods.CreateHardLink(fileName, existingFileName, IntPtr.Zero))
            {
                throw new IOException("ハードリンクの作成に失敗しました");
            }
        }
        catch (EntryPointNotFoundException e)
        {
            throw new IOException("ハードリンクの作成が未対応です。", e);
        }
    }

    /// <summary>
    /// PHPのis_writable()的な。
    /// </summary>
    public static bool IsWritable(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                // テンポラリファイル作って試す。_no
                string tmp = Path.Combine(path, "{6F5EC475-CA08-485c-B782-AEC4466FE3E1}.tmp");
                if (!File.Exists(tmp))
                {
                    File.WriteAllBytes(tmp, []);
                    bool writable = File.Exists(tmp);
                    if (writable)
                    {
                        File.Delete(tmp);
                    }
                    return writable;
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
            return false;
        }
        else if (File.Exists(path))
        {
            try
            {
                // 実際に開いてしまってみる
                using FileStream s = File.OpenWrite(path);
                return s.CanWrite;
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
            return false;
        }
        else
        {
            return false;
        }
    }
}
