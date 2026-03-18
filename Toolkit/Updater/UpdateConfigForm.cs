using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Toolkit.Updater {
	/// <summary>
	/// 更新情報の設定を行うダイアログ。
	/// </summary>
	/// <example>
	/// <code>
	/// using (UpdateConfigForm form = new UpdateConfigForm(config.UpdateConfig)) {
	///		if (form.ShowDialog(this) == DialogResult.OK) {
	///			config.UpdateConfig = form.Config;
	///		}
	/// }
	/// </code>
	/// </example>
	public partial class UpdateConfigForm : Form {
		Data.UpdateConfig config;

		public Data.UpdateConfig Config {
			get { return config; }
		}

		public UpdateConfigForm(Data.UpdateConfig v) {
			InitializeComponent();

			config = v.Clone();

			checkBox1.Checked = config.Backup;
			numericUpDown1.Value = config.BackupCount;
			radioButtonList1.SelectedIndex = (int)config.ProxyType; // 手抜き
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
			config.ProxyType = (Data.ProxyType)radioButtonList1.SelectedIndex;
			config.ProxyServer = textBox1.Text;
		}

		private void radioButtonList1_SelectedIndexChanged(object sender, EventArgs e) {
			textBox1.Enabled = radioButtonList1.SelectedIndex == 2;
		}
	}
}