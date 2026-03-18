using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace らんちゃ {
	static class Program {
		public const int WM_APPMSG = Toolkit.Windows.WM.WM_APP + 0;
		public static readonly IntPtr WM_APPMSG_WPARAM = (IntPtr)0x11747b79; // ←誤爆防止用ダミー。
		public static readonly IntPtr WM_APPMSG_SHOWHIDE = (IntPtr)0x14d94a96;
		public static readonly IntPtr WM_APPMSG_RELOAD = (IntPtr)0x338ca4c1;
		public static readonly IntPtr WM_APPMSG_RESTART = (IntPtr)0x6b60850f;

		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			using (Toolkit.App.App.Initializer app = new Toolkit.App.App.Initializer())
			using (Toolkit.AntiMultiplex antiMultiplex = new Toolkit.AntiMultiplex()) {
				bool exit = false;
				// 引数の処理
				for (int i = 0; i < args.Length; i++) {
					if (args[i] == "/close") {
						try {
							Data data = Data.Deserialize();
							Toolkit.Windows.Window window =
								new Toolkit.Windows.Window((IntPtr)data.WindowHandle);
							window.PostMessage(Toolkit.Windows.WM.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
						} catch {
							// とりあえずエラーは無視。
						}
						return;
					} else if (args[i] == "/restart") {
						try {
							Data data = Data.Deserialize();
							Toolkit.Windows.Window window =
								new Toolkit.Windows.Window((IntPtr)data.WindowHandle);
							window.PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RESTART);
						} catch {
							// とりあえずエラーは無視。
						}
						return;
					//} else if (args[i] == "/run" && i + 1 < args.Length) {
					// TODO: 実装
					} else if (System.IO.File.Exists(args[i]) || System.IO.Directory.Exists(args[i])) {
						Command command = Command.FromFile(args[i]);
						new ReplaceEnvList(Config.Deserialize().ReplaceEnv).Replace(command);
						using (EditCommandForm form = new EditCommandForm(command)) {
							if (form.ShowDialog() == DialogResult.OK) {
                                CommandList commandList = CommandList.Deserialize(".cmd.cfg");
								commandList.Add(command);
                                commandList.Serialize(".cmd.cfg");

								try {
									Data data = Data.Deserialize();
									Toolkit.Windows.Window window =
										new Toolkit.Windows.Window((IntPtr)data.WindowHandle);
									window.PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RELOAD);
								} catch {
									// とりあえずエラーは無視。
								}
							}
							exit = true;
						}
					} else {
						// とりあえず無視
					}
				}

				if (exit) return;

				if (!antiMultiplex.FirstRun) {
					antiMultiplex.SetActive();
					return;
				}

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new DummyForm());
			}
		}
	}
}