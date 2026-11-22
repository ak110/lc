namespace Launcher.Infrastructure;

public static class PathHelper
{
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
    public static bool EqualsPath(string path1, string path2)
    {
        return string.Equals(
            PathNormalize(path1), PathNormalize(path2), StringComparison.OrdinalIgnoreCase);
    }
}
