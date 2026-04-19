using Launcher.Core;

namespace Launcher.UI;

/// <summary>
/// スケジューラー設定のトップレベルフォーム。SchedulerItemのリストを編集する。
/// </summary>
public partial class SchedulerConfigForm : Form
{
    SchedulerData data;
    readonly Action<string, string>? showBalloonTip;
    readonly Action<string, string>? showMessageBox;

    /// <summary>
    /// 編集結果
    /// </summary>
    public SchedulerData Value => data;

    public SchedulerConfigForm(
        SchedulerData original,
        Action<string, string>? showBalloonTip = null,
        Action<string, string>? showMessageBox = null)
    {
        InitializeComponent();
        this.showBalloonTip = showBalloonTip;
        this.showMessageBox = showMessageBox;
        // ディープコピーしてキャンセル対応
        data = new SchedulerData
        {
            Items = original.Items.Select(i => i.Clone()).ToList(),
        };
        FormsHelper.SetArray(listBoxItems, data.Items);
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        data.Items = FormsHelper.GetArray<SchedulerItem>(listBoxItems);
    }

    private void buttonAdd_Click(object? sender, EventArgs e)
    {
        var item = new SchedulerItem { Name = "新規アイテム" };
        using var form = new SchedulerItemForm(item, showBalloonTip, showMessageBox);
        if (form.ShowDialogOver(this) == DialogResult.OK)
        {
            FormsHelper.Insert(listBoxItems, item);
        }
    }

    private void buttonClone_Click(object? sender, EventArgs e)
    {
        if (listBoxItems.SelectedItem is SchedulerItem selected)
        {
            if (MessageBox.Show(this, $"「{selected.Name}」を複製しますか？", Text,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            FormsHelper.Insert(listBoxItems, selected.Clone());
        }
    }

    private void buttonEdit_Click(object? sender, EventArgs e)
    {
        EditSelectedItem();
    }

    private void buttonUp_Click(object? sender, EventArgs e)
    {
        FormsHelper.UpSelected(listBoxItems);
    }

    private void buttonDown_Click(object? sender, EventArgs e)
    {
        FormsHelper.DownSelected(listBoxItems);
    }

    private void buttonDelete_Click(object? sender, EventArgs e)
    {
        if (listBoxItems.SelectedItem is SchedulerItem selected)
        {
            if (MessageBox.Show(this, $"「{selected.Name}」を削除しますか？", Text,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        }
        FormsHelper.RemoveSelected(listBoxItems);
    }

    private void listBoxItems_DoubleClick(object? sender, EventArgs e)
    {
        EditSelectedItem();
    }

    private void EditSelectedItem()
    {
        if (listBoxItems.SelectedItem is SchedulerItem selected)
        {
            using var form = new SchedulerItemForm(selected, showBalloonTip, showMessageBox);
            if (form.ShowDialogOver(this) == DialogResult.OK)
            {
                int index = listBoxItems.SelectedIndex;
                listBoxItems.Items[index] = selected;
            }
        }
    }
}
