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
        comboBoxType.SelectedIndex = (int)v.Type;
        textBoxFileName.Text = v.FileName;
        textBoxParam.Text = v.Param;
        launchOptions.WindowStyle = v.Show;
        launchOptions.Priority = v.Priority;
        textBoxMessage.Text = v.Message;
        UpdateControlVisibility();
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        v.Enable = checkBoxEnable.Checked;
        v.Type = (SchedulerTaskType)comboBoxType.SelectedIndex;
        v.FileName = textBoxFileName.Text;
        v.Param = textBoxParam.Text;
        v.Show = launchOptions.WindowStyle;
        v.Priority = launchOptions.Priority;
        v.Message = textBoxMessage.Text;
    }

    private void buttonBrowse_Click(object? sender, EventArgs e)
    {
        CommandFileBrowser.Browse(this, textBoxFileName, openFileDialog1);
    }

    private void comboBoxType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateControlVisibility();
    }

    private void UpdateControlVisibility()
    {
        bool isExecute = comboBoxType.SelectedIndex == (int)SchedulerTaskType.Execute;

        label1.Visible = isExecute;
        textBoxFileName.Visible = isExecute;
        buttonBrowse.Visible = isExecute;
        label2.Visible = isExecute;
        textBoxParam.Visible = isExecute;
        launchOptions.Visible = isExecute;

        labelMessage.Visible = !isExecute;
        textBoxMessage.Visible = !isExecute;
    }
}
