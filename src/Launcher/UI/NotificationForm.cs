using System.Media;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// スケジューラーの通知ダイアログ。MessageBoxタスクの表示に使う。
/// DummyFormが追跡中の間はMainFormのauto-hideを抑制することで、
/// 通知表示中にユーザーがホットキーで主ウィンドウを開いてもレイアウトが維持される。
/// </summary>
public partial class NotificationForm : Form
{
    public NotificationForm(string title, string message)
    {
        InitializeComponent();

        Text = title;
        labelMessage.Text = message;

        // ラベルの内容に応じてフォームサイズを調整する。
        // 最大幅480pxで折り返し、最小クライアント幅280pxを確保する。
        var labelSize = labelMessage.GetPreferredSize(new Size(480, 0));
        labelMessage.Size = labelSize;

        int clientWidth = Math.Max(labelSize.Width + 32, 280);
        int clientHeight = labelSize.Height + buttonOk.Height + 48;
        ClientSize = new Size(clientWidth, clientHeight);
        buttonOk.Location = new Point(
            clientWidth - buttonOk.Width - 16,
            clientHeight - buttonOk.Height - 12);
    }

    /// <summary>
    /// 表示時に最前面化と通知音の再生を行う。
    /// スケジューラー発火はユーザーが別の作業をしている最中に起きるため、
    /// バックグラウンドからの確実なアクティブ化が必要になる。
    /// </summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        WindowHelper.ActivateForce(this);
        SystemSounds.Asterisk.Play();
    }
}
