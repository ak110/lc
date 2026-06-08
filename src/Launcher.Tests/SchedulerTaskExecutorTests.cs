using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// 単一スケジューラータスク実行 (<see cref="SchedulerTaskExecutor"/>) のテスト。
/// </summary>
public sealed class SchedulerTaskExecutorTests
{
    [Fact]
    public void Execute_BalloonTipタスクでShowBalloonTipActionが呼ばれる()
    {
        string? capturedTitle = null;
        string? capturedMessage = null;
        var task = new SchedulerTask { Type = SchedulerTaskType.BalloonTip, Message = "テスト通知" };
        SchedulerTaskExecutor.Execute(task, (t, m) => { capturedTitle = t; capturedMessage = m; }, null);

        capturedTitle.Should().NotBeNull();
        capturedMessage.Should().Be("テスト通知");
    }

    [Fact]
    public void Execute_MessageBoxタスクでShowMessageBoxActionが呼ばれる()
    {
        string? capturedTitle = null;
        string? capturedMessage = null;
        var task = new SchedulerTask { Type = SchedulerTaskType.MessageBox, Message = "テストメッセージ" };
        SchedulerTaskExecutor.Execute(task, null, (t, m) => { capturedTitle = t; capturedMessage = m; });

        capturedTitle.Should().NotBeNull();
        capturedMessage.Should().Be("テストメッセージ");
    }

    [Fact]
    public void Execute_メッセージ内の環境変数が展開される()
    {
        string? capturedMessage = null;
        var task = new SchedulerTask { Type = SchedulerTaskType.BalloonTip, Message = "%USERNAME%" };
        SchedulerTaskExecutor.Execute(task, (_, m) => capturedMessage = m, null);

        capturedMessage.Should().NotBe("%USERNAME%");
        capturedMessage.Should().Be(Environment.GetEnvironmentVariable("USERNAME"));
    }

    [Fact]
    public void Execute_デリゲート未設定でも例外が発生しない()
    {
        var balloonTask = new SchedulerTask { Type = SchedulerTaskType.BalloonTip, Message = "テスト" };
        var messageBoxTask = new SchedulerTask { Type = SchedulerTaskType.MessageBox, Message = "テスト" };

        var act1 = () => SchedulerTaskExecutor.Execute(balloonTask, null, null);
        var act2 = () => SchedulerTaskExecutor.Execute(messageBoxTask, null, null);

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }
}
