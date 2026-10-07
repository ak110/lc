using System.Drawing;
using FluentAssertions;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class SharedUiComponentsTests
{
    [Fact]
    public void タブの当たり判定とホイール移動は両端を循環する() => UiAcceptance.Run(() =>
    {
        using var form = new Form();
        using var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.AddRange([new TabPage("一"), new TabPage("二"), new TabPage("三")]);
        form.Controls.Add(tabs);
        UiAcceptance.InspectDialog(form, () =>
        {
            var second = tabs.GetTabRect(1);
            TabControlHelper.HitTest(tabs, new Point(second.Left + 2, second.Top + 2)).Should().Be(1);
            TabControlHelper.HitTest(tabs, new Point(-1, -1)).Should().Be(-1);
            tabs.SelectedIndex = 0;
            TabControlHelper.SelectByWheel(tabs, 120);
            tabs.SelectedIndex.Should().Be(2);
            TabControlHelper.SelectByWheel(tabs, -120);
            tabs.SelectedIndex.Should().Be(0);
            TabControlHelper.SelectByWheel(tabs, 0);
            tabs.SelectedIndex.Should().Be(0);
            tabs.TabPages.Clear();
            TabControlHelper.SelectByWheel(tabs, 120);
            tabs.SelectedIndex.Should().Be(-1);
        });
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void 共通入力は親のTopMostを伝播して確定または取消を返す(bool topMost, bool cancel) => UiAcceptance.Run(() =>
    {
        using var owner = new Form { TopMost = topMost };
        owner.Show();
        bool? observedTopMost = null;
        Form? observedOwner = null;
        using var timer = new System.Windows.Forms.Timer { Interval = 10 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.OfType<InputDialog>().SingleOrDefault();
            if (dialog is null) return;
            timer.Stop();
            observedTopMost = dialog.TopMost;
            observedOwner = dialog.Owner;
            dialog.Controls.OfType<TextBox>().Single().Text = "変更した名前";
            (cancel ? dialog.CancelButton : dialog.AcceptButton)!.PerformClick();
        };
        timer.Start();
        string? result = InputDialog.Prompt(owner, "名前", "タブ", "変更前");
        observedTopMost.Should().Be(topMost);
        observedOwner.Should().BeSameAs(owner);
        result.Should().Be(cancel ? null : "変更した名前");
    });

    [Theory]
    [InlineData(-100000, -100000)]
    [InlineData(100000, 100000)]
    public void 画面外位置を復元するとフォーム全体が作業領域内へ収まる(int x, int y) => UiAcceptance.Run(() =>
    {
        using var form = new Form { Size = new Size(320, 180), StartPosition = FormStartPosition.Manual };
        form.SetLocationWithClip(new Point(x, y));
        UiAcceptance.InspectDialog(form, () =>
            Screen.AllScreens.Any(screen => screen.WorkingArea.Contains(form.Bounds)).Should().BeTrue());
    });

    [Fact]
    public void カーソル中心配置は大きなフォームも現在画面の作業領域へ収める() => UiAcceptance.Run(() =>
    {
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        using var form = new Form
        {
            Size = new Size(area.Width + 100, area.Height + 100),
            StartPosition = FormStartPosition.Manual,
        };
        FormsHelper.CenterOnCursor(form);
        UiAcceptance.InspectDialog(form, () => area.Contains(form.Bounds).Should().BeTrue());
    });
}
