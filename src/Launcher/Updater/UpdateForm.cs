using System.Windows.Forms;

namespace Launcher.Updater;

/// <summary>
/// 更新通知ダイアログ。
/// 「更新する」ボタン押下後はフォームを閉じずに進捗を表示し、更新完了後にApplication.Exit()する。
/// </summary>
public sealed class UpdateForm : Form
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

    private async void buttonUpdate_Click(object? sender, EventArgs e)
    {
        // ボタンを無効化してフォームを閉じられないようにする
        buttonUpdate.Enabled = false;
        buttonLater.Enabled = false;
        ControlBox = false;

        try
        {
            var progress = new Progress<string>(message =>
            {
                if (!IsDisposed)
                {
                    labelMessage.Text = message;
                }
            });
            await UpdatePerformer.PerformUpdateAsync(_release, progress);
            // バッチが起動された。フォームを閉じてApplication.Exit()を呼び出し側に任せる
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or System.IO.IOException or InvalidOperationException)
        {
            System.Diagnostics.Debug.WriteLine($"更新失敗: {ex.Message}");
            MessageBox.Show(this, $"更新に失敗しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            // エラー後はフォームを閉じる
            DialogResult = DialogResult.Cancel;
        }
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
        };
        buttonUpdate.Click += buttonUpdate_Click;

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
