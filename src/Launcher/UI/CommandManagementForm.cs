using Launcher.Core;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// コマンド管理画面
/// </summary>
public partial class CommandManagementForm : Form
{
    readonly DummyForm owner;
    readonly AsyncIconLoader iconLoader = new AsyncIconLoader();
    ImageList? imageList;

    public CommandManagementForm(DummyForm owner)
    {
        InitializeComponent();
        this.owner = owner;

        // アイコン表示の設定
        imageList = new ImageList
        {
            ImageSize = new Size(16, 16),
            ColorDepth = ColorDepth.Depth32Bit,
        };
        listView1.SmallImageList = imageList;
        iconLoader.IconLoaded += IconLoader_IconLoaded;

        LoadCommands();
    }

    /// <summary>
    /// コマンドリストをListViewに表示
    /// </summary>
    private void LoadCommands()
    {
        listView1.BeginUpdate();
        listView1.Items.Clear();
        iconLoader.Clear();
        imageList?.Images.Clear();

        foreach (var cmd in owner.CommandList.Commands)
        {
            var item = new ListViewItem(cmd.Name ?? "")
            {
                Tag = cmd,
            };
            item.SubItems.Add(cmd.FileName ?? "");
            item.SubItems.Add(cmd.Param ?? "");
            item.SubItems.Add(cmd.WorkDir ?? "");
            listView1.Items.Add(item);

            // アイコン非同期読み込み
            if (!string.IsNullOrEmpty(cmd.FileName))
            {
                iconLoader.Load(cmd.FileName, true, item);
            }
        }
        listView1.EndUpdate();
        UpdateButtonState();
    }

    private void IconLoader_IconLoaded(object? sender, IconLoadedEventArgs e)
    {
        if (e.Generation != iconLoader.Generation) return;
        if (!IsHandleCreated) return;

        BeginInvoke(() =>
        {
            if (IsDisposed) return;
            if (e.Icon == null) return;

            var item = (ListViewItem)e.Arg!;
            if (item.ListView == null) return;

            int index = imageList!.Images.Count;
            imageList.Images.Add(e.Icon);
            item.ImageIndex = index;
        });
    }

    private void UpdateButtonState()
    {
        int count = listView1.SelectedIndices.Count;
        buttonEdit.Enabled = count == 1;
        buttonDelete.Enabled = count > 0;
    }

    private void listView1_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateButtonState();
    }

    private void listView1_DoubleClick(object? sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count == 1)
        {
            EditSelectedCommand();
        }
    }

    private void buttonEdit_Click(object? sender, EventArgs e)
    {
        EditSelectedCommand();
    }

    private void EditSelectedCommand()
    {
        if (listView1.SelectedItems.Count != 1) return;

        var item = listView1.SelectedItems[0];
        var cmd = (Command)item.Tag!;

        using (var form = new EditCommandForm(cmd))
        {
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // EditCommandFormが直接cmdを更新するので、表示を更新
                item.Text = cmd.Name ?? "";
                item.SubItems[1].Text = cmd.FileName ?? "";
                item.SubItems[2].Text = cmd.Param ?? "";
                item.SubItems[3].Text = cmd.WorkDir ?? "";
                SaveAndApply();
            }
        }
    }

    private void buttonDelete_Click(object? sender, EventArgs e)
    {
        int count = listView1.SelectedItems.Count;
        if (count <= 0) return;

        string msg = count == 1
            ? $"「{listView1.SelectedItems[0].Text}」を削除しますか？"
            : $"{count}件のコマンドを削除しますか？";

        if (MessageBox.Show(this, msg, "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        // 選択されたコマンドを削除
        var toRemove = new List<Command>();
        foreach (ListViewItem item in listView1.SelectedItems)
        {
            toRemove.Add((Command)item.Tag!);
        }
        foreach (var cmd in toRemove)
        {
            owner.CommandList.Commands.Remove(cmd);
        }

        SaveAndApply();
        LoadCommands();
    }

    private void SaveAndApply()
    {
        owner.CommandList.Serialize(".cmd.cfg");
        // MainFormの表示を更新
        owner.RefreshMainForm();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            iconLoader.Clear();
            imageList?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
