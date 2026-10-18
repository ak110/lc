namespace Launcher.Infrastructure;

/// <summary>
/// パス文字列操作のユーティリティ。
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// パスを正規化する。
    /// </summary>
    /// <remarks>
    /// 環境変数の展開、/ から \ への置換、末尾の \ の削除 (C:\ などは除く) を行う。
    /// 必要に応じて Path.GetFullPath() も併用する。
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
    /// 2つのパスが等価とみなせる場合に true を返す。
    /// </summary>
    public static bool EqualsPath(string path1, string path2)
    {
        return string.Equals(
            PathNormalize(path1), PathNormalize(path2), StringComparison.OrdinalIgnoreCase);
    }
}
