using Launcher.Core;

namespace Launcher.UI;

public partial class ScheduleEditForm : Form
{
    Schedule v;

    public ScheduleEditForm(Schedule schedule)
    {
        InitializeComponent();
        v = schedule;

        checkBoxEnable.Checked = v.Enable;

        // 時刻設定
        radioTimeSpecific.Checked = v.TimeType == ScheduleTimeType.SpecificTimes;
        radioTimeInterval.Checked = v.TimeType == ScheduleTimeType.Interval;
        FormsHelper.SetArray(listBoxTimes, v.Times);
        numTimeIntervalStartH.Value = v.TimeIntervalStart.Hour;
        numTimeIntervalStartM.Value = v.TimeIntervalStart.Minute;
        numTimeIntervalEndH.Value = v.TimeIntervalEnd.Hour;
        numTimeIntervalEndM.Value = v.TimeIntervalEnd.Minute;
        numTimeInterval.Value = v.TimeIntervalMinutes;

        // 日付設定
        radioDateWeekday.Checked = v.DateType == ScheduleDateType.Weekday;
        radioDateInterval.Checked = v.DateType == ScheduleDateType.DateInterval;
        numWeeksStartM.Value = v.WeeksStart.Month;
        numWeeksStartD.Value = v.WeeksStart.Day;
        numWeeksEndM.Value = v.WeeksEnd.Month;
        numWeeksEndD.Value = v.WeeksEnd.Day;
        checkMon.Checked = v.Weekdays[0];
        checkTue.Checked = v.Weekdays[1];
        checkWed.Checked = v.Weekdays[2];
        checkThu.Checked = v.Weekdays[3];
        checkFri.Checked = v.Weekdays[4];
        checkSat.Checked = v.Weekdays[5];
        checkSun.Checked = v.Weekdays[6];
        numDateIntervalStartM.Value = v.DateIntervalStart.Month;
        numDateIntervalStartD.Value = v.DateIntervalStart.Day;
        numDateIntervalEndM.Value = v.DateIntervalEnd.Month;
        numDateIntervalEndD.Value = v.DateIntervalEnd.Day;
        numDateInterval.Value = v.DateIntervalDays;

        UpdateTimePanelEnabled();
        UpdateDatePanelEnabled();
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        v.Enable = checkBoxEnable.Checked;

        v.TimeType = radioTimeSpecific.Checked ? ScheduleTimeType.SpecificTimes : ScheduleTimeType.Interval;
        v.Times = FormsHelper.GetArray<HourMinute>(listBoxTimes);
        v.TimeIntervalStart = new HourMinute((int)numTimeIntervalStartH.Value, (int)numTimeIntervalStartM.Value);
        v.TimeIntervalEnd = new HourMinute((int)numTimeIntervalEndH.Value, (int)numTimeIntervalEndM.Value);
        v.TimeIntervalMinutes = (int)numTimeInterval.Value;

        v.DateType = radioDateWeekday.Checked ? ScheduleDateType.Weekday : ScheduleDateType.DateInterval;
        v.WeeksStart = new MonthDay((int)numWeeksStartM.Value, (int)numWeeksStartD.Value);
        v.WeeksEnd = new MonthDay((int)numWeeksEndM.Value, (int)numWeeksEndD.Value);
        v.Weekdays = [checkMon.Checked, checkTue.Checked, checkWed.Checked, checkThu.Checked, checkFri.Checked, checkSat.Checked, checkSun.Checked];
        v.DateIntervalStart = new MonthDay((int)numDateIntervalStartM.Value, (int)numDateIntervalStartD.Value);
        v.DateIntervalEnd = new MonthDay((int)numDateIntervalEndM.Value, (int)numDateIntervalEndD.Value);
        v.DateIntervalDays = (int)numDateInterval.Value;
    }

    private void radioTimeType_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateTimePanelEnabled();
    }

    private void radioDateType_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateDatePanelEnabled();
    }

    private void UpdateTimePanelEnabled()
    {
        bool specific = radioTimeSpecific.Checked;
        panelTimeSpecific.Enabled = specific;
        panelTimeInterval.Enabled = !specific;
    }

    private void UpdateDatePanelEnabled()
    {
        bool weekday = radioDateWeekday.Checked;
        panelDateWeekday.Enabled = weekday;
        panelDateInterval.Enabled = !weekday;
    }

    private void buttonTimeAdd_Click(object? sender, EventArgs e)
    {
        var hm = new HourMinute((int)numTimeAddH.Value, (int)numTimeAddM.Value);
        FormsHelper.Insert(listBoxTimes, hm);
    }

    private void buttonTimeDelete_Click(object? sender, EventArgs e)
    {
        FormsHelper.RemoveSelected(listBoxTimes);
    }
}
