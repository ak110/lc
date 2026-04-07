using Launcher.Core;

namespace Launcher.UI;

/// <summary>
/// 1つのSchedulerItemを編集するフォーム
/// </summary>
public partial class SchedulerItemForm : Form
{
    SchedulerItem v;

    public SchedulerItemForm(SchedulerItem item)
    {
        InitializeComponent();
        v = item;

        checkBoxEnable.Checked = v.Enable;
        textBoxName.Text = v.Name;
        numSleepTime.Value = v.SleepTimeMs;
        FormsHelper.SetArray(listBoxSchedules, v.Schedules);
        FormsHelper.SetArray(listBoxTasks, v.Tasks);
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        v.Enable = checkBoxEnable.Checked;
        v.Name = textBoxName.Text;
        v.SleepTimeMs = (int)numSleepTime.Value;
        v.Schedules = FormsHelper.GetArray<Schedule>(listBoxSchedules);
        v.Tasks = FormsHelper.GetArray<SchedulerTask>(listBoxTasks);
    }

    // --- スケジュール操作 ---

    private void buttonScheduleAdd_Click(object? sender, EventArgs e)
    {
        var schedule = new Schedule();
        using var form = new ScheduleEditForm(schedule);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            FormsHelper.Insert(listBoxSchedules, schedule);
        }
    }

    private void buttonScheduleClone_Click(object? sender, EventArgs e)
    {
        if (listBoxSchedules.SelectedItem is Schedule selected)
        {
            if (MessageBox.Show(this, "選択中のスケジュールを複製しますか？", Text,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            FormsHelper.Insert(listBoxSchedules, selected.Clone());
        }
    }

    private void buttonScheduleEdit_Click(object? sender, EventArgs e)
    {
        EditSelectedSchedule();
    }

    private void buttonScheduleDelete_Click(object? sender, EventArgs e)
    {
        if (listBoxSchedules.SelectedItem is not null)
        {
            if (MessageBox.Show(this, "選択中のスケジュールを削除しますか？", Text,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        }
        FormsHelper.RemoveSelected(listBoxSchedules);
    }

    private void listBoxSchedules_DoubleClick(object? sender, EventArgs e)
    {
        EditSelectedSchedule();
    }

    private void EditSelectedSchedule()
    {
        if (listBoxSchedules.SelectedItem is Schedule selected)
        {
            using var form = new ScheduleEditForm(selected);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // selectedは直接編集済み。表示を更新。
                int index = listBoxSchedules.SelectedIndex;
                listBoxSchedules.Items[index] = selected;
            }
        }
    }

    // --- タスク操作 ---

    private void buttonTaskAdd_Click(object? sender, EventArgs e)
    {
        var task = new SchedulerTask();
        using var form = new SchedulerTaskForm(task);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            FormsHelper.Insert(listBoxTasks, task);
        }
    }

    private void buttonTaskClone_Click(object? sender, EventArgs e)
    {
        if (listBoxTasks.SelectedItem is SchedulerTask selected)
        {
            if (MessageBox.Show(this, "選択中のタスクを複製しますか？", Text,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            FormsHelper.Insert(listBoxTasks, selected.Clone());
        }
    }

    private void buttonTaskEdit_Click(object? sender, EventArgs e)
    {
        EditSelectedTask();
    }

    private void buttonTaskUp_Click(object? sender, EventArgs e)
    {
        FormsHelper.UpSelected(listBoxTasks);
    }

    private void buttonTaskDown_Click(object? sender, EventArgs e)
    {
        FormsHelper.DownSelected(listBoxTasks);
    }

    private void buttonTaskDelete_Click(object? sender, EventArgs e)
    {
        if (listBoxTasks.SelectedItem is not null)
        {
            if (MessageBox.Show(this, "選択中のタスクを削除しますか？", Text,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        }
        FormsHelper.RemoveSelected(listBoxTasks);
    }

    private void buttonTaskTest_Click(object? sender, EventArgs e)
    {
        // 現在のタスク一覧でテスト実行
        var item = new SchedulerItem
        {
            Enable = true,
            SleepTimeMs = (int)numSleepTime.Value,
            Tasks = FormsHelper.GetArray<SchedulerTask>(listBoxTasks),
        };
        SchedulerPresenter.ExecuteItemTasks(item);
    }

    private void listBoxTasks_DoubleClick(object? sender, EventArgs e)
    {
        EditSelectedTask();
    }

    private void EditSelectedTask()
    {
        if (listBoxTasks.SelectedItem is SchedulerTask selected)
        {
            using var form = new SchedulerTaskForm(selected);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                int index = listBoxTasks.SelectedIndex;
                listBoxTasks.Items[index] = selected;
            }
        }
    }
}
