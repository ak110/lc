using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

public enum ScheduleTimeType
{
    [XmlEnum("0")] SpecificTimes = 0,
    [XmlEnum("1")] Interval = 1,
}

public enum ScheduleDateType
{
    [XmlEnum("0")] Weekday = 0,
    [XmlEnum("1")] DateInterval = 1,
}

public struct HourMinute : IComparable<HourMinute>, IEquatable<HourMinute>, ICloneable
{
    public int Hour { get; set; }
    public int Minute { get; set; }

    public HourMinute() { }
    public HourMinute(int hour, int minute) { Hour = hour; Minute = minute; }

    public int ToMinutes() => Hour * 60 + Minute;

    public int CompareTo(HourMinute other) => ToMinutes().CompareTo(other.ToMinutes());

    public override string ToString() => $"{Hour:D2}:{Minute:D2}";

    public bool Equals(HourMinute other) => Hour == other.Hour && Minute == other.Minute;
    public override bool Equals(object? obj) => obj is HourMinute other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Hour, Minute);
    public static bool operator ==(HourMinute left, HourMinute right) => left.Equals(right);
    public static bool operator !=(HourMinute left, HourMinute right) => !left.Equals(right);

    public object Clone() => this;
}

public struct MonthDay : IComparable<MonthDay>, IEquatable<MonthDay>
{
    public int Month { get; set; }
    public int Day { get; set; }

    public MonthDay() { }
    public MonthDay(int month, int day) { Month = month; Day = day; }

    public int ToDayNumber() => Month * 32 + Day;

    public int CompareTo(MonthDay other) => ToDayNumber().CompareTo(other.ToDayNumber());

    public override string ToString() => $"{Month:D2}/{Day:D2}";

    public bool Equals(MonthDay other) => Month == other.Month && Day == other.Day;
    public override bool Equals(object? obj) => obj is MonthDay other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Month, Day);
    public static bool operator ==(MonthDay left, MonthDay right) => left.Equals(right);
    public static bool operator !=(MonthDay left, MonthDay right) => !left.Equals(right);
    public static bool operator <=(MonthDay left, MonthDay right) => left.CompareTo(right) <= 0;
    public static bool operator >=(MonthDay left, MonthDay right) => left.CompareTo(right) >= 0;
}

public enum SchedulerTaskType
{
    [XmlEnum("0")] Execute = 0,
    [XmlEnum("1")] BalloonTip = 1,
    [XmlEnum("2")] MessageBox = 2,
}

public sealed class SchedulerTask : ICloneable
{
    public bool Enable { get; set; } = true;
    public SchedulerTaskType Type { get; set; } = SchedulerTaskType.Execute;

    public string FileName { get; set; } = string.Empty;
    public string Param { get; set; } = string.Empty;
    public WindowStyle Show { get; set; } = WindowStyle.Normal;
    public ProcessPriorityLevel Priority { get; set; } = ProcessPriorityLevel.Normal;

    public string Message { get; set; } = string.Empty;

    public SchedulerTask Clone() => (SchedulerTask)MemberwiseClone();
    object ICloneable.Clone() => Clone();

    public override string ToString()
    {
        string status = Enable ? "● " : "× ";
        return Type switch
        {
            SchedulerTaskType.BalloonTip => $"{status}[通知] {Message}",
            SchedulerTaskType.MessageBox => $"{status}[メッセージ] {Message}",
            _ => $"{status}{FileName}",
        };
    }
}

public sealed class Schedule : ICloneable
{
    public bool Enable { get; set; } = true;

    public ScheduleTimeType TimeType { get; set; } = ScheduleTimeType.SpecificTimes;
    public List<HourMinute> Times { get; set; } = [];
    public HourMinute TimeIntervalStart { get; set; }
    public HourMinute TimeIntervalEnd { get; set; }
    public int TimeIntervalMinutes { get; set; } = 60;

    public ScheduleDateType DateType { get; set; } = ScheduleDateType.Weekday;
    public MonthDay WeeksStart { get; set; } = new(1, 1);
    public MonthDay WeeksEnd { get; set; } = new(12, 31);
    public bool[] Weekdays { get; set; } = [true, true, true, true, true, true, true];
    public MonthDay DateIntervalStart { get; set; } = new(1, 1);
    public MonthDay DateIntervalEnd { get; set; } = new(12, 31);
    public int DateIntervalDays { get; set; } = 1;

    public Schedule Clone()
    {
        var copy = (Schedule)MemberwiseClone();
        copy.Times = new List<HourMinute>(Times);
        copy.Weekdays = (bool[])Weekdays.Clone();
        return copy;
    }
    object ICloneable.Clone() => Clone();

    public override string ToString()
    {
        string status = Enable ? "● " : "× ";
        string time = TimeType switch
        {
            ScheduleTimeType.SpecificTimes => Times.Count > 0
                ? string.Join(",", Times)
                : "(未設定)",
            ScheduleTimeType.Interval =>
                $"{TimeIntervalStart}～{TimeIntervalEnd} 毎{TimeIntervalMinutes}分",
            _ => "",
        };
        string date = DateType switch
        {
            ScheduleDateType.Weekday => FormatWeekdays(),
            ScheduleDateType.DateInterval =>
                $"{DateIntervalStart}～{DateIntervalEnd} 毎{DateIntervalDays}日",
            _ => "",
        };
        return $"{status}{time} / {date}";
    }

    private string FormatWeekdays()
    {
        string[] names = ["月", "火", "水", "木", "金", "土", "日"];
        var days = new List<string>();
        for (int i = 0; i < 7; i++)
        {
            if (i < Weekdays.Length && Weekdays[i])
                days.Add(names[i]);
        }
        return days.Count > 0 ? string.Join(",", days) : "(未設定)";
    }
}

public sealed class SchedulerItem : ICloneable
{
    public string Id { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public int SleepTimeMs { get; set; } = 2000;
    public List<Schedule> Schedules { get; set; } = [];
    public List<SchedulerTask> Tasks { get; set; } = [];

    public static string NewId() => Guid.NewGuid().ToString("N");

    public SchedulerItem Clone()
    {
        var copy = (SchedulerItem)MemberwiseClone();
        copy.Schedules = Schedules.Select(s => s.Clone()).ToList();
        copy.Tasks = Tasks.Select(t => t.Clone()).ToList();
        return copy;
    }

    public SchedulerItem CloneAsNew()
    {
        var copy = Clone();
        copy.Id = NewId();
        return copy;
    }
    object ICloneable.Clone() => Clone();

    public override string ToString()
    {
        string status = Enable ? "● " : "× ";
        return $"{status}{Name}";
    }
}

public sealed class SchedulerData : ConfigStore
{
    public List<SchedulerItem> Items { get; set; } = [];

    public bool EnsureIds()
    {
        var seen = new HashSet<string>();
        bool changed = false;
        foreach (var item in Items)
        {
            if (string.IsNullOrEmpty(item.Id) || !seen.Add(item.Id))
            {
                item.Id = SchedulerItem.NewId();
                seen.Add(item.Id);
                changed = true;
            }
        }
        return changed;
    }

    public bool Save(Action<ConfigSaveFailure> notify, string? baseName = null) => Store.Save(this, notify, baseName);

    public static ConfigLoadResult<SchedulerData> Load(string? baseName = null) => Store.Load(baseName);

    public static ConfigFile<SchedulerData> Store { get; } = new(".sch.cfg");
}
