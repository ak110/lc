namespace Launcher.Infrastructure;

/// <summary>
/// パス文字列操作のユーティリティ。
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// 親ディレクトリへ遡らない相対パスを取得する。
    /// </summary>
    /// <remarks>
    /// path と baseDir が同じディレクトリを指す場合は "." を返す。
    /// </remarks>
    /// <param name="path">求めるパス。baseDir 以下のパスである必要がある。</param>
    /// <param name="baseDir">基準ディレクトリ</param>
    /// <exception cref="IOException">path が baseDir 以下のパスではない場合</exception>
    public static string GetRelativeSubPath(string path, string baseDir)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }
        if (string.IsNullOrEmpty(baseDir))
        {
            throw new ArgumentNullException(nameof(baseDir));
        }
        // 安全のため正規化する。
        path = PathHelper.PathNormalizeWithFullPath(path);
        baseDir = PathHelper.PathNormalizeWithFullPath(baseDir);
        // 同一ディレクトリ判定 (baseDir に \ を付加する前に行う)。
        if (string.Equals(path, baseDir, StringComparison.OrdinalIgnoreCase))
        {
            return ".";
        }
        // StartsWith 判定のため baseDir の終端を \ に統一する。
        if (baseDir[baseDir.Length - 1] != Path.DirectorySeparatorChar)
        {
            baseDir += Path.DirectorySeparatorChar.ToString();
        }
        // path が baseDir の下位ではない場合。
        if (!path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"{path} の相対パスの生成に失敗した");
        }
        return path[baseDir.Length..];
    }

    /// <summary>
    /// パスを正規化する。
    /// </summary>
    /// <remarks>
    /// 環境変数の展開、/ から \ への置換、末尾の \ の削除 (C:\ などは除く) を行う。
    /// 必要に応じて FileHelper.GetCorrectPath() や Path.GetFullPath() も併用する。
    /// </remarks>
    /// <param name="path">パス文字列</param>
    /// <returns>正規化されたパス</returns>
    public static string PathNormalize(string path)
    {
        path = Environment.ExpandEnvironmentVariables(path)
            .Normalize()
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);
        if (path.Length == 2 && path[1] == Path.VolumeSeparatorChar)
        {
            path = $"{path}{Path.DirectorySeparatorChar}";
        }
        return path;
    }
    /// <summary>
    /// パスの正規化とフルパス化を行う。
    /// </summary>
    /// <param name="path">パス文字列</param>
    /// <returns>正規化されたパス</returns>
    public static string PathNormalizeWithFullPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "";
        }
        return Path.GetFullPath(PathNormalize(path));
    }

    /// <summary>
    /// 2つのパスが等価とみなせる場合に true を返す。
    /// </summary>
    public static bool EqualsPath(string path1, string path2)
    {
        return string.Equals(
            PathNormalize(path1), PathNormalize(path2), StringComparison.OrdinalIgnoreCase);
    }
}
