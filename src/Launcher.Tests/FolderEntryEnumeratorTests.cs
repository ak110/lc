using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// FolderEntryEnumeratorのテスト
/// </summary>
public sealed class FolderEntryEnumeratorTests
{
    [Fact]
    public void Enumerate_存在しないパスは空を返す()
    {
        FolderEntryEnumerator.Enumerate(@"Z:\definitely\not\existing").Should().BeEmpty();
    }

    [Fact]
    public void Enumerate_フォルダを先頭にナチュラルソートで並べる()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub10"));
            Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub2"));
            File.WriteAllText(Path.Combine(tempDir.FullName, "a10.txt"), "");
            File.WriteAllText(Path.Combine(tempDir.FullName, "a2.txt"), "");

            var names = FolderEntryEnumerator.Enumerate(tempDir.FullName)
                .Select(e => e.DisplayName).ToArray();

            names.Should().Equal("sub2", "sub10", "a2.txt", "a10.txt");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Enumerate_隠しファイルとシステムファイルを除外する()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var hidden = Path.Combine(tempDir.FullName, "hidden.txt");
            File.WriteAllText(hidden, "");
            File.SetAttributes(hidden, FileAttributes.Hidden);

            var system = Path.Combine(tempDir.FullName, "system.txt");
            File.WriteAllText(system, "");
            File.SetAttributes(system, FileAttributes.System);

            File.WriteAllText(Path.Combine(tempDir.FullName, "visible.txt"), "");

            var names = FolderEntryEnumerator.Enumerate(tempDir.FullName)
                .Select(e => e.DisplayName).ToArray();

            names.Should().Equal("visible.txt");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Enumerate_空フォルダは空を返す()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            FolderEntryEnumerator.Enumerate(tempDir.FullName).Should().BeEmpty();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
