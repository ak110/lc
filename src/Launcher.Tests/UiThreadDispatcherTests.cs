using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// UiThreadDispatcherのテスト
/// </summary>
public sealed class UiThreadDispatcherTests
{
    [Fact]
    public void SafeBeginInvoke_ハンドル未作成ならactionを実行せずonSkippedを呼ぶ()
    {
        using var control = new Control();
        bool actionCalled = false;
        bool onSkippedCalled = false;

        UiThreadDispatcher.SafeBeginInvoke(control,
            () => actionCalled = true,
            onSkipped: () => onSkippedCalled = true);

        actionCalled.Should().BeFalse();
        onSkippedCalled.Should().BeTrue();
    }

    [Fact]
    public void SafeBeginInvoke_破棄済みならactionを実行せずonSkippedを呼ぶ()
    {
        var control = new Control();
        _ = control.Handle; // ハンドル作成
        control.Dispose();
        bool actionCalled = false;
        bool onSkippedCalled = false;

        UiThreadDispatcher.SafeBeginInvoke(control,
            () => actionCalled = true,
            onSkipped: () => onSkippedCalled = true);

        actionCalled.Should().BeFalse();
        onSkippedCalled.Should().BeTrue();
    }

    [Fact]
    public void SafeBeginInvoke_onSkipped省略時はガード発火でも何もしない()
    {
        using var control = new Control();
        bool actionCalled = false;

        UiThreadDispatcher.SafeBeginInvoke(control, () => actionCalled = true);

        actionCalled.Should().BeFalse();
    }

    [Fact]
    public void SafeBeginInvoke_ハンドル作成済みならactionをポストしonSkippedは呼ばない()
    {
        using var form = new Form();
        _ = form.Handle; // ハンドル作成
        bool actionCalled = false;
        bool onSkippedCalled = false;
        using var completed = new ManualResetEventSlim();

        UiThreadDispatcher.SafeBeginInvoke(form, () =>
        {
            actionCalled = true;
            completed.Set();
        }, onSkipped: () => onSkippedCalled = true);

        // BeginInvokeで投入されたポストをUIメッセージポンプで処理する
        Application.DoEvents();
        completed.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();

        actionCalled.Should().BeTrue();
        onSkippedCalled.Should().BeFalse();
    }
}
