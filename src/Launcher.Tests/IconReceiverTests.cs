using FluentAssertions;
using Launcher.UI;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class IconReceiverTests
{
    [Theory]
    [InlineData("採用")]
    [InlineData("旧世代")]
    [InlineData("配送後世代更新")]
    [InlineData("ローダー破棄")]
    [InlineData("配送後ローダー破棄")]
    [InlineData("ハンドル未作成")]
    [InlineData("フォーム破棄")]
    [InlineData("配送後ハンドル破棄")]
    public void 受信したアイコンは採用または破棄の後に解放する(string scenario) => UiAcceptance.Run(() =>
    {
        using var loader = new AsyncIconLoader(workerCount: 0);
        using var host = new Form();
        using var target = new Control();
        using var icon = (Icon)SystemIcons.Application.Clone();
        using var images = new ImageList();
        Bitmap? image = null;
        int applied = 0;
        host.Shown += (_, _) =>
        {
            if (scenario != "ハンドル未作成") _ = target.Handle;
            var result = new IconLoadedEventArgs(icon, "icon.exe", true, null, loader.Generation);
            if (scenario == "旧世代") loader.Clear();
            if (scenario == "ローダー破棄") loader.Dispose();
            if (scenario == "フォーム破棄") target.Dispose();
            IconReceiver.Receive(target, loader, result, value =>
            {
                image = value.ToBitmap();
                images.Images.Add(value);
                images.Images.SetKeyName(images.Images.Count - 1, "icon.exe");
                applied++;
            });
            if (scenario == "配送後世代更新") loader.Clear();
            if (scenario == "配送後ローダー破棄") loader.Dispose();
            if (scenario == "配送後ハンドル破棄") target.Dispose();
            UiThreadDispatcher.SafeBeginInvoke(host, host.Close);
        };
        Application.Run(host);
        using (image)
        {
            applied.Should().Be(scenario == "採用" ? 1 : 0);
            if (scenario == "採用")
            {
                image.Should().NotBeNull();
                using var retainedImage = images.Images["icon.exe"];
                retainedImage.Should().NotBeNull();
            }
            Action readDisposedIcon = () => { using var unused = icon.ToBitmap(); };
            readDisposedIcon.Should().Throw<Exception>();
        }
    });

    [Fact]
    public void 配送後のハンドル破棄は処理を実行せず解放を一度だけ呼ぶ() => UiAcceptance.Run(() =>
    {
        using var host = new Form();
        using var target = new Control();
        int invoked = 0;
        int skipped = 0;
        host.Shown += (_, _) =>
        {
            _ = target.Handle;
            UiThreadDispatcher.SafeBeginInvoke(target, () => invoked++, () => skipped++);
            target.Dispose();
            UiThreadDispatcher.SafeBeginInvoke(host, host.Close);
        };
        Application.Run(host);
        invoked.Should().Be(0);
        skipped.Should().Be(1);
    });
}
