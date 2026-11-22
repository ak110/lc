using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

public sealed class PathHelperTests
{
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
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
        PathHelper.PathNormalize(@"%SystemRoot%\System32").Should().Be(systemRoot + @"\System32");
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
}
