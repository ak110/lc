using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace らんちゃ {
	public partial class ConfigForm : Form {
		Config config;

		public Config Config {
			get { return config; }
		}

		public ConfigForm(Config v) {
			InitializeComponent();

			config = v.Clone();

			comboBox1.Items.AddRange(
				Toolkit.Windows.KeyTable.GetKeyNames(false));

			Toolkit.Pair<Toolkit.Windows.KeyTable.Keys?, Toolkit.Windows.KeyTable.Modifiers>
				hk = Toolkit.Windows.KeyTable.GetKeyWithModifiers(config.HotKey);
			checkBox1.Checked = (hk.Second & Toolkit.Windows.KeyTable.Modifiers.Ctrl) != 0;
			checkBox2.Checked = (hk.Second & Toolkit.Windows.KeyTable.Modifiers.Alt) != 0;
			checkBox3.Checked = (hk.Second & Toolkit.Windows.KeyTable.Modifiers.Shift) != 0;
			checkBox4.Checked = (hk.Second & Toolkit.Windows.KeyTable.Modifiers.Win) != 0;
			try {
				comboBox1.SelectedItem = Toolkit.Windows.KeyTable.GetKeyName(hk.First.Value);
			} catch {
				comboBox1.SelectedItem = "Space";
			}
			checkBox5.Checked = config.TrayIcon;
			radioButtonList1.SelectedIndex = config.IconDoubleClick;
			radioButtonList2.SelectedIndex = config.ItemDoubleClick;
			checkBox6.Checked = config.HideFirst;
			checkBox7.Checked = config.LargeIcon;
			checkBox8.Checked = config.WindowTopMost;
			checkBox9.Checked = config.WindowHideNoActive;
			checkBox10.Checked = config.WindowNoResize;
            checkBox11.Checked = config.HideOnRun;
			comboBox2.SelectedIndex = config.CloseButton;
		}

		private void buttonOk_Click(object sender, EventArgs e) {
			string key = "";
			if (checkBox1.Checked) key += "Ctrl+";
			if (checkBox2.Checked) key += "Alt+";
			if (checkBox3.Checked) key += "Shift+";
			if (checkBox4.Checked) key += "Win+";
			config.HotKey = key + comboBox1.SelectedItem.ToString();
			config.TrayIcon = checkBox5.Checked;
			config.IconDoubleClick = radioButtonList1.SelectedIndex;
			config.ItemDoubleClick = radioButtonList2.SelectedIndex;
			config.HideFirst = checkBox6.Checked;
			config.LargeIcon = checkBox7.Checked;
			config.WindowTopMost = checkBox8.Checked;
			config.WindowHideNoActive = checkBox9.Checked;
			config.WindowNoResize = checkBox10.Checked;
            config.HideOnRun = checkBox11.Checked;
			config.CloseButton = comboBox2.SelectedIndex;
		}

		/// <summary>
		/// 置換環境変数の設定ボタン
		/// </summary>
		private void button1_Click(object sender, EventArgs e) {
			using (EnvConfigForm form = new EnvConfigForm(config.ReplaceEnv)) {
				if (form.ShowDialog(this) == DialogResult.OK) {
					config.ReplaceEnv = form.ReplaceEnv;
				}
			}
		}
	}
}