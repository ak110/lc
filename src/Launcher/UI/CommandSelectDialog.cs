using Launcher.Core;

namespace Launcher.UI;

/// <summary>
/// コマンド選択ダイアログ
/// </summary>
internal sealed class CommandSelectDialog : Form
{
    readonly ListView listView;
    public Command? SelectedCommand { get; private set; }

    public CommandSelectDialog(CommandList commandList)
    {
        Text = "コマンドの選択";
        ClientSize = new Size(400, 350);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;

        listView = new ListView
        {
            Dock = DockStyle.Top,
            Height = 300,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
        };
        listView.Columns.Add("名前", 150);
        listView.Columns.Add("ファイル名", 230);
        listView.DoubleClick += (s, e) =>
        {
            if (listView.SelectedItems.Count == 1)
            {
                SelectedCommand = (Command)listView.SelectedItems[0].Tag!;
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        foreach (var cmd in commandList.Commands)
        {
            var item = new ListViewItem(cmd.Name ?? "") { Tag = cmd };
            item.SubItems.Add(cmd.FileName ?? "");
            listView.Items.Add(item);
        }

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(230, 315),
            Width = 75,
        };
        ok.Click += (s, e) =>
        {
            if (listView.SelectedItems.Count == 1)
            {
                SelectedCommand = (Command)listView.SelectedItems[0].Tag!;
            }
        };
        var cancel = new Button
        {
            Text = "キャンセル",
            DialogResult = DialogResult.Cancel,
            Location = new Point(310, 315),
            Width = 75,
        };

        Controls.Add(listView);
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
