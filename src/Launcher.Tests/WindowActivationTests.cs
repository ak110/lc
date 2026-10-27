using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class WindowActivationTests
{
    [Fact]
    public void ボタン型ランチャーは元ウィンドウのキャプチャを解放してから表示する() => UiAcceptance.Run(() =>
    {
        using var data = new UiHostData(new Config
        {
            TrayIcon = false,
            HotKey = "",
            WindowHideNoActive = false,
            ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight,
        });
        using var host = new ApplicationHostForm(data.BaseName);
        using var source = new Form();
        using var button = new Button();
        source.Controls.Add(button);
        try
        {
            host.ButtonLauncherData.IsLocked = false;
            var launcher = host.OwnedForms.OfType<ButtonLauncherForm>().Single();
            source.Show();
            source.Activate();
            button.Capture = true;
            button.Capture.Should().BeTrue();
            bool? captureWhenShown = null;
            launcher.VisibleChanged += (_, _) =>
            {
                if (launcher.Visible) captureWhenShown ??= button.Capture;
            };

            launcher.ShowLauncher();

            captureWhenShown.Should().BeFalse();
            launcher.Visible.Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });

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
