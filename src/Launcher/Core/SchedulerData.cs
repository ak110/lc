using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

// --- Enums ---

/// <summary>
/// スケジュールの時刻指定方法
/// </summary>
public enum ScheduleTimeType
{
    [XmlEnum("0")] SpecificTimes = 0,
    [XmlEnum("1")] Interval = 1,
}

/// <summary>
/// スケジュールの日付指定方法
/// </summary>
public enum ScheduleDateType
{
    [XmlEnum("0")] Weekday = 0,
    [XmlEnum("1")] DateInterval = 1,
}

// --- Value Types ---

/// <summary>
/// 時:分を表す値型
/// </summary>
[Serializable]
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
    public static bool operator <(HourMinute left, HourMinute right) => left.CompareTo(right) < 0;
    public static bool operator >(HourMinute left, HourMinute right) => left.CompareTo(right) > 0;
    public static bool operator <=(HourMinute left, HourMinute right) => left.CompareTo(right) <= 0;
    public static bool operator >=(HourMinute left, HourMinute right) => left.CompareTo(right) >= 0;

    public object Clone() => this;
}

/// <summary>
/// 月/日を表す値型
/// </summary>
[Serializable]
public struct MonthDay : IComparable<MonthDay>, IEquatable<MonthDay>
{
    public int Month { get; set; }
    public int Day { get; set; }

    public MonthDay() { }
    public MonthDay(int month, int day) { Month = month; Day = day; }

    /// <summary>
    /// 比較用の数値。月×32+日で一意にソートできる。
    /// </summary>
    public int ToDayNumber() => Month * 32 + Day;

    public int CompareTo(MonthDay other) => ToDayNumber().CompareTo(other.ToDayNumber());

    public override string ToString() => $"{Month:D2}/{Day:D2}";

    public bool Equals(MonthDay other) => Month == other.Month && Day == other.Day;
    public override bool Equals(object? obj) => obj is MonthDay other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Month, Day);
    public static bool operator ==(MonthDay left, MonthDay right) => left.Equals(right);
    public static bool operator !=(MonthDay left, MonthDay right) => !left.Equals(right);
    public static bool operator <(MonthDay left, MonthDay right) => left.CompareTo(right) < 0;
    public static bool operator >(MonthDay left, MonthDay right) => left.CompareTo(right) > 0;
    public static bool operator <=(MonthDay left, MonthDay right) => left.CompareTo(right) <= 0;
    public static bool operator >=(MonthDay left, MonthDay right) => left.CompareTo(right) >= 0;
}

// --- Domain Models ---

/// <summary>
/// スケジューラーのタスク (実行するプログラムの定義)
/// </summary>
[Serializable]
public sealed class SchedulerTask : ICloneable
{
    public bool Enable { get; set; } = true;
    public string FileName { get; set; } = string.Empty;
    public string Param { get; set; } = string.Empty;
    public WindowStyle Show { get; set; } = WindowStyle.Normal;
    public ProcessPriorityLevel Priority { get; set; } = ProcessPriorityLevel.Normal;

    public SchedulerTask Clone() => (SchedulerTask)MemberwiseClone();
    object ICloneable.Clone() => Clone();

    public override string ToString()
    {
        string status = Enable ? "● " : "× ";
        return $"{status}{FileName}";
    }
}

/// <summary>
/// スケジュール条件 (いつ実行するかの定義)
/// </summary>
[Serializable]
public sealed class Schedule : ICloneable
{
    public bool Enable { get; set; } = true;

    // --- 時刻設定 ---
    public ScheduleTimeType TimeType { get; set; } = ScheduleTimeType.SpecificTimes;
    /// <summary>指定時刻リスト (TimeType == SpecificTimes)</summary>
    public List<HourMinute> Times { get; set; } = [];
    /// <summary>間隔の開始時刻 (TimeType == Interval)</summary>
    public HourMinute TimeIntervalStart { get; set; }
    /// <summary>間隔の終了時刻 (TimeType == Interval)</summary>
    public HourMinute TimeIntervalEnd { get; set; }
    /// <summary>間隔 (分) (TimeType == Interval)</summary>
    public int TimeIntervalMinutes { get; set; } = 60;

    // --- 日付設定 ---
    public ScheduleDateType DateType { get; set; } = ScheduleDateType.Weekday;
    /// <summary>曜日フィルタの開始月日 (DateType == Weekday)</summary>
    public MonthDay WeeksStart { get; set; } = new(1, 1);
    /// <summary>曜日フィルタの終了月日 (DateType == Weekday)</summary>
    public MonthDay WeeksEnd { get; set; } = new(12, 31);
    /// <summary>曜日フラグ [0]=月 ... [6]=日 (DateType == Weekday)</summary>
    public bool[] Weekdays { get; set; } = [true, true, true, true, true, true, true];
    /// <summary>日数間隔の開始月日 (DateType == DateInterval)</summary>
    public MonthDay DateIntervalStart { get; set; } = new(1, 1);
    /// <summary>日数間隔の終了月日 (DateType == DateInterval)</summary>
    public MonthDay DateIntervalEnd { get; set; } = new(12, 31);
    /// <summary>日数間隔 (DateType == DateInterval)</summary>
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

/// <summary>
/// スケジューラーのアイテム (スケジュール条件+タスクのセット)
/// </summary>
[Serializable]
public sealed class SchedulerItem : ICloneable
{
    public bool Enable { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    /// <summary>タスク間の待機時間 (ミリ秒)</summary>
    public int SleepTimeMs { get; set; } = 2000;
    public List<Schedule> Schedules { get; set; } = [];
    public List<SchedulerTask> Tasks { get; set; } = [];

    public SchedulerItem Clone()
    {
        var copy = (SchedulerItem)MemberwiseClone();
        copy.Schedules = Schedules.Select(s => s.Clone()).ToList();
        copy.Tasks = Tasks.Select(t => t.Clone()).ToList();
        return copy;
    }
    object ICloneable.Clone() => Clone();

    public override string ToString()
    {
        string status = Enable ? "● " : "× ";
        return $"{status}{Name}";
    }
}

// --- Top-level ConfigStore ---

/// <summary>
/// スケジューラーの設定データ。らんちゃ.sch.cfg に永続化される。
/// </summary>
public sealed class SchedulerData : ConfigStore
{
    public List<SchedulerItem> Items { get; set; } = [];

    public void Serialize()
    {
        Serialize(".sch.cfg");
    }

    public static SchedulerData Deserialize()
    {
        try
        {
            return Deserialize<SchedulerData>(".sch.cfg");
        }
        catch (InvalidOperationException)
        {
            return new SchedulerData();
        }
        catch (XmlException)
        {
            return new SchedulerData();
        }
        catch (IOException)
        {
            return new SchedulerData();
        }
    }
}
