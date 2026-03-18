#nullable disable
using Launcher.UI;

namespace Launcher.Updater;

/// <summary>
/// 更新時の設定を行うダイアログ。
/// </summary>
public partial class UpdateConfigForm : Form {
	UpdateConfig config;

	public UpdateConfig Config {
		get { return config; }
	}

	public UpdateConfigForm(UpdateConfig v) {
		InitializeComponent();

		config = v.Clone();

		checkBox1.Checked = config.Backup;
		numericUpDown1.Value = config.BackupCount;
		radioButtonList1.SelectedIndex = (int)config.ProxyType;
		textBox1.Text = config.ProxyServer;

		// 一応手動でも呼んでおく。
		radioButtonList1_SelectedIndexChanged(this, null);
	}

	/// <summary>
	/// OKボタン押された
	/// </summary>
	private void buttonOk_Click(object sender, EventArgs e) {
		config.Backup = checkBox1.Checked;
		config.BackupCount = (int)numericUpDown1.Value;
		config.ProxyType = (ProxyType)radioButtonList1.SelectedIndex;
		config.ProxyServer = textBox1.Text;
	}

	private void radioButtonList1_SelectedIndexChanged(object sender, EventArgs e) {
		textBox1.Enabled = radioButtonList1.SelectedIndex == 2;
	}
}
