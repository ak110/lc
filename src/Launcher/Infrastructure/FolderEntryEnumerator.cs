using System.Security;
using Launcher.Win32;

namespace Launcher.Infrastructure;

/// <summary>
/// フォルダ配下のフォルダ・ファイルを列挙するユーティリティ。
/// </summary>
public static class FolderEntryEnumerator
{
    /// <summary>
    /// フォルダ配下のフォルダ・ファイルを列挙する。
    /// フォルダ→ファイルの順に並べ、各ブロックはナチュラルソートで揃える。
    /// 隠しファイル・システムファイルは除外する。
    /// folderPath が存在しない、またはアクセス権限がない場合は空の一覧を返す。
    /// </summary>
    /// <param name="folderPath">列挙対象のフォルダパス</param>
    /// <returns>フォルダ→ファイルの順に並んだ一覧</returns>
    public static IReadOnlyList<FolderEntry> Enumerate(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        var directories = new List<FolderEntry>();
        var files = new List<FolderEntry>();
        try
        {
            foreach (var info in new DirectoryInfo(folderPath).EnumerateFileSystemInfos())
            {
                if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                {
                    continue;
                }
                bool isDirectory = (info.Attributes & FileAttributes.Directory) != 0;
                var entry = new FolderEntry(info.FullName, info.Name, isDirectory);
                (isDirectory ? directories : files).Add(entry);
            }
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (SecurityException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        directories.Sort((a, b) => NaturalStringComparer.Instance.Compare(a.DisplayName, b.DisplayName));
        files.Sort((a, b) => NaturalStringComparer.Instance.Compare(a.DisplayName, b.DisplayName));

        var result = new List<FolderEntry>(directories.Count + files.Count);
        result.AddRange(directories);
        result.AddRange(files);
        return result;
    }
}
