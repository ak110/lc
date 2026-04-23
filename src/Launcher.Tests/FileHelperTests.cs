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

    [Fact]
    public void ResolveCommandPath_bare_nameを解決してフルパスを返す()
    {
        string result = FileHelper.ResolveCommandPath("notepad.exe");
        result.Should().NotBe("notepad.exe");
        Path.IsPathRooted(result).Should().BeTrue();
        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public void ResolveCommandPath_絶対パスは正規化して返す()
    {
        string path = @"C:\Windows\System32\notepad.exe";
        FileHelper.ResolveCommandPath(path).Should().Be(path);
    }

    [Fact]
    public void ResolveCommandPath_環境変数を展開してPATH解決と矛盾しない結果を返す()
    {
        string result = FileHelper.ResolveCommandPath(@"%WINDIR%\System32\notepad.exe");
        // 環境変数が展開されてルート化されていること。
        Path.IsPathRooted(result).Should().BeTrue();
        result.Should().NotContain("%");
        File.Exists(result).Should().BeTrue();
    }

    [Fact]
    public void ResolveCommandPath_スラッシュ区切りは区切り文字を正規化する()
    {
        // PathNormalize が / を \ に置換する挙動を経由していることを確認する。
        string result = FileHelper.ResolveCommandPath(@"C:\Windows/System32/notepad.exe");
        result.Should().Be(@"C:\Windows\System32\notepad.exe");
    }

    [Fact]
    public void ResolveCommandPath_nullは空文字を返す()
    {
        FileHelper.ResolveCommandPath(null).Should().BeEmpty();
    }

    [Fact]
    public void ResolveCommandPath_空文字は空文字を返す()
    {
        FileHelper.ResolveCommandPath("").Should().BeEmpty();
    }

    [Fact]
    public void ResolveCommandPath_存在しない名前はPathNormalize結果を返す()
    {
        string name = "zzz_nonexistent_command_12345";
        FileHelper.ResolveCommandPath(name).Should().Be(name);
    }
}
