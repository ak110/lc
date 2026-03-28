using Launcher.Core;

namespace Launcher.UI;

public partial class SchedulerTaskForm : Form
{
    SchedulerTask v;

    public SchedulerTaskForm(SchedulerTask task)
    {
        InitializeComponent();
        v = task;

        checkBoxEnable.Checked = v.Enable;
        textBoxFileName.Text = v.FileName;
        textBoxParam.Text = v.Param;
        new Radios(groupBoxShow, 6).Value = (int)v.Show;
        new Radios(groupBoxPriority, 6).Value = (int)v.Priority;
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        v.Enable = checkBoxEnable.Checked;
        v.FileName = textBoxFileName.Text;
        v.Param = textBoxParam.Text;
        v.Show = (WindowStyle)new Radios(groupBoxShow, 6).Value;
        v.Priority = (ProcessPriorityLevel)new Radios(groupBoxPriority, 6).Value;
    }

    private void buttonBrowse_Click(object? sender, EventArgs e)
    {
        if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            textBoxFileName.Text = openFileDialog1.FileName;
        }
    }
}
