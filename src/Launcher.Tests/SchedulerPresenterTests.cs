using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

public sealed class SchedulerPresenterTests
{
    private static readonly DateTime Monday0900 = new(2025, 6, 16, 9, 0, 0);

    [Fact]
    public void ExecuteItemTasks_タスク列の完了後に完了通知を呼ぶ()
    {
        var events = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var done = new ManualResetEventSlim();
        int completedCount = 0;
        var item = new SchedulerItem
        {
            SleepTimeMs = 100,
            Tasks =
            [
                new SchedulerTask { Type = SchedulerTaskType.BalloonTip, Message = "1" },
                new SchedulerTask { Type = SchedulerTaskType.MessageBox, Message = "2" },
            ],
        };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var owner = new Control();
        SchedulerTaskRunner.ExecuteItemTasks(
            owner, item,
            (_, message) =>
            {
                events.Enqueue("balloon");
                throw new InvalidOperationException("タスクの例外");
            },
            (_, message) => events.Enqueue("message"),
            () =>
            {
                events.Enqueue("completed");
                Interlocked.Increment(ref completedCount);
                done.Set();
            });

        done.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        Thread.Sleep(200);
        events.Should().Equal("balloon", "message", "completed");
        completedCount.Should().Be(1);
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(190);
    }

    #region SpecificTimes (IsScheduleActive 経由)

    [Fact]
    public void SpecificTimes_last未満now以下の時刻で発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(9, 30));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void SpecificTimes_時刻がlast以前では不発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(8, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    [Fact]
    public void SpecificTimes_時刻がnow以降では不発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(11, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    [Fact]
    public void SpecificTimes_時刻がnowと同じで発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(10, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void SpecificTimes_時刻がlastと同じでは不発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(9, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    #endregion

    #region Interval (IsScheduleActive 経由)

    [Fact]
    public void Interval_範囲内の間隔時刻で発火()
    {
        var schedule = MakeIntervalSchedule(new HourMinute(8, 0), new HourMinute(20, 0), 30);
        var last = new DateTime(2025, 6, 16, 9, 25, 0);
        var now = new DateTime(2025, 6, 16, 9, 35, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void Interval_範囲外では不発火()
    {
        var schedule = MakeIntervalSchedule(new HourMinute(8, 0), new HourMinute(12, 0), 60);
        var last = new DateTime(2025, 6, 16, 13, 0, 0);
        var now = new DateTime(2025, 6, 16, 14, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    #endregion

    #region IsScheduleActive (日またぎ)

    [Fact]
    public void IsScheduleActive_日をまたいだ見逃し実行を検出()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(23, 0));
        schedule.Weekdays = [true, true, true, true, true, true, true];

        var last = new DateTime(2025, 6, 16, 22, 0, 0);
        var now = new DateTime(2025, 6, 17, 8, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void IsScheduleActive_無効スケジュールはfalse()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(9, 30));
        schedule.Enable = false;

        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerScheduleEvaluator.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    #endregion

    #region ShouldItemRun

    [Fact]
    public void ShouldItemRun_無効アイテムはfalse()
    {
        var item = new SchedulerItem
        {
            Enable = false,
            Schedules = [MakeSpecificTimesSchedule(new HourMinute(9, 30))],
            Tasks = [new SchedulerTask { FileName = "test.exe" }],
        };

        SchedulerScheduleEvaluator.ShouldItemRun(item, Monday0900.AddHours(1), Monday0900).Should().BeFalse();
    }

    [Fact]
    public void ShouldItemRun_有効アイテムでマッチするスケジュールがあればtrue()
    {
        var item = new SchedulerItem
        {
            Enable = true,
            Schedules = [MakeSpecificTimesSchedule(new HourMinute(9, 30))],
            Tasks = [new SchedulerTask { FileName = "test.exe" }],
        };

        SchedulerScheduleEvaluator.ShouldItemRun(item, Monday0900.AddHours(1), Monday0900).Should().BeTrue();
    }

    [Fact]
    public void ShouldItemRun_複数スケジュールのOR評価()
    {
        var item = new SchedulerItem
        {
            Enable = true,
            Schedules =
            [
                MakeSpecificTimesSchedule(new HourMinute(8, 0)),
                MakeSpecificTimesSchedule(new HourMinute(9, 30)),
            ],
            Tasks = [new SchedulerTask { FileName = "test.exe" }],
        };

        SchedulerScheduleEvaluator.ShouldItemRun(item, Monday0900.AddHours(1), Monday0900).Should().BeTrue();
    }

    #endregion

    #region GetItemsToRun

    [Fact]
    public void GetItemsToRun_マッチするアイテムのみ返す()
    {
        var lastCheckTime = Monday0900;
        var data = new SchedulerData
        {
            Items =
            [
                new SchedulerItem
                {
                    Enable = true, Name = "マッチ",
                    Schedules = [MakeSpecificTimesSchedule(new HourMinute(9, 30))],
                    Tasks = [new SchedulerTask { FileName = "a.exe" }],
                },
                new SchedulerItem
                {
                    Enable = true, Name = "不一致",
                    Schedules = [MakeSpecificTimesSchedule(new HourMinute(11, 0))],
                    Tasks = [new SchedulerTask { FileName = "b.exe" }],
                },
                new SchedulerItem
                {
                    Enable = false, Name = "無効",
                    Schedules = [MakeSpecificTimesSchedule(new HourMinute(9, 30))],
                    Tasks = [new SchedulerTask { FileName = "c.exe" }],
                },
            ],
        };

        var now = Monday0900.AddHours(1);
        var result = SchedulerScheduleEvaluator.GetItemsToRun(data, lastCheckTime, now);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("マッチ");
    }

    #endregion

    private static Schedule MakeSpecificTimesSchedule(params HourMinute[] times)
    {
        return new Schedule
        {
            Enable = true,
            TimeType = ScheduleTimeType.SpecificTimes,
            Times = [.. times],
            DateType = ScheduleDateType.Weekday,
            WeeksStart = new MonthDay(1, 1),
            WeeksEnd = new MonthDay(12, 31),
            Weekdays = [true, true, true, true, true, true, true],
        };
    }

    private static Schedule MakeIntervalSchedule(HourMinute start, HourMinute end, int intervalMinutes)
    {
        return new Schedule
        {
            Enable = true,
            TimeType = ScheduleTimeType.Interval,
            TimeIntervalStart = start,
            TimeIntervalEnd = end,
            TimeIntervalMinutes = intervalMinutes,
            DateType = ScheduleDateType.Weekday,
            WeeksStart = new MonthDay(1, 1),
            WeeksEnd = new MonthDay(12, 31),
            Weekdays = [true, true, true, true, true, true, true],
        };
    }
}
