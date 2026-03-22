using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// FileHelper.ResolveExecutableのテスト
/// </summary>
public sealed class FileHelperTests
{
    [Fact]
    public void ResolveExecutable_絶対パスはそのまま返す()
    {
        string path = @"C:\Windows\System32\notepad.exe";
        FileHelper.ResolveExecutable(path).Should().Be(path);
    }

    [Fact]
    public void ResolveExecutable_相対パスはPATH検索しない()
    {
        string path = @"subdir\notepad.exe";
        FileHelper.ResolveExecutable(path).Should().Be(path);
    }

    [Fact]
    public void ResolveExecutable_スラッシュ含む相対パスもPATH検索しない()
    {
        string path = "subdir/notepad.exe";
        FileHelper.ResolveExecutable(path).Should().Be(path);
    }

    [Fact]
    public void ResolveExecutable_拡張子ありのbare_nameを解決する()
    {
        // notepad.exeはWindowsに必ず存在する
        string result = FileHelper.ResolveExecutable("notepad.exe");
        result.Should().NotBe("notepad.exe");
        Path.IsPathRooted(result).Should().BeTrue();
        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public void ResolveExecutable_拡張子なしのbare_nameをPATHEXTで解決する()
    {
        string result = FileHelper.ResolveExecutable("notepad");
        result.Should().NotBe("notepad");
        Path.IsPathRooted(result).Should().BeTrue();
        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public void ResolveExecutable_存在しない名前はそのまま返す()
    {
        string name = "zzz_nonexistent_command_12345";
        FileHelper.ResolveExecutable(name).Should().Be(name);
    }

    [Fact]
    public void ResolveExecutable_nullは空文字を返す()
    {
        FileHelper.ResolveExecutable(null!).Should().BeEmpty();
    }

    [Fact]
    public void ResolveExecutable_空文字はそのまま返す()
    {
        FileHelper.ResolveExecutable("").Should().BeEmpty();
    }
}
