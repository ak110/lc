using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// StaThreadRunnerのテスト
/// </summary>
public sealed class StaThreadRunnerTests
{
    [Fact]
    public void Start_別のバックグラウンドSTAスレッドで処理する()
    {
        int callerThreadId = Environment.CurrentManagedThreadId;
        int workerThreadId = callerThreadId;
        bool isBackground = false;
        ApartmentState apartmentState = ApartmentState.Unknown;

        Thread worker = StaThreadRunner.Start(() =>
        {
            workerThreadId = Environment.CurrentManagedThreadId;
            isBackground = Thread.CurrentThread.IsBackground;
            apartmentState = Thread.CurrentThread.GetApartmentState();
        });

        worker.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
        workerThreadId.Should().NotBe(callerThreadId);
        isBackground.Should().BeTrue();
        apartmentState.Should().Be(ApartmentState.STA);
    }
}
