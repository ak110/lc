using System.Text;
using Microsoft.Win32;

namespace Launcher.Infrastructure;

/// <summary>
/// ファイル・ディレクトリ操作のユーティリティ。
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// コマンド登録パスを正規化し、bare name の場合は PATH 解決する。
    /// <see cref="PathHelper.PathNormalize"/> と <see cref="ResolveExecutable"/> を順に適用する薄いラッパー。
    /// </summary>
    /// <remarks>
    /// アイコン読み込み用の <see cref="ResolveExecutable"/> と同じ解決順を操作系 (「フォルダを開く」や
    /// 参照ダイアログの起点設定など) にも適用し、パス解釈を揃えるために使う。
    /// </remarks>
    /// <param name="path">ファイル名またはパス</param>
    /// <returns>正規化・解決されたパス。null または空文字入力時は空文字を返す</returns>
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

        // SearchPath API による PATH 検索。
        resolved = SearchPathApi(path);
        if (resolved is not null)
        {
            return resolved;
        }

        return path;
    }

    /// <summary>
    /// App Paths レジストリから bare name を検索する。HKCU → HKLM の順。
    /// </summary>
    static string? SearchAppPaths(string name)
    {
        // 名前そのものと、PATHEXT の各拡張子を付けた名前を試す。
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
                    // 引用符を除去し、環境変数を展開する。
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
    /// SearchPath API で PATH 上のファイルを検索する。
    /// </summary>
    static string? SearchPathApi(string name)
    {
        if (Path.HasExtension(name))
        {
            return SearchPathSingle(name, null);
        }

        // 拡張子なしの場合は PATHEXT の各拡張子で試す。
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

    /// <summary>
    /// SearchPath API を1回呼び出す。
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
        // バッファ不足の場合は再試行する。
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
    /// PATHEXT 環境変数から拡張子リストを取得する。
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
    /// パスへの書き込み可否を判定する。ディレクトリは一時ファイル生成で、ファイルは実際に開いて確認する。
    /// </summary>
    public static bool IsWritable(string path)
    {
        if (Directory.Exists(path))
        {
            return IoFailureHandler.IgnoreIoErrors(() =>
            {
                // 一時ファイルを作成して書き込み可否を判定する。
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
                // 実際に書き込み用に開いて確認する。
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
