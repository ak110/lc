using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// スケジューラーのオーケストレーション層 (<see cref="SchedulerPresenter"/>) のテスト。
/// 日付条件判定の単体は <see cref="SchedulerScheduleEvaluatorTests"/>、
/// 単一タスク実行は <see cref="SchedulerTaskExecutorTests"/> を参照。
/// </summary>
public sealed class SchedulerPresenterTests
{
    // テスト用日時: 2025年6月16日(月) を基準に使う
    private static readonly DateTime Monday0900 = new(2025, 6, 16, 9, 0, 0);

    #region SpecificTimes (IsScheduleActive 経由)

    [Fact]
    public void SpecificTimes_last未満now以下の時刻で発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(9, 30));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void SpecificTimes_時刻がlast以前では不発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(8, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    [Fact]
    public void SpecificTimes_時刻がnow以降では不発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(11, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    [Fact]
    public void SpecificTimes_時刻がnowと同じで発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(10, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void SpecificTimes_時刻がlastと同じでは不発火()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(9, 0));
        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    #endregion

    #region Interval (IsScheduleActive 経由)

    [Fact]
    public void Interval_範囲内の間隔時刻で発火()
    {
        // 8:00～20:00 の間、30分間隔
        var schedule = MakeIntervalSchedule(new HourMinute(8, 0), new HourMinute(20, 0), 30);
        var last = new DateTime(2025, 6, 16, 9, 25, 0);
        var now = new DateTime(2025, 6, 16, 9, 35, 0);

        // 9:30 が last(9:25) < t <= now(9:35) に該当
        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void Interval_範囲外では不発火()
    {
        // 8:00～12:00 の間、60分間隔
        var schedule = MakeIntervalSchedule(new HourMinute(8, 0), new HourMinute(12, 0), 60);
        var last = new DateTime(2025, 6, 16, 13, 0, 0);
        var now = new DateTime(2025, 6, 16, 14, 0, 0);

        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeFalse();
    }

    #endregion

    #region IsScheduleActive (日またぎ)

    [Fact]
    public void IsScheduleActive_日をまたいだ見逃し実行を検出()
    {
        // 月曜 23:00 に実行するスケジュール
        var schedule = MakeSpecificTimesSchedule(new HourMinute(23, 0));
        schedule.Weekdays = [true, true, true, true, true, true, true]; // 全曜日

        // アプリが月曜22:00～火曜08:00まで停止していた
        var last = new DateTime(2025, 6, 16, 22, 0, 0);  // 月曜 22:00
        var now = new DateTime(2025, 6, 17, 8, 0, 0);    // 火曜 08:00

        // 月曜23:00が検出されるべき
        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeTrue();
    }

    [Fact]
    public void IsScheduleActive_無効スケジュールはfalse()
    {
        var schedule = MakeSpecificTimesSchedule(new HourMinute(9, 30));
        schedule.Enable = false;

        var last = new DateTime(2025, 6, 16, 9, 0, 0);
        var now = new DateTime(2025, 6, 16, 10, 0, 0);

        SchedulerPresenter.IsScheduleActive(schedule, now, last).Should().BeFalse();
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

        SchedulerPresenter.ShouldItemRun(item, Monday0900.AddHours(1), Monday0900).Should().BeFalse();
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

        SchedulerPresenter.ShouldItemRun(item, Monday0900.AddHours(1), Monday0900).Should().BeTrue();
    }

    [Fact]
    public void ShouldItemRun_複数スケジュールのOR評価()
    {
        var item = new SchedulerItem
        {
            Enable = true,
            Schedules =
            [
                MakeSpecificTimesSchedule(new HourMinute(8, 0)),  // last(9:00)より前→不発火
                MakeSpecificTimesSchedule(new HourMinute(9, 30)), // 範囲内→発火
            ],
            Tasks = [new SchedulerTask { FileName = "test.exe" }],
        };

        SchedulerPresenter.ShouldItemRun(item, Monday0900.AddHours(1), Monday0900).Should().BeTrue();
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
        var result = SchedulerPresenter.GetItemsToRun(data, lastCheckTime, now);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("マッチ");
    }

    #endregion

    // --- ヘルパー ---

    /// <summary>指定時刻のスケジュール (全曜日有効) を作成</summary>
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

    /// <summary>間隔ベースのスケジュール (全曜日有効) を作成</summary>
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
