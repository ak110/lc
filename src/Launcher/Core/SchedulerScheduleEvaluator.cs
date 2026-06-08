namespace Launcher.Core;

/// <summary>
/// スケジュールの日付条件・時刻条件を判定する純粋関数群。
/// 副作用ある <see cref="SchedulerPresenter"/> から、テスト容易性のために分離した。
/// </summary>
public static class SchedulerScheduleEvaluator
{
    /// <summary>
    /// 特定の日について、日付条件と時刻条件を組み合わせて判定する。
    /// </summary>
    public static bool CheckDateAndTime(Schedule schedule, DateOnly date, DateTime now, DateTime last)
    {
        if (!MatchesDateCondition(schedule, date))
            return false;

        // この日の有効時刻範囲を計算 (last, now でクランプ)
        // rangeStart = last より後 (last と同じ時刻は含まない)
        // rangeEnd = now と同じかそれより前
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(new TimeOnly(23, 59, 59));
        var effectiveStart = last > dayStart ? last : dayStart;
        var effectiveEnd = now < dayEnd ? now : dayEnd;

        if (effectiveStart > effectiveEnd)
            return false;

        var rangeStart = TimeOnly.FromDateTime(effectiveStart);
        var rangeEnd = TimeOnly.FromDateTime(effectiveEnd);

        // lastと同じ日の場合、rangeStartはlastの時刻 (排他的: last < t)
        // 異なる日の場合、rangeStartは00:00 (包含的: 00:00 <= t)
        bool startExclusive = DateOnly.FromDateTime(last) == date;

        return CheckTimeInRange(schedule, rangeStart, rangeEnd, startExclusive);
    }

    /// <summary>
    /// 日付条件に合致するか判定する。
    /// </summary>
    public static bool MatchesDateCondition(Schedule schedule, DateOnly date)
    {
        if (schedule.DateType == ScheduleDateType.Weekday)
        {
            return MatchesWeekday(schedule, date);
        }
        else
        {
            return MatchesDateInterval(schedule, date);
        }
    }

    /// <summary>
    /// 曜日ベースの日付条件を判定する。
    /// </summary>
    private static bool MatchesWeekday(Schedule schedule, DateOnly date)
    {
        // 月日範囲チェック
        var md = new MonthDay(date.Month, date.Day);
        if (md < schedule.WeeksStart || md > schedule.WeeksEnd)
            return false;

        // 曜日チェック ([0]=月 ... [6]=日)
        int dayIndex = date.DayOfWeek switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => -1,
        };
        return dayIndex >= 0 && dayIndex < schedule.Weekdays.Length && schedule.Weekdays[dayIndex];
    }

    /// <summary>
    /// 日数間隔ベースの日付条件を判定する。
    /// DateOnly ベースの日数差分で年境界を正確に扱う。
    /// </summary>
    private static bool MatchesDateInterval(Schedule schedule, DateOnly date)
    {
        var md = new MonthDay(date.Month, date.Day);
        var start = schedule.DateIntervalStart;
        var end = schedule.DateIntervalEnd;

        // 月日範囲チェック (end < start の場合はラップアラウンド: 11月～2月など)
        bool inRange;
        if (start <= end)
        {
            inRange = md >= start && md <= end;
        }
        else
        {
            inRange = md >= start || md <= end;
        }
        if (!inRange)
            return false;

        // 日数間隔チェック: 開始日からの経過日数が間隔の倍数か
        int interval = schedule.DateIntervalDays;
        if (interval <= 0) interval = 1;

        // 基準年は当年の開始月日
        var startDate = new DateOnly(date.Year, start.Month, Math.Min(start.Day, DateTime.DaysInMonth(date.Year, start.Month)));
        // ラップアラウンドで開始日が未来の場合は前年基準
        if (startDate > date)
            startDate = new DateOnly(date.Year - 1, start.Month, Math.Min(start.Day, DateTime.DaysInMonth(date.Year - 1, start.Month)));

        int daysSinceStart = date.DayNumber - startDate.DayNumber;
        return daysSinceStart >= 0 && (daysSinceStart % interval) == 0;
    }

    /// <summary>
    /// 時刻条件を判定する。rangeStart～rangeEnd の間にスケジュール時刻があるか。
    /// </summary>
    private static bool CheckTimeInRange(Schedule schedule, TimeOnly rangeStart, TimeOnly rangeEnd, bool startExclusive)
    {
        if (schedule.TimeType == ScheduleTimeType.SpecificTimes)
        {
            return CheckSpecificTimes(schedule.Times, rangeStart, rangeEnd, startExclusive);
        }
        else
        {
            return CheckIntervalTimes(
                schedule.TimeIntervalStart, schedule.TimeIntervalEnd,
                schedule.TimeIntervalMinutes, rangeStart, rangeEnd, startExclusive);
        }
    }

    /// <summary>
    /// 指定時刻リストから、範囲内に含まれる時刻があるか判定する。
    /// 条件: startExclusive ? (rangeStart &lt; t &amp;&amp; t &lt;= rangeEnd) : (rangeStart &lt;= t &amp;&amp; t &lt;= rangeEnd)
    /// </summary>
    private static bool CheckSpecificTimes(List<HourMinute> times, TimeOnly rangeStart, TimeOnly rangeEnd, bool startExclusive)
    {
        foreach (var hm in times)
        {
            var t = new TimeOnly(hm.Hour, hm.Minute);
            bool afterStart = startExclusive ? t > rangeStart : t >= rangeStart;
            if (afterStart && t <= rangeEnd)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 間隔ベースの時刻リストから、範囲内に含まれる時刻があるか判定する。
    /// </summary>
    private static bool CheckIntervalTimes(
        HourMinute intervalStart, HourMinute intervalEnd, int intervalMinutes,
        TimeOnly rangeStart, TimeOnly rangeEnd, bool startExclusive)
    {
        if (intervalMinutes <= 0) intervalMinutes = 1;
        int startMin = intervalStart.ToMinutes();
        int endMin = intervalEnd.ToMinutes();
        for (int m = startMin; m <= endMin; m += intervalMinutes)
        {
            var t = new TimeOnly(m / 60, m % 60);
            bool afterStart = startExclusive ? t > rangeStart : t >= rangeStart;
            if (afterStart && t <= rangeEnd)
                return true;
        }
        return false;
    }
}
