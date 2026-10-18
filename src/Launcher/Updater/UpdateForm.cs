using Launcher.Infrastructure;

namespace Launcher.Updater;

/// <summary>
/// 更新通知ダイアログ。
/// 「更新する」ボタン押下後はフォームを閉じずに進捗を表示し、バッチ起動後にプロセスを終了する。
/// </summary>
public sealed class UpdateForm : Form
{
    private Label labelMessage = null!;
    private Button buttonUpdate = null!;
    private Button buttonLater = null!;

    private readonly GitHubRelease _release;
    private readonly Action prepareShutdown;

    public UpdateForm(GitHubRelease release, Action prepareShutdown)
    {
        _release = release;
        this.prepareShutdown = prepareShutdown;
        InitializeComponents();
        labelMessage.Text = $"新しいバージョン {release.TagName} が利用可能です。";
    }

    private async void buttonUpdate_Click(object? sender, EventArgs e)
    {
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
            prepareShutdown();
            // バッチが起動された。プロセスを即座に終了してバッチに再起動を任せる。
            // Application.Exit()ではなくEnvironment.Exit()を使う。
            // Application.Exit()はモーダルダイアログ内から呼ぶとハングする場合がある。
            Environment.Exit(0);
        }
#pragma warning disable CA1031 // 更新処理は様々な例外が発生しうる
        catch (Exception ex)
        {
            DiagnosticLog.Error("Update", ex, "画面の案内に従う");
            MessageBox.Show(this, $"更新に失敗しました: {ex.Message}\r\n\r\nインストール済みのアプリは変更していません。\r\n{RetryGuidance}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.Cancel;
        }
#pragma warning restore CA1031
    }

    /// <summary>更新の確認や適用に失敗したときの再試行と代替手段の案内</summary>
    internal const string RetryGuidance = "時間をおいてタスクトレイの「ネットワーク更新」をやり直すか、https://github.com/ak110/lc/releases から最新版のZIPをダウンロードして、らんちゃのフォルダーへ上書きで展開してください。";

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
