using System.Text;
using Launcher.Infrastructure;
using Microsoft.Win32;

namespace Launcher.Win32;

public static class FileHelper
{
    public static string ResolveCommandPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }
        return ResolveExecutable(PathHelper.PathNormalize(path));
    }

    /// <summary>
    /// bare name (パス区切りを含まない名前) をアイコン取得用にパス解決する。
    /// ShellExecuteEx の解決順 (App Paths → PATH 検索) に合わせる。
    /// </summary>
    /// <param name="path">ファイル名またはパス</param>
    /// <returns>解決されたフルパス、または元の path</returns>
    public static string ResolveExecutable(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path ?? string.Empty;
        }

        // パス区切りを含む場合、またはシェル特殊パス ("::{CLSID}" や "shell:xxx") は解決不要。
        if (path.Contains('\\') || path.Contains('/')
            || path.StartsWith("::", StringComparison.Ordinal)
            || path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        // App Paths レジストリ検索 (ShellExecuteEx と同じく最優先)。
        string? resolved = SearchAppPaths(path);
        if (resolved is not null)
        {
            return resolved;
        }

        resolved = SearchPathApi(path);
        if (resolved is not null)
        {
            return resolved;
        }

        return path;
    }

    static string? SearchAppPaths(string name)
    {
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

    static string? SearchPathApi(string name)
    {
        if (Path.HasExtension(name))
        {
            return SearchPathSingle(name, null);
        }

        foreach (string ext in GetPathExtExtensions())
        {
            string? found = SearchPathSingle(name, ext);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    static string? SearchPathSingle(string fileName, string? extension)
    {
        var buffer = new StringBuilder(260);
        int result = NativeMethods.SearchPath(
            null, fileName, extension, buffer.Capacity, buffer, out _);
        if (result > 0 && result <= buffer.Capacity)
        {
            return buffer.ToString();
        }
        if (result > buffer.Capacity)
        {
            buffer.EnsureCapacity(result);
            result = NativeMethods.SearchPath(
                null, fileName, extension, buffer.Capacity, buffer, out _);
            if (result > 0 && result <= buffer.Capacity)
            {
                return buffer.ToString();
            }
        }
        return null;
    }

    static string[] GetPathExtExtensions()
    {
        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrEmpty(pathExt))
        {
            return [".COM", ".EXE", ".BAT", ".CMD"];
        }
        return pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool IsWritable(string path)
    {
        if (Directory.Exists(path))
        {
            return IoFailureHandler.IgnoreIoErrors(() =>
            {
                string tmp = Path.Combine(path, "{6F5EC475-CA08-485c-B782-AEC4466FE3E1}.tmp");
                if (File.Exists(tmp))
                {
                    return false;
                }
                File.WriteAllBytes(tmp, []);
                bool writable = File.Exists(tmp);
                if (writable)
                {
                    File.Delete(tmp);
                }
                return writable;
            }, false);
        }
        else if (File.Exists(path))
        {
            return IoFailureHandler.IgnoreIoErrors(() =>
            {
                using FileStream s = File.OpenWrite(path);
                return s.CanWrite;
            }, false);
        }
        else
        {
            return false;
        }
    }
}
