using System.Windows.Forms;

namespace Launcher.Updater;

/// <summary>
/// 更新通知ダイアログ
/// </summary>
public class UpdateForm : Form
{
    private Label labelMessage = null!;
    private Button buttonUpdate = null!;
    private Button buttonLater = null!;

    private readonly GitHubRelease _release;

    public UpdateForm(GitHubRelease release)
    {
        _release = release;
        InitializeComponents();
        labelMessage.Text = $"新しいバージョン {release.TagName} が利用可能です。\n\n{release.Name}";
    }

    /// <summary>
    /// 「更新する」ボタン押下時に更新を実行する
    /// </summary>
    public async Task PerformUpdateAsync()
    {
        var progress = new Progress<string>(message =>
        {
            if (!IsDisposed)
            {
                labelMessage.Text = message;
            }
        });
        await UpdatePerformer.PerformUpdateAsync(_release, progress);
    }

    private void InitializeComponents()
    {
        Text = "更新通知";
        Size = new System.Drawing.Size(400, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        labelMessage = new Label
        {
            Location = new System.Drawing.Point(12, 12),
            Size = new System.Drawing.Size(360, 80),
            AutoSize = false,
        };

        buttonUpdate = new Button
        {
            Text = "更新する",
            Location = new System.Drawing.Point(12, 120),
            Size = new System.Drawing.Size(160, 30),
            DialogResult = DialogResult.OK,
        };

        buttonLater = new Button
        {
            Text = "後で",
            Location = new System.Drawing.Point(268, 120),
            Size = new System.Drawing.Size(80, 30),
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange(new Control[] { labelMessage, buttonUpdate, buttonLater });
        AcceptButton = buttonUpdate;
        CancelButton = buttonLater;
    }
}
