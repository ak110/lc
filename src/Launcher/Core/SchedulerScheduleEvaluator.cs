namespace Launcher.Core;

public static class SchedulerScheduleEvaluator
{
    public static List<SchedulerItem> GetItemsToRun(SchedulerData data, DateTime lastCheckTime, DateTime now)
    {
        var result = new List<SchedulerItem>();
        foreach (var item in data.Items)
        {
            if (ShouldItemRun(item, now, lastCheckTime))
                result.Add(item);
        }
        return result;
    }

    public static bool ShouldItemRun(SchedulerItem item, DateTime now, DateTime last)
    {
        if (!item.Enable) return false;
        foreach (var schedule in item.Schedules)
        {
            if (IsScheduleActive(schedule, now, last))
                return true;
        }
        return false;
    }

    public static bool IsScheduleActive(Schedule schedule, DateTime now, DateTime last)
    {
        if (!schedule.Enable) return false;

        var startDate = DateOnly.FromDateTime(last);
        var endDate = DateOnly.FromDateTime(now);
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (CheckDateAndTime(schedule, date, now, last))
                return true;
        }
        return false;
    }


    public static bool CheckDateAndTime(Schedule schedule, DateOnly date, DateTime now, DateTime last)
    {
        if (!MatchesDateCondition(schedule, date))
            return false;

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(new TimeOnly(23, 59, 59));
        var effectiveStart = last > dayStart ? last : dayStart;
        var effectiveEnd = now < dayEnd ? now : dayEnd;

        if (effectiveStart > effectiveEnd)
            return false;

        var rangeStart = TimeOnly.FromDateTime(effectiveStart);
        var rangeEnd = TimeOnly.FromDateTime(effectiveEnd);

        bool startExclusive = DateOnly.FromDateTime(last) == date;

        return CheckTimeInRange(schedule, rangeStart, rangeEnd, startExclusive);
    }

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

    private static bool MatchesWeekday(Schedule schedule, DateOnly date)
    {
        var md = new MonthDay(date.Month, date.Day);
        if (!IsInMonthDayRange(md, schedule.WeeksStart, schedule.WeeksEnd))
            return false;

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

    private static bool MatchesDateInterval(Schedule schedule, DateOnly date)
    {
        var md = new MonthDay(date.Month, date.Day);
        var start = schedule.DateIntervalStart;
        var end = schedule.DateIntervalEnd;

        if (!IsInMonthDayRange(md, start, end))
            return false;

        int interval = schedule.DateIntervalDays;
        if (interval <= 0) interval = 1;

        var startDate = new DateOnly(date.Year, start.Month, Math.Min(start.Day, DateTime.DaysInMonth(date.Year, start.Month)));
        if (startDate > date)
            startDate = new DateOnly(date.Year - 1, start.Month, Math.Min(start.Day, DateTime.DaysInMonth(date.Year - 1, start.Month)));

        int daysSinceStart = date.DayNumber - startDate.DayNumber;
        return daysSinceStart >= 0 && (daysSinceStart % interval) == 0;
    }

    static bool IsInMonthDayRange(MonthDay value, MonthDay start, MonthDay end) =>
        start <= end ? value >= start && value <= end : value >= start || value <= end;

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
