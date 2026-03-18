#nullable disable
using System.Windows.Forms;

namespace Launcher.Updater;

/// <summary>
/// 更新通知ダイアログ
/// </summary>
public class UpdateForm : Form {
    private Label labelMessage;
    private Button buttonUpdate;
    private Button buttonSkip;
    private Button buttonLater;

    private readonly GitHubRelease _release;

    public UpdateForm(GitHubRelease release) {
        _release = release;
        InitializeComponents();
        labelMessage.Text = $"新しいバージョン {release.TagName} が利用可能です。\n\n{release.Name}";
    }

    private void InitializeComponents() {
        Text = "更新通知";
        Size = new System.Drawing.Size(400, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        labelMessage = new Label {
            Location = new System.Drawing.Point(12, 12),
            Size = new System.Drawing.Size(360, 80),
            AutoSize = false,
        };

        buttonUpdate = new Button {
            Text = "ダウンロードページを開く",
            Location = new System.Drawing.Point(12, 120),
            Size = new System.Drawing.Size(160, 30),
            DialogResult = DialogResult.OK,
        };
        buttonUpdate.Click += (s, e) => GitHubUpdateClient.OpenReleasePage(_release);

        buttonSkip = new Button {
            Text = "スキップ",
            Location = new System.Drawing.Point(180, 120),
            Size = new System.Drawing.Size(80, 30),
            DialogResult = DialogResult.Ignore,
        };

        buttonLater = new Button {
            Text = "後で",
            Location = new System.Drawing.Point(268, 120),
            Size = new System.Drawing.Size(80, 30),
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange(new Control[] { labelMessage, buttonUpdate, buttonSkip, buttonLater });
        AcceptButton = buttonUpdate;
        CancelButton = buttonLater;
    }
}
