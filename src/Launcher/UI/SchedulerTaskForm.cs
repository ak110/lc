using Launcher.Core;
using Launcher.Infrastructure;

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
        new Radios(groupBoxShow, 6).Value = (int)v.Show;
        new Radios(groupBoxPriority, 6).Value = (int)v.Priority;
        textBoxMessage.Text = v.Message;
        UpdateControlVisibility();
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        v.Enable = checkBoxEnable.Checked;
        v.Type = (SchedulerTaskType)comboBoxType.SelectedIndex;
        v.FileName = textBoxFileName.Text;
        v.Param = textBoxParam.Text;
        v.Show = (WindowStyle)new Radios(groupBoxShow, 6).Value;
        v.Priority = (ProcessPriorityLevel)new Radios(groupBoxPriority, 6).Value;
        v.Message = textBoxMessage.Text;
    }

    private void buttonBrowse_Click(object? sender, EventArgs e)
    {
        // PATH解決済みパスをダイアログの起点に設定する。
        // bare name (例: "notepad.exe") をアイコン表示と同じ解決順で扱い、操作時の挙動を揃える。
        string resolved = FileHelper.ResolveCommandPath(textBoxFileName.Text);
        if (File.Exists(resolved))
        {
            openFileDialog1.FileName = resolved;
            string? dir = Path.GetDirectoryName(resolved);
            if (!string.IsNullOrEmpty(dir))
            {
                openFileDialog1.InitialDirectory = dir;
            }
        }
        if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            textBoxFileName.Text = openFileDialog1.FileName;
        }
    }

    private void comboBoxType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateControlVisibility();
    }

    /// <summary>
    /// タスク種類に応じてコントロールの表示/非表示を切り替える。
    /// </summary>
    private void UpdateControlVisibility()
    {
        bool isExecute = comboBoxType.SelectedIndex == (int)SchedulerTaskType.Execute;

        // ファイル実行用コントロール
        label1.Visible = isExecute;
        textBoxFileName.Visible = isExecute;
        buttonBrowse.Visible = isExecute;
        label2.Visible = isExecute;
        textBoxParam.Visible = isExecute;
        groupBoxShow.Visible = isExecute;
        groupBoxPriority.Visible = isExecute;

        // メッセージ表示用コントロール
        labelMessage.Visible = !isExecute;
        textBoxMessage.Visible = !isExecute;
    }
}
