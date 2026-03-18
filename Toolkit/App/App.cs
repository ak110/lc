using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace Toolkit.App {
	/// <summary>
	/// アプリケーションな処理。
	/// </summary>
	public static class App {
		static bool started = false;
		static bool restart = false;

		/// <summary>
		/// Program.Main()の開始時に呼ぶ処理。
		/// </summary>
		public static void Initialize() {
			Debug.Assert(!started);
			Toolkit.App.ErrorReporter errorReporter = Toolkit.App.ErrorReporter.Instance;
			errorReporter.ExitApplication += new EventHandler(errorReporter_ExitApplication);
			errorReporter.RestartApplication += new EventHandler(errorReporter_RestartApplication);
			errorReporter.Register();

			//AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);

			started = true;
		}

		/// <summary>
		/// 終了時に再起動させるようにする時に呼ぶ。
		/// </summary>
		public static void SetRestart() {
			Debug.Assert(started);
			restart = true;
		}

		/// <summary>
		/// 終了時の処理
		/// </summary>
		public static void OnExit() {
			Debug.Assert(started);
			started = false;
			if (restart) {
				Process.Start(Process.GetCurrentProcess().MainModule.FileName);
			}
		}

		/// <summary>
		/// Initialize(), OnExit()を呼ぶクラス。
		/// </summary>
		public class Initializer : IDisposable {
			/// <summary>
			/// 初期化
			/// </summary>
			public Initializer() {
				App.Initialize();
			}

			/// <summary>
			/// 後始末
			/// </summary>
			public void Dispose() {
				App.OnExit();
			}
		}

		static void errorReporter_ExitApplication(object sender, EventArgs e) {
			Application.Exit();
		}

		static void errorReporter_RestartApplication(object sender, EventArgs e) {
			restart = true;
			Application.Exit();
		}

	}
}
