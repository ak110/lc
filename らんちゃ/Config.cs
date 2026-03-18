using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace らんちゃ {
	public class Config : Toolkit.Serializable, ICloneable {
		public bool Debug = false;

		public int IconDoubleClick = 0;
		public int ItemDoubleClick = 0;
		public int ProcessPriority = 3;
		public bool HideFirst = false;

		public string HotKey = "Win+Space";

		public bool OpenDirByFiler = true;
		public string Filer = "Explorer.exe";
		public string OpenParentFiler = "Explorer.exe";
		public string OpenParentFilerParam1 = "/select,";
		public string OpenParentFilerParam2 = "";

		public bool LargeIcon = true;
		public bool TrayIcon = true;
		public List<string> ReplaceEnv = new List<string>();

		public int CloseButton = 2; // WindowNoClose ? 0 : 2
		public bool WindowNoResize = false;
		public bool WindowTopMost = false;
		public bool WindowHideNoActive = true;
        public bool HideOnRun = false;
		public bool CommandIgnoreCase = true; // !CommandCharDistinct

		public Point WindowPos = new Point(200, 125);
		public Size WindowSize = new Size(400, 350);

		public int RunAsAdminType = 0;
		public string RunAsCommandLine = "/user:Administrator /savecred";
		public string VECmdPath = @"%ProgramFiles%\Vistaのエレベータ\VECmd.exe";

        public bool UseTreeLauncher = false;

		public Toolkit.Updater.Data.UpdateConfig UpdateConfig = new Toolkit.Updater.Data.UpdateConfig();

		/// <summary>
		/// 複製の作成
		/// </summary>
		public Config Clone() {
			Config copy = (Config)MemberwiseClone();
			return copy;
		}

		#region ICloneable メンバ

		object ICloneable.Clone() {
			return Clone();
		}

		#endregion

		#region Serialize/Deserialize

		/// <summary>
		/// 書き込み
		/// </summary>
		public void Serialize() {
			Serialize(".cfg");
		}

		/// <summary>
		/// 読み込み
		/// </summary>
		public static Config Deserialize() {
			try {
				return Deserialize<Config>(".cfg");
			} catch {
				string name = DefaultBaseName + ".cfg";
				if (System.IO.File.Exists(name)) {
					Toolkit.LegacyConfigReader reader = new Toolkit.LegacyConfigReader(name);
					try {
						return Config.LoadFrom(reader);
					} catch {
					}
				}
				return new Config();
			}
		}

		#endregion

		/// <summary>
		/// 後方互換性のための処理
		/// </summary>
		public static Config LoadFrom(Toolkit.LegacyConfigReader reader) {
			Config data = new Config();

			data.IconDoubleClick = reader.Num("IconDoubleClick");
			data.ItemDoubleClick = reader.Num("ItemDoubleClick");
			if (data.ItemDoubleClick == 0) {
				data.ItemDoubleClick = 1;
			} else if (data.ItemDoubleClick == 1) {
				data.ItemDoubleClick = 0;
			}
			data.ProcessPriority = reader.Num("ProcessPriority");
			data.HideFirst = reader.Bool("HideFirst");

			data.HotKey = reader.Indirect("HotKey");

			data.OpenDirByFiler = reader.Bool("OpenDirByFiler");
			data.Filer = reader.Indirect("Filer");
			data.OpenParentFiler = reader.Indirect("OpenParentFiler");
			data.OpenParentFilerParam1 = reader.Indirect("OpenParentFilerParam1");
			data.OpenParentFilerParam2 = reader.Indirect("OpenParentFilerParam2");

			data.LargeIcon = reader.Bool("LargeIcon");
			data.TrayIcon = reader.Bool("TrayIcon");
			data.ReplaceEnv = new List<string>(
				reader.Indirect("ReplaceEnv").Split('%'));

			data.CloseButton = reader.Bool("WindowNoClose") ? 0 : 2;
			data.WindowNoResize = reader.Bool("WindowNoResize");
			data.WindowTopMost = reader.Bool("WindowTopMost");
			data.WindowHideNoActive = reader.Bool("WindowHideNoActive");
			data.CommandIgnoreCase = !reader.Bool("CommandCharDistinct");

			Regex regex = new Regex(@"(-?\d+), *(-?\d+)");
			Match m = regex.Match(reader.Indirect("WindowPos"));
			if (m.Success) {
				data.WindowPos = new Point(
					int.Parse(m.Groups[1].Value),
					int.Parse(m.Groups[2].Value));
			}

			m = regex.Match(reader.Indirect("WindowSize"));
			if (m.Success) {
				data.WindowSize = new Size(
					int.Parse(m.Groups[1].Value),
					int.Parse(m.Groups[2].Value));
			}

			return data;
		}
	}
}
