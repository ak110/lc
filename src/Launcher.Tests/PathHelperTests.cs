using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// PathHelperのテスト
/// </summary>
public sealed class PathHelperTests
{
    // --- PathNormalize ---

    [Fact]
    public void PathNormalize_スラッシュをバックスラッシュに変換()
    {
        PathHelper.PathNormalize(@"C:/Users/test").Should().Be(@"C:\Users\test");
    }

    [Fact]
    public void PathNormalize_末尾のバックスラッシュを除去()
    {
        PathHelper.PathNormalize(@"C:\Users\test\").Should().Be(@"C:\Users\test");
    }

    [Fact]
    public void PathNormalize_ドライブルートは末尾バックスラッシュを維持()
    {
        PathHelper.PathNormalize(@"C:").Should().Be(@"C:\");
    }

    [Fact]
    public void PathNormalize_環境変数を展開()
    {
        // %SystemRoot% は通常 C:\WINDOWS
        var result = PathHelper.PathNormalize(@"%SystemRoot%\System32");
        result.Should().NotContain("%");
        result.Should().EndWith(@"\System32");
    }

    // --- EqualsPath ---

    [Fact]
    public void EqualsPath_同じパスはtrue()
    {
        PathHelper.EqualsPath(@"C:\Windows", @"C:\Windows").Should().BeTrue();
    }

    [Fact]
    public void EqualsPath_大文字小文字無視()
    {
        PathHelper.EqualsPath(@"C:\WINDOWS", @"C:\windows").Should().BeTrue();
    }

    [Fact]
    public void EqualsPath_スラッシュの違いを吸収()
    {
        PathHelper.EqualsPath(@"C:/Windows", @"C:\Windows").Should().BeTrue();
    }

    [Fact]
    public void EqualsPath_末尾スラッシュの有無を吸収()
    {
        PathHelper.EqualsPath(@"C:\Windows\", @"C:\Windows").Should().BeTrue();
    }

    [Fact]
    public void EqualsPath_異なるパスはfalse()
    {
        PathHelper.EqualsPath(@"C:\Windows", @"C:\Users").Should().BeFalse();
    }

    // --- GetRelativeSubPath ---

    [Fact]
    public void GetRelativeSubPath_サブパスを返す()
    {
        // 実在するパスを使用 (GetFullPathで正規化されるため)
        var baseDir = Path.GetTempPath().TrimEnd('\\');
        var subPath = Path.Combine(baseDir, "sub", "file.txt");
        var result = PathHelper.GetRelativeSubPath(subPath, baseDir);
        result.Should().Be(@"sub\file.txt");
    }

    [Fact]
    public void GetRelativeSubPath_同じディレクトリならドット()
    {
        var baseDir = Path.GetTempPath().TrimEnd('\\');
        var result = PathHelper.GetRelativeSubPath(baseDir, baseDir);
        result.Should().Be(".");
    }

    [Fact]
    public void GetRelativeSubPath_下位でなければIOException()
    {
        var act = () => PathHelper.GetRelativeSubPath(@"D:\other\file.txt", @"C:\base");
        act.Should().Throw<IOException>();
    }

    [Fact]
    public void GetRelativeSubPath_null引数はArgumentNullException()
    {
        var act1 = () => PathHelper.GetRelativeSubPath(null!, @"C:\base");
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => PathHelper.GetRelativeSubPath(@"C:\path", null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetRelativeSubPath_空文字はArgumentNullException()
    {
        var act = () => PathHelper.GetRelativeSubPath("", @"C:\base");
        act.Should().Throw<ArgumentNullException>();
    }

    // --- PathNormalizeWithFullPath ---

    [Fact]
    public void PathNormalizeWithFullPath_空文字は空文字を返す()
    {
        PathHelper.PathNormalizeWithFullPath("").Should().BeEmpty();
    }

    [Fact]
    public void PathNormalizeWithFullPath_nullは空文字を返す()
    {
        PathHelper.PathNormalizeWithFullPath(null!).Should().BeEmpty();
    }
}
