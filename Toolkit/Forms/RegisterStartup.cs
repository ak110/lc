using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace Toolkit.Forms {
	/// <summary>
	/// スタートアップに登録とかのボタン４つ。
	/// </summary>
	[DefaultProperty("BaseName")]
	public partial class RegisterStartup : UserControl {
		string baseName = null;
		string sendToName = null;

		public RegisterStartup() {
			InitializeComponent();
		}

		private void RegisterStartup_Load(object sender, EventArgs e) {
			UpdateButtonValid();
		}

		#region IDE用プロパティ

		[DefaultValue(null)]
		[Description("作成するショートカットのファイル名(拡張子を除く)")]
		public string BaseName {
			get { return baseName; }
			set { baseName = value; }
		}

		[DefaultValue(true)]
		[Description("送るへの登録ボタンも使用する")]
		public bool UseSendTo {
			get {
				return button5.Visible && button6.Visible;
			}
			set {
				button5.Visible = value;
				button6.Visible = value;
			}
		}

		[DefaultValue(null)]
		[Description("送るへ登録するファイル名(拡張子を除いた部分)。デフォルトならBaseNameが使用される。")]
		public string SendToName {
			get { return sendToName; }
			set { sendToName = value; }
		}

		#endregion
			
		private string InnerGetBaseName() {
			if (string.IsNullOrEmpty(baseName)) {
				return Path.GetFileNameWithoutExtension(
					Process.GetCurrentProcess().MainModule.FileName);
			}
			return baseName;
		}

		private void button1_Click(object sender, EventArgs e) {
			CreateShortcut(GetCommonStartupLinkName());
			UpdateButtonValid();
		}

		private void button2_Click(object sender, EventArgs e) {
			File.Delete(GetCommonStartupLinkName());
			UpdateButtonValid();
		}

		private void button3_Click(object sender, EventArgs e) {
			CreateShortcut(GetStartupLinkName());
			UpdateButtonValid();
		}

		private void button4_Click(object sender, EventArgs e) {
			File.Delete(GetStartupLinkName());
			UpdateButtonValid();
		}

		private void button5_Click(object sender, EventArgs e) {
			CreateShortcut(GetSendToName());
			UpdateButtonValid();
		}

		private void button6_Click(object sender, EventArgs e) {
			File.Delete(GetSendToName());
			UpdateButtonValid();
		}

		/// <summary>
		/// ショートカットの作成
		/// </summary>
		private static void CreateShortcut(string file) {
			using (Toolkit.Windows.ShellLink link = new Toolkit.Windows.ShellLink()) {
				link.TargetPath = Process.GetCurrentProcess().MainModule.FileName;
				link.Arguments = "";
				//link.WorkingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
				link.Description = "";
				//link.IconFile = Process.GetCurrentProcess().MainModule.FileName;
				//link.IconIndex = 0;
				link.Save(file);
			}
		}

		/// <summary>
		/// ボタンの有効・無効を設定
		/// </summary>
		private void UpdateButtonValid() {
			int common = -1, user = -1;
			try {
				string path = GetCommonStartupLinkName();
				common = File.Exists(path) ? 1 : 0;
			} catch (IOException) {
			}
			try {
				if (!IsCommonStartupWritable()) {
					common = -1;
				}
			} catch (IOException) {
			}
			try {
				string path = GetStartupLinkName();
				user = File.Exists(path) ? 1 : 0;
			} catch (IOException) {
			}
			button1.Enabled = common == 0 && user <= 0;
			button2.Enabled = common == 1;
			button3.Enabled = common <= 0 && user == 0;
			button4.Enabled = user == 1;
			if (UseSendTo) {
				int sendto = -1;
				try {
					sendto = File.Exists(GetSendToName()) ? 1 : 0;
				} catch {
				}
				button5.Enabled = sendto == 0;
				button6.Enabled = sendto == 1;
			}
		}

		private bool IsCommonStartupWritable() {
			return Toolkit.IO.Utility.IsWritable(Toolkit.Windows.Environment.GetFolderPath(
				Toolkit.Windows.Environment.SpecialFolder.CommonStartup));
		}

		private string GetCommonStartupLinkName() {
			string startupDir = Toolkit.Windows.Environment.GetFolderPath(
				Toolkit.Windows.Environment.SpecialFolder.CommonStartup);
			return Path.Combine(startupDir, InnerGetBaseName() + ".lnk");
		}

		private string GetStartupLinkName() {
			string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
			return Path.Combine(startupDir, InnerGetBaseName() + ".lnk");
		}

		private string GetSendToName() {
			string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
			string baseName = string.IsNullOrEmpty(sendToName) ? InnerGetBaseName() : sendToName;
			return Path.Combine(startupDir, baseName + ".lnk");
		}
	}
}
