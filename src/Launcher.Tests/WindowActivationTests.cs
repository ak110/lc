using FluentAssertions;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class WindowActivationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 表示イベントから再入しても別フォームを表示せずTopMostを復元する(bool topMost) => UiAcceptance.Run(() =>
    {
        using var form = new Form { TopMost = topMost };
        using var nested = new Form();
        int visibleEvents = 0;
        form.VisibleChanged += (_, _) =>
        {
            if (!form.Visible) return;
            visibleEvents++;
            WindowHelper.ActivateForce(nested);
        };
        WindowHelper.ActivateForce(form);
        form.Visible.Should().BeTrue();
        visibleEvents.Should().Be(1);
        nested.Visible.Should().BeFalse();
        form.TopMost.Should().Be(topMost);
        WindowHelper.ActivateForce(nested);
        nested.Visible.Should().BeTrue();
    });
}
