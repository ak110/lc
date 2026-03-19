namespace Launcher.Infrastructure;

/// <summary>
/// パス文字列操作のユーティリティ
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
        // 念のため正規化
        path = PathHelper.PathNormalizeWithFullPath(path);
        baseDir = PathHelper.PathNormalizeWithFullPath(baseDir);
        // 同一ディレクトリ判定（baseDirに\を付加する前に行う）
        if (string.Equals(path, baseDir, StringComparison.OrdinalIgnoreCase))
        {
            return ".";
        }
        // baseDirの終端を \\ に統一（startsWith判定用）
        if (baseDir[baseDir.Length - 1] != Path.DirectorySeparatorChar)
        {
            baseDir += Path.DirectorySeparatorChar.ToString();
        }
        // pathがbaseDirの下位ではない場合
        if (!path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"{path} の相対パスの生成に失敗しました。");
        }
        return path[baseDir.Length..];
    }

    /// <summary>
    /// パスの正規化を行う。
    /// </summary>
    /// <remarks>
    /// 環境変数の展開、/ -> \\の置換、末尾に\\があれば削除(C:\ とか以外)。
    /// 必要があれば、FileHelper.GetCorrectPath()やPath.GetFullPath()も通すべし。
    /// </remarks>
    /// <param name="path">パスな文字列</param>
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
    /// <param name="path">パスな文字列</param>
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
    /// 二つのパスが等しいっぽければtrue
    /// </summary>
    public static bool EqualsPath(string path1, string path2)
    {
        return string.Equals(
            PathNormalize(path1), PathNormalize(path2), StringComparison.OrdinalIgnoreCase);
    }
}
