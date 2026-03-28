using System.Xml.Serialization;
using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// スケジューラデータモデルのテスト
/// </summary>
public sealed class SchedulerDataTests
{
    #region HourMinute

    [Fact]
    public void HourMinute_ToMinutesと比較()
    {
        var hm1 = new HourMinute(9, 30);
        var hm2 = new HourMinute(10, 0);
        var hm3 = new HourMinute(9, 30);

        hm1.ToMinutes().Should().Be(570);
        hm2.ToMinutes().Should().Be(600);

        (hm1 < hm2).Should().BeTrue();
        (hm1 == hm3).Should().BeTrue();
        (hm1 != hm2).Should().BeTrue();
        hm1.ToString().Should().Be("09:30");
    }

    #endregion

    #region MonthDay

    [Fact]
    public void MonthDay_比較()
    {
        var md1 = new MonthDay(3, 15);
        var md2 = new MonthDay(4, 1);
        var md3 = new MonthDay(3, 15);

        (md1 < md2).Should().BeTrue();
        (md1 == md3).Should().BeTrue();
        md1.ToString().Should().Be("03/15");
    }

    #endregion

    #region SchedulerData シリアライズ

    [Fact]
    public void SchedulerData_ラウンドトリップでデフォルト値が保持される()
    {
        var original = new SchedulerData();
        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<SchedulerData>(xml);

        deserialized.Items.Should().BeEmpty();
        deserialized.LastCheckTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void SchedulerData_全プロパティがラウンドトリップで保持される()
    {
        var original = new SchedulerData
        {
            LastCheckTime = new DateTime(2025, 6, 15, 12, 30, 0),
            Items =
            [
                new SchedulerItem
                {
                    Enable = true,
                    Name = "バックアップ",
                    SleepTimeMs = 3000,
                    Schedules =
                    [
                        new Schedule
                        {
                            Enable = true,
                            TimeType = ScheduleTimeType.SpecificTimes,
                            Times = [new HourMinute(9, 0), new HourMinute(18, 0)],
                            DateType = ScheduleDateType.Weekday,
                            WeeksStart = new MonthDay(1, 1),
                            WeeksEnd = new MonthDay(12, 31),
                            Weekdays = [true, true, true, true, true, false, false],
                        },
                        new Schedule
                        {
                            Enable = false,
                            TimeType = ScheduleTimeType.Interval,
                            TimeIntervalStart = new HourMinute(8, 0),
                            TimeIntervalEnd = new HourMinute(20, 0),
                            TimeIntervalMinutes = 30,
                            DateType = ScheduleDateType.DateInterval,
                            DateIntervalStart = new MonthDay(4, 1),
                            DateIntervalEnd = new MonthDay(9, 30),
                            DateIntervalDays = 3,
                        },
                    ],
                    Tasks =
                    [
                        new SchedulerTask
                        {
                            Enable = true,
                            FileName = @"C:\backup\run.bat",
                            Param = "/silent",
                            Show = WindowStyle.Hidden,
                            Priority = ProcessPriorityLevel.BelowNormal,
                        },
                        new SchedulerTask
                        {
                            Enable = false,
                            FileName = "notepad.exe",
                            Show = WindowStyle.Maximized,
                        },
                    ],
                },
            ],
        };

        var xml = SerializeToString(original);
        var deserialized = DeserializeFromString<SchedulerData>(xml);

        deserialized.LastCheckTime.Should().Be(new DateTime(2025, 6, 15, 12, 30, 0));
        deserialized.Items.Should().HaveCount(1);

        var item = deserialized.Items[0];
        item.Enable.Should().BeTrue();
        item.Name.Should().Be("バックアップ");
        item.SleepTimeMs.Should().Be(3000);
        item.Schedules.Should().HaveCount(2);
        item.Tasks.Should().HaveCount(2);

        // Schedule 1: SpecificTimes + Weekday
        var sch1 = item.Schedules[0];
        sch1.Enable.Should().BeTrue();
        sch1.TimeType.Should().Be(ScheduleTimeType.SpecificTimes);
        sch1.Times.Should().Equal(new HourMinute(9, 0), new HourMinute(18, 0));
        sch1.DateType.Should().Be(ScheduleDateType.Weekday);
        sch1.Weekdays.Should().Equal(true, true, true, true, true, false, false);

        // Schedule 2: Interval + DateInterval
        var sch2 = item.Schedules[1];
        sch2.Enable.Should().BeFalse();
        sch2.TimeType.Should().Be(ScheduleTimeType.Interval);
        sch2.TimeIntervalStart.Should().Be(new HourMinute(8, 0));
        sch2.TimeIntervalEnd.Should().Be(new HourMinute(20, 0));
        sch2.TimeIntervalMinutes.Should().Be(30);
        sch2.DateType.Should().Be(ScheduleDateType.DateInterval);
        sch2.DateIntervalStart.Should().Be(new MonthDay(4, 1));
        sch2.DateIntervalEnd.Should().Be(new MonthDay(9, 30));
        sch2.DateIntervalDays.Should().Be(3);

        // Task 1
        var task1 = item.Tasks[0];
        task1.Enable.Should().BeTrue();
        task1.FileName.Should().Be(@"C:\backup\run.bat");
        task1.Param.Should().Be("/silent");
        task1.Show.Should().Be(WindowStyle.Hidden);
        task1.Priority.Should().Be(ProcessPriorityLevel.BelowNormal);

        // Task 2
        var task2 = item.Tasks[1];
        task2.Enable.Should().BeFalse();
        task2.FileName.Should().Be("notepad.exe");
        task2.Show.Should().Be(WindowStyle.Maximized);
    }

    #endregion

    #region Clone

    [Fact]
    public void SchedulerItem_Cloneはディープコピー()
    {
        var original = new SchedulerItem
        {
            Name = "テスト",
            Schedules = [new Schedule { Times = [new HourMinute(12, 0)] }],
            Tasks = [new SchedulerTask { FileName = "test.exe" }],
        };

        var clone = original.Clone();

        // 値が同じ
        clone.Name.Should().Be("テスト");
        clone.Schedules.Should().HaveCount(1);
        clone.Tasks.Should().HaveCount(1);

        // 参照が異なる (ディープコピー)
        clone.Schedules.Should().NotBeSameAs(original.Schedules);
        clone.Schedules[0].Should().NotBeSameAs(original.Schedules[0]);
        clone.Schedules[0].Times.Should().NotBeSameAs(original.Schedules[0].Times);
        clone.Tasks.Should().NotBeSameAs(original.Tasks);
        clone.Tasks[0].Should().NotBeSameAs(original.Tasks[0]);

        // 変更が影響しない
        clone.Name = "変更後";
        clone.Schedules[0].Times.Add(new HourMinute(18, 0));
        clone.Tasks[0].FileName = "changed.exe";
        original.Name.Should().Be("テスト");
        original.Schedules[0].Times.Should().HaveCount(1);
        original.Tasks[0].FileName.Should().Be("test.exe");
    }

    [Fact]
    public void Schedule_CloneはWeekdaysをディープコピー()
    {
        var original = new Schedule
        {
            Weekdays = [true, false, true, false, true, false, true],
        };

        var clone = original.Clone();
        clone.Weekdays[0] = false;

        original.Weekdays[0].Should().BeTrue();
    }

    #endregion

    // --- ヘルパー ---

    private static string SerializeToString<T>(T obj)
    {
        using var writer = new StringWriter();
        var serializer = new XmlSerializer(typeof(T));
        serializer.Serialize(writer, obj);
        return writer.ToString();
    }

    private static T DeserializeFromString<T>(string xml)
    {
        using var reader = new StringReader(xml);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(reader)!;
    }
}
