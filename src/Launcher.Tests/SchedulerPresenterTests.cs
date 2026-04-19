using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// スケジューラーロジックのテスト
/// </summary>
public sealed class SchedulerPresenterTests
{
    // テスト用日時: 2025年6月16日(月) を基準に使う
    private static readonly DateTime Monday0900 = new(2025, 6, 16, 9, 0, 0);

    #region CheckTimeInRange (SpecificTimes)

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

    #region CheckTimeInRange (Interval)

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

    #region MatchesDateCondition (Weekday)

    [Fact]
    public void Weekday_正しい曜日でtrue()
    {
        var schedule = MakeWeekdaySchedule([true, false, false, false, false, false, false]); // 月曜のみ
        var date = new DateOnly(2025, 6, 16); // 月曜

        SchedulerPresenter.MatchesDateCondition(schedule, date).Should().BeTrue();
    }

    [Fact]
    public void Weekday_誤った曜日でfalse()
    {
        var schedule = MakeWeekdaySchedule([true, false, false, false, false, false, false]); // 月曜のみ
        var date = new DateOnly(2025, 6, 17); // 火曜

        SchedulerPresenter.MatchesDateCondition(schedule, date).Should().BeFalse();
    }

    [Fact]
    public void Weekday_月日範囲外でfalse()
    {
        var schedule = MakeWeekdaySchedule([true, true, true, true, true, true, true]);
        schedule.WeeksStart = new MonthDay(7, 1);
        schedule.WeeksEnd = new MonthDay(8, 31);

        var date = new DateOnly(2025, 6, 16); // 6月は範囲外
        SchedulerPresenter.MatchesDateCondition(schedule, date).Should().BeFalse();
    }

    #endregion

    #region MatchesDateCondition (DateInterval)

    [Fact]
    public void DateInterval_正しい日数間隔でtrue()
    {
        var schedule = MakeDateIntervalSchedule(new MonthDay(1, 1), new MonthDay(12, 31), 3);
        // 2025/1/1 から3日おき: 1/1, 1/4, 1/7, ...
        var date = new DateOnly(2025, 1, 4);
        SchedulerPresenter.MatchesDateCondition(schedule, date).Should().BeTrue();
    }

    [Fact]
    public void DateInterval_年をまたぐ場合も正しく動作()
    {
        // 元のすけじゅらでバグだった: DayOfYear % (DateInterval + 1) は年境界で壊れる
        var schedule = MakeDateIntervalSchedule(new MonthDay(12, 30), new MonthDay(1, 5), 2);
        // 12/30(開始), 1/1(2日後), 1/3(4日後)
        var dec30 = new DateOnly(2025, 12, 30);
        var jan1 = new DateOnly(2026, 1, 1);
        var jan2 = new DateOnly(2026, 1, 2);

        SchedulerPresenter.MatchesDateCondition(schedule, dec30).Should().BeTrue();
        SchedulerPresenter.MatchesDateCondition(schedule, jan1).Should().BeTrue();
        SchedulerPresenter.MatchesDateCondition(schedule, jan2).Should().BeFalse();
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

    #region ExecuteTask (メッセージ表示)

    [Fact]
    public void ExecuteTask_BalloonTipタスクでShowBalloonTipActionが呼ばれる()
    {
        string? capturedTitle = null;
        string? capturedMessage = null;
        var task = new SchedulerTask { Type = SchedulerTaskType.BalloonTip, Message = "テスト通知" };
        SchedulerPresenter.ExecuteTask(task, (t, m) => { capturedTitle = t; capturedMessage = m; }, null);

        capturedTitle.Should().NotBeNull();
        capturedMessage.Should().Be("テスト通知");
    }

    [Fact]
    public void ExecuteTask_MessageBoxタスクでShowMessageBoxActionが呼ばれる()
    {
        string? capturedTitle = null;
        string? capturedMessage = null;
        var task = new SchedulerTask { Type = SchedulerTaskType.MessageBox, Message = "テストメッセージ" };
        SchedulerPresenter.ExecuteTask(task, null, (t, m) => { capturedTitle = t; capturedMessage = m; });

        capturedTitle.Should().NotBeNull();
        capturedMessage.Should().Be("テストメッセージ");
    }

    [Fact]
    public void ExecuteTask_メッセージ内の環境変数が展開される()
    {
        string? capturedMessage = null;
        var task = new SchedulerTask { Type = SchedulerTaskType.BalloonTip, Message = "%USERNAME%" };
        SchedulerPresenter.ExecuteTask(task, (_, m) => capturedMessage = m, null);

        capturedMessage.Should().NotBe("%USERNAME%");
        capturedMessage.Should().Be(Environment.GetEnvironmentVariable("USERNAME"));
    }

    [Fact]
    public void ExecuteTask_デリゲート未設定でも例外が発生しない()
    {
        var balloonTask = new SchedulerTask { Type = SchedulerTaskType.BalloonTip, Message = "テスト" };
        var messageBoxTask = new SchedulerTask { Type = SchedulerTaskType.MessageBox, Message = "テスト" };

        var act1 = () => SchedulerPresenter.ExecuteTask(balloonTask, null, null);
        var act2 = () => SchedulerPresenter.ExecuteTask(messageBoxTask, null, null);

        act1.Should().NotThrow();
        act2.Should().NotThrow();
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

    /// <summary>曜日指定のスケジュール (時刻は全時間帯) を作成</summary>
    private static Schedule MakeWeekdaySchedule(bool[] weekdays)
    {
        return new Schedule
        {
            Enable = true,
            TimeType = ScheduleTimeType.SpecificTimes,
            Times = [new HourMinute(0, 0)],
            DateType = ScheduleDateType.Weekday,
            WeeksStart = new MonthDay(1, 1),
            WeeksEnd = new MonthDay(12, 31),
            Weekdays = weekdays,
        };
    }

    /// <summary>日数間隔のスケジュール (時刻は0:00、曜日は全日) を作成</summary>
    private static Schedule MakeDateIntervalSchedule(MonthDay start, MonthDay end, int interval)
    {
        return new Schedule
        {
            Enable = true,
            TimeType = ScheduleTimeType.SpecificTimes,
            Times = [new HourMinute(0, 0)],
            DateType = ScheduleDateType.DateInterval,
            DateIntervalStart = start,
            DateIntervalEnd = end,
            DateIntervalDays = interval,
        };
    }
}
