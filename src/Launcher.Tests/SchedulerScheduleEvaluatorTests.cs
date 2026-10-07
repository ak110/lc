using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// スケジュール日付条件判定 (<see cref="SchedulerScheduleEvaluator"/>) のテスト。
/// 時刻条件判定は内部ディスパッチのため、<see cref="SchedulerPresenterTests"/> の
/// <c>SpecificTimes</c>・<c>Interval</c> region で間接的に検証する。
/// </summary>
public sealed class SchedulerScheduleEvaluatorTests
{
    #region MatchesDateCondition (Weekday)

    [Fact]
    public void Weekday_正しい曜日でtrue()
    {
        var schedule = MakeWeekdaySchedule([true, false, false, false, false, false, false]); // 月曜のみ
        var date = new DateOnly(2025, 6, 16); // 月曜

        SchedulerScheduleEvaluator.MatchesDateCondition(schedule, date).Should().BeTrue();
    }

    [Fact]
    public void Weekday_誤った曜日でfalse()
    {
        var schedule = MakeWeekdaySchedule([true, false, false, false, false, false, false]); // 月曜のみ
        var date = new DateOnly(2025, 6, 17); // 火曜

        SchedulerScheduleEvaluator.MatchesDateCondition(schedule, date).Should().BeFalse();
    }

    [Fact]
    public void Weekday_月日範囲外でfalse()
    {
        var schedule = MakeWeekdaySchedule([true, true, true, true, true, true, true]);
        schedule.WeeksStart = new MonthDay(7, 1);
        schedule.WeeksEnd = new MonthDay(8, 31);

        var date = new DateOnly(2025, 6, 16); // 6月は範囲外
        SchedulerScheduleEvaluator.MatchesDateCondition(schedule, date).Should().BeFalse();
    }

    #endregion

    #region MatchesDateCondition (DateInterval)

    [Fact]
    public void DateInterval_正しい日数間隔でtrue()
    {
        var schedule = MakeDateIntervalSchedule(new MonthDay(1, 1), new MonthDay(12, 31), 3);
        // 2025/1/1 から3日おき: 1/1, 1/4, 1/7, ...
        var date = new DateOnly(2025, 1, 4);
        SchedulerScheduleEvaluator.MatchesDateCondition(schedule, date).Should().BeTrue();
    }

    [Fact]
    public void DateInterval_年をまたぐ場合も正しく動作()
    {
        // 12/30(開始)から2日間隔: 12/30, 1/1, 1/3。1/2はマッチしない。
        var schedule = MakeDateIntervalSchedule(new MonthDay(12, 30), new MonthDay(1, 5), 2);
        var dec30 = new DateOnly(2025, 12, 30);
        var jan1 = new DateOnly(2026, 1, 1);
        var jan2 = new DateOnly(2026, 1, 2);

        SchedulerScheduleEvaluator.MatchesDateCondition(schedule, dec30).Should().BeTrue();
        SchedulerScheduleEvaluator.MatchesDateCondition(schedule, jan1).Should().BeTrue();
        SchedulerScheduleEvaluator.MatchesDateCondition(schedule, jan2).Should().BeFalse();
    }

    #endregion

    [Theory]
    [InlineData(11, 1, 2, 28, 12, 15, true)]
    [InlineData(11, 1, 2, 28, 1, 15, true)]
    [InlineData(11, 1, 2, 28, 11, 1, true)]
    [InlineData(11, 1, 2, 28, 2, 28, true)]
    [InlineData(11, 1, 2, 28, 3, 1, false)]
    [InlineData(11, 1, 2, 28, 4, 15, false)]
    [InlineData(11, 1, 2, 28, 5, 15, false)]
    [InlineData(11, 1, 2, 28, 6, 15, false)]
    [InlineData(11, 1, 2, 28, 7, 15, false)]
    [InlineData(11, 1, 2, 28, 8, 15, false)]
    [InlineData(11, 1, 2, 28, 9, 15, false)]
    [InlineData(11, 1, 2, 28, 10, 31, false)]
    [InlineData(4, 1, 9, 30, 4, 1, true)]
    [InlineData(4, 1, 9, 30, 9, 30, true)]
    [InlineData(4, 1, 9, 30, 3, 31, false)]
    [InlineData(4, 1, 9, 30, 10, 1, false)]
    [InlineData(6, 15, 6, 15, 6, 15, true)]
    [InlineData(6, 15, 6, 15, 6, 14, false)]
    [InlineData(6, 15, 6, 15, 6, 16, false)]
    public void 月日範囲_曜日と日数間隔で同じ期間を判定する(
        int startMonth, int startDay, int endMonth, int endDay, int month, int day, bool expected)
    {
        var start = new MonthDay(startMonth, startDay);
        var end = new MonthDay(endMonth, endDay);
        var weekday = MakeWeekdaySchedule([true, true, true, true, true, true, true]);
        weekday.WeeksStart = start;
        weekday.WeeksEnd = end;
        var interval = MakeDateIntervalSchedule(start, end, 1);
        var date = new DateOnly(2026, month, day);

        SchedulerScheduleEvaluator.MatchesDateCondition(weekday, date).Should().Be(expected);
        SchedulerScheduleEvaluator.MatchesDateCondition(interval, date).Should().Be(expected);
    }

    // --- ヘルパー ---

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
