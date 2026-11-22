using System.Security;
using Launcher.Infrastructure;

namespace Launcher.Win32;

public static class FolderEntryEnumerator
{
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
