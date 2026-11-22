using System.Media;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

public partial class NotificationForm : Form
{
    public NotificationForm(string title, string message)
    {
        InitializeComponent();

        Text = title;
        labelMessage.Text = message;

        var labelSize = labelMessage.GetPreferredSize(new Size(480, 0));
        labelMessage.Size = labelSize;

        int clientWidth = Math.Max(labelSize.Width + 32, 280);
        int clientHeight = labelSize.Height + buttonOk.Height + 48;
        ClientSize = new Size(clientWidth, clientHeight);
        buttonOk.Location = new Point(
            clientWidth - buttonOk.Width - 16,
            clientHeight - buttonOk.Height - 12);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        WindowHelper.ActivateForce(this);
        SystemSounds.Asterisk.Play();
        DiagnosticLog.Info("Notification.Show", "");
    }
}
