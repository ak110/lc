using FluentAssertions;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// ErrorReporterクラスのテスト
/// </summary>
public sealed class ErrorReporterTests
{
    [Fact]
    public void GetDetailMessage_単一例外の情報を含む()
    {
        var ex = new InvalidOperationException("テストエラー");

        string result = ErrorReporter.GetDetailMessage(ex);

        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("テストエラー");
    }

    [Fact]
    public void GetDetailMessage_InnerExceptionの情報を含む()
    {
        var inner = new ArgumentException("内部エラー");
        var ex = new InvalidOperationException("外部エラー", inner);

        string result = ErrorReporter.GetDetailMessage(ex);

        result.Should().Contain("InnerException ->");
        result.Should().Contain("ArgumentException");
        result.Should().Contain("内部エラー");
        result.Should().Contain("<- InnerException");
    }

    [Fact]
    public void GetDetailMessage_BaseExceptionがInnerExceptionと異なる場合に両方出力する()
    {
        // 3段ネストでBaseException != InnerExceptionを生成する
        var root = new ArgumentException("根本原因");
        var middle = new IOException("中間エラー", root);
        var outer = new InvalidOperationException("外部エラー", middle);

        // InnerException = middle, BaseException = root
        outer.InnerException.Should().Be(middle);
        outer.GetBaseException().Should().Be(root);

        string result = ErrorReporter.GetDetailMessage(outer);

        // InnerExceptionセクション
        result.Should().Contain("InnerException ->");
        result.Should().Contain("IOException");
        result.Should().Contain("中間エラー");

        // BaseExceptionセクション (修正前はここがInnerExceptionの情報になっていた)
        result.Should().Contain("BaseException ->");
        result.Should().Contain("ArgumentException");
        result.Should().Contain("根本原因");
        result.Should().Contain("<- BaseException");
    }

    [Fact]
    public void GetDetailMessage_InnerExceptionがない場合はBaseExceptionセクションも出ない()
    {
        var ex = new InvalidOperationException("単独エラー");

        string result = ErrorReporter.GetDetailMessage(ex);

        result.Should().NotContain("InnerException");
        result.Should().NotContain("BaseException");
    }

    [Fact]
    public void GetDetailMessage_BaseExceptionが自分自身の場合はBaseExceptionセクションを出力しない()
    {
        // InnerExceptionが1段のみ → BaseException == InnerException
        var inner = new ArgumentException("内部エラー");
        var ex = new InvalidOperationException("外部エラー", inner);

        ex.GetBaseException().Should().Be(inner);
        ex.InnerException.Should().Be(inner);

        string result = ErrorReporter.GetDetailMessage(ex);

        // InnerExceptionはあるがBaseExceptionは重複するので出ない
        result.Should().Contain("InnerException ->");
        result.Should().NotContain("BaseException");
    }

    [Fact]
    public void 報告画面_続行してよいかと報告先を表示し詳細欄やボタンと重ならない() => UiAcceptance.Run(() =>
    {
        using var form = new ErrorReporterForm(new InvalidOperationException("想定外の失敗")) { Opacity = 0 };
        form.Show();
        try
        {
            var guidance = form.Controls.OfType<Label>().Single(label => label.Name == "guidanceLabel");
            guidance.Visible.Should().BeTrue();
            guidance.Text.Should().Contain("続行すると").And.Contain("「終了」または「再起動」")
                .And.Contain("https://github.com/ak110/lc/issues").And.Contain("logs");
            var buttons = form.Controls.OfType<Button>().Where(button => button.Visible).ToList();
            buttons.Should().NotBeEmpty();
            foreach (var button in buttons)
                guidance.Bounds.IntersectsWith(button.Bounds).Should().BeFalse($"{button.Text}と案内が重ならない");
            form.ClientRectangle.Contains(guidance.Bounds).Should().BeTrue();
        }
        finally
        {
            form.Close();
        }
    });
}
