using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;                 // Mutex
using System.Diagnostics;               // Process
using System.Runtime.InteropServices;
using System.IO;   // DllImport

namespace Toolkit {
	/// <summary>
	/// 多重起動対策などを行う
	/// </summary>
	/// <example>
	/// using (Toolkit.AntiMultiplex antiMultiplex = new Toolkit.AntiMultiplex()) {
	///		if (!antiMultiplex.FirstRun) {
	///			antiMultiplex.SetActive();
	///			return;
	///		}
	///		メインの処理を記述
	/// }
	/// </example>
	public class AntiMultiplex : IDisposable {
		Mutex mutex = null;
		bool firstRun;

		/// <summary>
		/// コンストラクタ。
		/// mutex名は自身へのパスを元に作成。
		/// </summary>
		public AntiMultiplex() : this(GetMutexName()) {}

		/// <summary>
		/// 自身へのパスを元にmutex名を作成
		/// </summary>
		private static string GetMutexName() {
			//string moduleFileName = Application.ExecutablePath;
			string moduleFileName = Process.GetCurrentProcess().MainModule.FileName;
			//moduleFileName = Path.GetFullPath(Environment.ExpandEnvironmentVariables(moduleFileName));
			string mutexName = moduleFileName.ToLower().Replace('\\', '/');
			return mutexName;
		}

		/// <summary>
		/// コンストラクタ。
		/// ここで多重起動の排他処理や初回起動なのかの判定が行われる。
		/// </summary>
		/// <param name="mutexName">mutex名</param>
		public AntiMultiplex(string mutexName) {
			try {
				mutex = new Mutex(false, mutexName);
				firstRun = mutex.WaitOne(0, false);
			} catch {
				// 多重起動な事にしてしまう。
				firstRun = false;
			}
		}

		/// <summary>
		/// あとしまつ。
		/// </summary>
		public void Dispose() {
			mutex.Close();
		}

		/// <summary>
		/// 初回起動なのかどうか。
		/// </summary>
		public bool FirstRun {
			get { return firstRun; }
		}

		/// <summary>
		/// 既に起動済みなウィンドウをアクティブにする
		/// </summary>
		public void SetActive() {
			foreach (Process p in GetProcesses()) {
				if (p.MainWindowHandle != IntPtr.Zero) {
					SetForegroundWindow(p.MainWindowHandle);
					BringWindowToTop(p.MainWindowHandle);
				}
			}
		}

		#region DllImport

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool BringWindowToTop(IntPtr hWnd);

		#endregion

		/// <summary>
		/// 自分のモジュールファイル名と同じファイル名なプロセスを列挙（多分０～１個のはず）
		/// </summary>
		public List<Process> GetProcesses() {
			List<Process> list = new List<Process>();
			Process current = Process.GetCurrentProcess();
			Process[] processes = Process.GetProcessesByName(current.ProcessName);
			foreach (Process p in processes) {
				// 自分なら無視
				if (p.Id == current.Id) {
					continue;
				}
				// ファイル名違うなら無視
				if (String.Compare(p.MainModule.FileName,
					current.MainModule.FileName, true) != 0) {
					continue;
				}
				// 追加
				list.Add(p);
			}
			return list;
		}
	}
}
