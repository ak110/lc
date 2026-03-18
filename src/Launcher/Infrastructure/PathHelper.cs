#nullable disable
using System.Security.AccessControl;
using System.Runtime.InteropServices;

namespace Launcher.Infrastructure
{
    /// <summary>
    /// IO関連のユーティリティ
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// 駆け上がりを行わない相対パスの取得。
        /// </summary>
        /// <remarks>
        /// pathとbaseDirが同じディレクトリを指していた場合は "." が返る。
        /// </remarks>
        /// <param name="path">求めるパス。baseDir以下へのパスである必要がある</param>
        /// <param name="baseDir">基準ディレクトリ</param>
        /// <exception cref="IOException">pathがbaseDir以下へのパスではない場合</exception>
        public static string GetRelativeSubPath(string path, string baseDir) {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException("path");
            }
            if (string.IsNullOrEmpty(baseDir)) {
                throw new ArgumentNullException("baseDir");
            }
            // 念のため正規化
            path = PathHelper.PathNormalizeWithFullPath(path);
            baseDir = PathHelper.PathNormalizeWithFullPath(baseDir);
            // baseDirの終端を \\ に統一
            if (baseDir[baseDir.Length - 1] != Path.DirectorySeparatorChar) {
                baseDir += Path.DirectorySeparatorChar.ToString();
            }
            string pathLc = path.ToLower();
            string baseDirLc = baseDir.ToLower();
            if (pathLc == baseDirLc) {
                return "."; // "" ？
            }
            // pathがbaseDirの下位ではない場合
            if (!pathLc.StartsWith(baseDirLc)) {
                throw new IOException(path + " の相対パスの生成に失敗しました。");
            }
            return path.Substring(baseDir.Length);
        }

        /// <summary>
        /// パスの正規化を行う。
        /// </summary>
        /// <remarks>
        /// 環境変数の展開、/ -> \\の置換、末尾に\\があれば削除(C:\ とか以外)。
        /// 必要があれば、GetCorrectPath()やPath.GetFullPath()も通すべし。
        /// </remarks>
        /// <param name="path">パスな文字列</param>
        /// <returns>正規化されたパス</returns>
        public static string PathNormalize(string path) {
            path = Environment.ExpandEnvironmentVariables(path)
                .Normalize()
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar);
            if (path.Length == 2 && path[1] == Path.VolumeSeparatorChar) {
                path = path + Path.DirectorySeparatorChar;
            }
            return path;
        }
        /// <summary>
        /// パスの正規化とフルパス化を行う。
        /// </summary>
        /// <param name="path">パスな文字列</param>
        /// <returns>正規化されたパス</returns>
        public static string PathNormalizeWithFullPath(string path) {
            if (string.IsNullOrEmpty(path)) {
                return "";
            }
            return Path.GetFullPath(PathNormalize(path));
        }

        /// <summary>
        /// 二つのパスが等しいっぽければtrue
        /// </summary>
        public static bool EqualsPath(string path1, string path2) {
            return string.Compare(
                PathNormalize(path1), PathNormalize(path2), true) == 0;
        }

        /// <summary>
        /// @"\\?\" を先頭にくっつける。
        /// </summary>
        /// <param name="path">パスな文字列</param>
        /// <returns>変換後のパス</returns>
        public static string StrictPath(string path) {
            if (2 < path.Length) {
                if (path[2] == Path.DirectorySeparatorChar) {
                    if (path[1] == Path.VolumeSeparatorChar) {
                        return @"\\?\" + path;
                    }
                } else if (path[2] != '?' && path.StartsWith(@"\\")) {
                    return @"¥¥?¥UNC¥" + path.Substring(2);
                }
            }
            return path;
        }

        /// <summary>
        /// 空なディレクトリならtrueを返す
        /// </summary>
        /// <param name="path">調べるディレクトリへのパス</param>
        /// <returns>空かどうか</returns>
        /// <exception cref="DirectoryNotFoundException">ディレクトリ見つからなかった例外</exception>
        public static bool IsDirectoryEmpty(string path) {
            if (!Directory.Exists(path)) {
                throw new DirectoryNotFoundException("ディレクトリ '" + path + "' が存在しません");
            }
            // ↓もっとちゃんとした調べ方あるかもしれんけどとりあえず適当実装
            string[] entries = Directory.GetFileSystemEntries(path);
            return entries.Length <= 0;
        }

        /// <summary>
        /// ファイルとディレクトリを列挙(SearchOption.AllDirectories版が無いようなので…。)
        /// </summary>
        public static string[] GetFileSystemEntries(string path) {
            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            string[] entries = new string[dirs.Length + files.Length];
            Array.Copy(dirs, entries, dirs.Length);
            Array.Copy(files, 0, entries, dirs.Length, files.Length);
            return entries;
        }

        /// <summary>
        /// ファイルの移動。
        /// どうも読み取り専用属性付いてたりすると移動出来ないぽいので…。
        /// </summary>
        /// <param name="sourceName">移動するファイルの名前。</param>
        /// <param name="destName">ファイルの新しいパス。</param>
        public static void MoveFileForce(string sourceName, string destName) {
            FileInfo src = new FileInfo(sourceName);
            FileAttributes? attributes = null;
            try {
                attributes = src.Attributes;
                src.Attributes = FileAttributes.Normal;
            } catch { }

            try {
                File.Move(sourceName, destName);
            } finally {
                FileInfo dst = new FileInfo(destName);
                if (dst.Exists) {
                    if (attributes.HasValue) {
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
        public static void CopyFileSafeWithAttributes(string sourceName, string destName) {
            // 属性とかを適当に取得(コピーに時間かかるかもしれないので先に取っておく)
            FileInfo src = new FileInfo(sourceName);
            FileAttributes? attributes = null;
            FileSecurity security = null;
            DateTime? creationTime = null;
            DateTime? lastAccessTime = null;
            DateTime? lastWriteTime = null;
            try { attributes = src.Attributes; } catch { }
            try { security = src.GetAccessControl(); } catch { }
            try { creationTime = src.CreationTime; } catch { }
            try { lastAccessTime = src.LastAccessTime; } catch { }
            try { lastWriteTime = src.LastWriteTime; } catch { }

            try {
                // コピー
                CopyFileSafe(sourceName, destName);
            } finally {
                // 属性とかを適当に設定
                FileInfo dst = new FileInfo(destName);
                if (dst.Exists) {
                    if (lastWriteTime.HasValue) try { dst.LastWriteTime = lastWriteTime.Value; } catch { }
                    if (lastAccessTime.HasValue) try { dst.LastAccessTime = lastAccessTime.Value; } catch { }
                    if (creationTime.HasValue) try { dst.CreationTime = creationTime.Value; } catch { }
                    if (security != null) try { dst.SetAccessControl(security); } catch { }
                    if (attributes.HasValue) try { dst.Attributes = attributes.Value; } catch { }
                }
            }
        }

        /// <summary>
        /// 出来るだけ安全になコピー。
        /// </summary>
        /// <param name="sourceName">コピー元</param>
        /// <param name="destName">コピー先</param>
        public static void CopyFileSafe(string sourceName, string destName) {
            string destTmpName1 = destName + ".{6F5EC475-CA08-485c-B782-AEC4466FE3E1}.tmp";
            string destTmpName2 = destName + ".{57C24E50-4686-4905-8610-6A19DE5FE906}.tmp";

            try {
                // とりあえずテンポラリなファイル名でコピー。
                //File.Copy(sourceName, destFileName, true); // overwrite = true
                CopyFileWithoutLock(sourceName, destTmpName1, true); // overwrite = true
                // コピー先が存在してたらリネーム
                try { MoveFileForce(destName, destTmpName2); } catch { }
                try {
                    // コピー先へリネーム
                    MoveFileForce(destTmpName1, destName);
                } finally {
                    // 元コピー先を削除
                    try { DeleteFileForce(destTmpName2); } catch { }
                }
            } catch {
                try {
                    if (File.Exists(destTmpName1)) {
                        DeleteFileForce(destTmpName2);
                    }
                } catch { }
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
        public static void CopyFileWithoutLock(string sourceName, string destFileName, bool overwrite) {
            // BackupAPIによるコピーを試してみる
            try {
                BackupFile.CopyFile(sourceName, destFileName);
                return;
            } catch (FileChangedOnCopyException) {
                throw;
            } catch (IOException e) {
                System.Diagnostics.Debug.Fail(e.ToString());
                // エラーったらそのまま続行
            }

            FileInfo srcInfo = new FileInfo(sourceName);
            FileInfo dstInfo = new FileInfo(destFileName);
            try {
                DateTime srcLastWrite = srcInfo.LastWriteTime;
                long srcLength = srcInfo.Length;

                FileShare fs = FileShare.ReadWrite | FileShare.Delete; // ここがキモ
                FileMode dstFM = overwrite ? FileMode.Create : FileMode.CreateNew;
                using (FileStream src = srcInfo.Open(FileMode.Open, FileAccess.Read, fs))
                using (FileStream dst = dstInfo.Open(dstFM, FileAccess.Write, fs)) {
                    byte[] buffer = new byte[65536]; // 最大64kbずつコピる
                    while (true) {
                        int n = src.Read(buffer, 0, buffer.Length);
                        if (n == 0) break;
                        dst.Write(buffer, 0, n);
                    }
                    // ↑この辺途中で他から書き換えられたりするとどうなるんだろ？ 無視？(´ω`)
                }

                srcInfo.Refresh();
                if (srcInfo.LastWriteTime != srcLastWrite ||
                    srcInfo.Length != srcLength) {
                    // ↑どーも頼りない検出方法だが。。
                    dstInfo.Delete();
                    throw new FileChangedOnCopyException(srcInfo.FullName);
                }
            } catch {
                try {
                    dstInfo.Refresh();
                    if (dstInfo.Exists) {
                        dstInfo.Delete();
                    }
                } catch {
                }
                throw;
            }
        }

        /// <summary>
        /// 強制的に削除。
        /// ファイルが存在しなくてもエラーにはならない。
        /// </summary>
        /// <param name="path">削除するファイル</param>
        public static void DeleteFileForce(string path) {
            DeleteFileForce(new FileInfo(path));
        }

        /// <summary>
        /// 強制的に削除。
        /// ファイルが存在しなくてもエラーにはならない。
        /// </summary>
        /// <param name="info">削除するファイル</param>
        public static void DeleteFileForce(FileInfo info) {
            if (info.Exists) {
                try {
                    info.Attributes &= ~(FileAttributes.ReadOnly |
                        FileAttributes.System | FileAttributes.Hidden); // 削除の邪魔になりそうなのは解除しといてみる
                } catch (Exception e) {
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
        public static void DeleteForce(string path, bool recursive) {
            if (Directory.Exists(path)) {
                DeleteDirectoryForce(path, recursive);
            } else if (File.Exists(path)) {
                DeleteFileForce(path);
            }
        }

        /// <summary>
        /// 強制的に削除。
        /// ディレクトリが存在しなくてもエラーにはならない。
        /// </summary>
        /// <param name="path">削除するディレクトリ</param>
        /// <param name="recursive">再帰的に削除するかどうか</param>
        public static void DeleteDirectoryForce(string path, bool recursive) {
            DeleteDirectoryForce(new DirectoryInfo(path), recursive);
        }

        /// <summary>
        /// 強制的に削除。
        /// ディレクトリが存在しなくてもエラーにはならない。
        /// </summary>
        /// <param name="info">削除するディレクトリ</param>
        /// <param name="recursive">再帰的に削除するかどうか</param>
        private static void DeleteDirectoryForce(DirectoryInfo info, bool recursive) {
            if (info.Exists) {
                if (recursive) {
                    foreach (FileSystemInfo fsi in info.GetFileSystemInfos()) {
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
        public static void DeleteForce(FileSystemInfo fsi, bool recursive) {
            FileInfo fi = fsi as FileInfo;
            if (fi != null) {
                DeleteFileForce(fi);
            } else {
                DirectoryInfo di = fsi as DirectoryInfo;
                if (di != null) {
                    DeleteDirectoryForce(di, recursive);
                } else {
                    // (´ω`)？
                }
            }
        }

        /// <summary>
        /// 短いファイル名から長いファイル名を取得する
        /// </summary>
        /// <param name="path">大文字小文字が変なファイル名とか短いファイル名とか相対パスとか。</param>
        /// <returns>ちゃんとしたファイル名</returns>
        public static string GetCorrectPath(string path) {
            string fullPath = Path.GetFullPath(path); // ←これだけでいい気もする。。
            string root = Path.GetPathRoot(fullPath);
            string[] dirs = path.Substring(root.Length).Split(Path.DirectorySeparatorChar);
            string ret = root;
            foreach (string name in dirs) {
                DirectoryInfo di = new DirectoryInfo(ret);
                if (di.Exists) {
                    try {
                        FileSystemInfo[] fsi = di.GetFileSystemInfos(name);
                        if (fsi.Length == 1) {
                            ret = fsi[0].FullName;
                        } else {
                            ret = Path.Combine(ret, name);
                        }
                    } catch (Exception e) {
                        System.Diagnostics.Debug.Fail(e.Message);
                        ret = Path.Combine(ret, name);
                    }
                } else {
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
        public static void CreateHardLink(string fileName, string existingFileName) {
            try {
                if (!CreateHardLink(fileName, existingFileName, IntPtr.Zero)) {
                    throw new IOException("ハードリンクの作成に失敗しました");
                }
            } catch (EntryPointNotFoundException e) {
                throw new IOException("ハードリンクの作成が未対応です。", e);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        /// <summary>
        /// PHPのis_readable()的な。
        /// 適当実装。
        /// </summary>
        public static bool IsReadable(string path) {
            if (Directory.Exists(path)) {
                System.Diagnostics.Debug.Fail("未実装！");
                throw new NotImplementedException();
            } else if (File.Exists(path)) {
                try {
                    // 実際に開いてしまってみる
                    using (FileStream s = File.OpenRead(path)) {
                        return s.CanRead;
                    }
                } catch (UnauthorizedAccessException) {
                } catch (IOException) {
                }
                return false;
            } else {
                return false;
            }
        }

        /// <summary>
        /// PHPのis_writable()的な。
        /// 適当実装。
        /// </summary>
        public static bool IsWritable(string path) {
            if (Directory.Exists(path)) {
                try {
                    // テンポラリファイル作って試す。_no
                    string tmp = Path.Combine(path, "{6F5EC475-CA08-485c-B782-AEC4466FE3E1}.tmp");
                    if (!File.Exists(tmp)) {
                        File.WriteAllBytes(tmp, new byte[0]);
                        bool writable = File.Exists(tmp);
                        if (writable) {
                            File.Delete(tmp);
                        }
                        return writable;
                    }
                } catch (UnauthorizedAccessException) {
                } catch (IOException) {
                }
                return false;
            } else if (File.Exists(path)) {
                try {
                    // 実際に開いてしまってみる
                    using (FileStream s = File.OpenWrite(path)) {
                        return s.CanWrite;
                    }
                } catch (UnauthorizedAccessException) {
                } catch (IOException) {
                }
                return false;
            } else {
                return false;
            }
        }

        /// <summary>
        /// フルパスならtrue。
        /// </summary>
        public static bool IsFullPath(string fileName) {
            return 2 < fileName.Length &&
                char.IsLower(fileName[0]) &&
                fileName[1] == ':' &&
                File.Exists(fileName) ||
                Directory.Exists(fileName);
        }
    }
}
