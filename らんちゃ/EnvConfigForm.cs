using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace らんちゃ {
	public partial class EnvConfigForm : Form {
		List<string> replaceEnv;

		public List<string> ReplaceEnv {
			get { return replaceEnv; }
		}

		public EnvConfigForm(List<string> replaceEnv) {
			InitializeComponent();

			this.replaceEnv = Toolkit.Utility.Clone(replaceEnv);
			Toolkit.Forms.Utility.SetArray(listBox2, this.replaceEnv);

			foreach (System.Collections.DictionaryEntry p in Environment.GetEnvironmentVariables()) {
				string value = p.Value.ToString();
				//try { value = FileName.GetFullPath(value); } catch { }
				if (Directory.Exists(value) || File.Exists(value)) {
					string name = p.Key.ToString();
					if (this.replaceEnv.Contains(name)) {
						//listBox2.Items.Add(name);
					} else {
						listBox1.Items.Add(name);
					}
				}
			}
		}

		private void buttonOk_Click(object sender, EventArgs e) {
			replaceEnv = Toolkit.Forms.Utility.GetArray<string>(listBox2);
		}

		/// <summary>
		/// 追加
		/// </summary>
		private void button1_Click(object sender, EventArgs e) {
			string item = listBox1.SelectedItem as string;
			if (item != null) {
				Toolkit.Forms.Utility.RemoveSelected(listBox1);
				listBox2.Items.Add(item);
			}
		}

		/// <summary>
		/// 削除
		/// </summary>
		private void button2_Click(object sender, EventArgs e) {
			string item = listBox2.SelectedItem as string;
			if (item != null) {
				Toolkit.Forms.Utility.RemoveSelected(listBox2);
				listBox1.Items.Add(item);
			}
		}
	}
}