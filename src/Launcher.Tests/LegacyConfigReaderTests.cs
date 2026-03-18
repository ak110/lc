using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

public class LegacyConfigReaderTests {
    private static LegacyConfigReader CreateReader(string content) {
        return new LegacyConfigReader(new StringReader(content), true);
    }

    [Fact]
    public void 基本的なキーバリューを読み込める() {
        var reader = CreateReader("key = value");
        reader.ContainsKey("key").Should().BeTrue();
        reader.Indirect("key").Should().Be("value");
    }

    [Fact]
    public void 複数行を読み込める() {
        var reader = CreateReader("a = 1\nb = 2\nc = hello");
        reader.Indirect("a").Should().Be("1");
        reader.Indirect("b").Should().Be("2");
        reader.Indirect("c").Should().Be("hello");
    }

    [Fact]
    public void Bool値を正しくパースする() {
        var reader = CreateReader("flag1 = True\nflag2 = False");
        reader.Bool("flag1").Should().BeTrue();
        reader.Bool("flag2").Should().BeFalse();
    }

    [Fact]
    public void 数値を正しくパースする() {
        var reader = CreateReader("count = 42");
        reader.Num("count").Should().Be(42);
    }

    [Fact]
    public void 浮動小数点を正しくパースする() {
        var reader = CreateReader("rate = 3.14");
        reader.Float("rate").Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void エスケープシーケンスを正しく処理する() {
        // \n → 改行, \r → CR, \\ → バックスラッシュ
        var reader = CreateReader(@"text = hello\nworld");
        reader.EscapedString("text").Should().Be("hello\nworld");
    }

    [Fact]
    public void バックスラッシュのエスケープ() {
        var reader = CreateReader(@"path = C:\\Windows\\System32");
        reader.EscapedString("path").Should().Be(@"C:\Windows\System32");
    }

    [Fact]
    public void 存在しないキーはContainsKeyでfalse() {
        var reader = CreateReader("a = 1");
        reader.ContainsKey("b").Should().BeFalse();
    }

    [Fact]
    public void Keys一覧を取得できる() {
        var reader = CreateReader("x = 1\ny = 2");
        reader.Keys.Should().Contain("x").And.Contain("y");
    }

    [Fact]
    public void 値にイコール記号を含む場合() {
        // "key = a = b" → key="a = b"
        var reader = CreateReader("key = a = b");
        reader.Indirect("key").Should().Be("a = b");
    }

    [Fact]
    public void 空行は無視される() {
        var reader = CreateReader("a = 1\n\nb = 2");
        reader.Indirect("a").Should().Be("1");
        reader.Indirect("b").Should().Be("2");
    }
}
