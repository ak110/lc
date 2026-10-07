using System.Runtime.ExceptionServices;
using FluentAssertions;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// FindReplaceDialogのレイアウトとタブオーダーを検証する。
/// </summary>
public sealed class FindReplaceDialogTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShowOrActivate_表示中のコントロールが重ならず画面内に収まる(bool replaceMode) =>
        RunInSta(() =>
        {
            using var owner = new Form();
            using var dialog = new FindReplaceDialog { Opacity = 0 };
            dialog.ShowOrActivate(owner, replaceMode, null);

            var visible = dialog.Controls.Cast<Control>().Where(c => c.Visible).ToList();
            foreach (var control in visible)
            {
                dialog.ClientRectangle.Contains(control.Bounds).Should().BeTrue(
                    $"{control.Text} ({control.Bounds}) がクライアント領域 {dialog.ClientRectangle} の外側に位置している");
            }
            for (int i = 0; i < visible.Count; i++)
            {
                for (int j = i + 1; j < visible.Count; j++)
                {
                    Rectangle.Intersect(visible[i].Bounds, visible[j].Bounds).IsEmpty.Should().BeTrue(
                        $"{visible[i].Text} {visible[i].Bounds} と {visible[j].Text} {visible[j].Bounds} が重なっている");
                }
            }
        });

    [Fact]
    public void ShowOrActivate_置換モードでは検索文字列の次に置換後へタブ移動する() =>
        RunInSta(() =>
        {
            using var owner = new Form();
            using var dialog = new FindReplaceDialog { Opacity = 0 };
            dialog.ShowOrActivate(owner, replaceMode: true, initialText: null);

            var order = dialog.Controls.Cast<Control>()
                .Where(c => c.Visible && c is not Label)
                .OrderBy(c => c.TabIndex)
                .Select(c => c.GetType())
                .ToList();

            order.Take(4).Should().Equal(typeof(TextBox), typeof(TextBox), typeof(CheckBox), typeof(CheckBox));
        });

    [Fact]
    public void ShowOrActivate_検索モードでは置換関連コントロールを表示しない() =>
        RunInSta(() =>
        {
            using var owner = new Form();
            using var dialog = new FindReplaceDialog { Opacity = 0 };
            dialog.ShowOrActivate(owner, replaceMode: true, initialText: null);
            dialog.ShowOrActivate(owner, replaceMode: false, initialText: null);

            dialog.Text.Should().Be("検索");
            dialog.Controls.Cast<Control>().Count(c => c.Visible && c is TextBox).Should().Be(1);
            dialog.Controls.Cast<Control>().Count(c => c.Visible && c is Button).Should().Be(3);
        });

    static void RunInSta(Action action)
    {
        ExceptionDispatchInfo? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex) when ((exception = ExceptionDispatchInfo.Capture(ex)) is not null)
            {
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        exception?.Throw();
    }
}
