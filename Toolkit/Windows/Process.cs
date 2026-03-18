using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Toolkit.Windows {
	public enum ProcessWindowStyle {
		Normal,
		Minimized,
		Maximized,
		NoActivate,				// 非アクティブ
		MinimizedNoActivate,	// 最小化非アクティブ
		Hidden,
	}

	public class ProcessStartInfo {
		public string Arguments = null;
		public string FileName = null;
		public string Verb = null;
		public string WorkingDirectory = null;
		public ProcessWindowStyle WindowStyle = ProcessWindowStyle.Normal;
		public bool CreateNoWindow = false;
		public bool ErrorDialog = true;
		public IntPtr ErrorDialogParentHandle = IntPtr.Zero;

		public ProcessStartInfo() {
		}
		public ProcessStartInfo(string fileName) {
			FileName = fileName;
		}
		public ProcessStartInfo(string fileName, string arguments) {
			FileName = fileName;
			Arguments = arguments;
		}
	}

	/// <summary>
	/// .NETの::ShellExecuteEx()のラッパーがWindowStyleの辺りとか中途半端なので自分用に自作。
	/// 基本的に.NETのインターフェースに大体準拠しつつも、使わん機能はとりあえず省略。。
	/// </summary>
	public class Process {
		//public static System.Diagnostics.Process Start(ProcessStartInfo info) {
		public static void Start(ProcessStartInfo info) {
			IntPtr hProcess = InnerStart(info);
			CloseHandle(hProcess);
		}

		public static void Start(ProcessStartInfo info, System.Diagnostics.ProcessPriorityClass priority) {
			IntPtr hProcess = InnerStart(info);
			switch (priority) {
			case System.Diagnostics.ProcessPriorityClass.RealTime: SetPriorityClass(hProcess, REALTIME_PRIORITY_CLASS); break;
			case System.Diagnostics.ProcessPriorityClass.High: SetPriorityClass(hProcess, HIGH_PRIORITY_CLASS); break;
			case System.Diagnostics.ProcessPriorityClass.AboveNormal: SetPriorityClass(hProcess, ABOVE_NORMAL_PRIORITY_CLASS); break;
			case System.Diagnostics.ProcessPriorityClass.Normal: SetPriorityClass(hProcess, NORMAL_PRIORITY_CLASS); break;
			case System.Diagnostics.ProcessPriorityClass.BelowNormal: SetPriorityClass(hProcess, BELOW_NORMAL_PRIORITY_CLASS); break;
			case System.Diagnostics.ProcessPriorityClass.Idle: SetPriorityClass(hProcess, IDLE_PRIORITY_CLASS); break;
			}
			CloseHandle(hProcess);
		}

		private static IntPtr InnerStart(ProcessStartInfo info) {
			SHELLEXECUTEINFO shinfo = new SHELLEXECUTEINFO();
			shinfo.cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO));
			shinfo.fMask = SEE_MASK_NOCLOSEPROCESS;
			if (info.CreateNoWindow) {
				shinfo.fMask |= SEE_MASK_NO_CONSOLE;
			}
			if (!info.ErrorDialog) {
				shinfo.fMask |= SEE_MASK_FLAG_NO_UI;
			}
			shinfo.hwnd = info.ErrorDialogParentHandle;
			shinfo.lpVerb = info.Verb;
			shinfo.lpFile = info.FileName;
			shinfo.lpParameters = info.Arguments;
			shinfo.lpDirectory = info.WorkingDirectory;
			switch (info.WindowStyle) {
			case ProcessWindowStyle.Normal: shinfo.nShow = SW_SHOWNORMAL; break;
			case ProcessWindowStyle.Minimized: shinfo.nShow = SW_SHOWMINIMIZED; break;
			case ProcessWindowStyle.Maximized: shinfo.nShow = SW_SHOWMAXIMIZED; break;
			case ProcessWindowStyle.NoActivate: shinfo.nShow = SW_SHOWNOACTIVATE; break;
			case ProcessWindowStyle.MinimizedNoActivate: shinfo.nShow = SW_SHOWMINNOACTIVE; break;
			case ProcessWindowStyle.Hidden: shinfo.nShow = SW_HIDE; break;
			}
			shinfo.hInstApp = IntPtr.Zero;
			shinfo.lpIDList = IntPtr.Zero;
			shinfo.lpClass = null;
			shinfo.hkeyClass = IntPtr.Zero;
			shinfo.dwHotKey = 0;
			shinfo.hIcon = IntPtr.Zero;
			shinfo.hProcess = IntPtr.Zero;

			if (!ShellExecuteEx(ref shinfo)) {
				// エラーダイアログ出す場合これせんでもええような気もするものの。。
				throw new System.ComponentModel.Win32Exception("ファイルの実行に失敗しました。");
			}
			return shinfo.hProcess;

			/*
			ProcessHandle -> Processの変換は難しいのでやめとく。

			try {
				int processId = GetProcessId(shinfo.hProcess);
					// ↑どうもこれほとんどのOSが対応してないような…。
				return System.Diagnostics.Process.GetProcessById(processId);
			} catch (Exception exception) {
				System.Diagnostics.Debug.WriteLine(exception.Message);
				// ここには結構な確率でくるような。。
			}

			foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses()) {
				if (p.Handle == shinfo.hProcess) {
					return p; // こんな一致の仕方も無いと思うが。。(´ω`)
				}
				// この辺でshinfo.hProcessと一致しそうなのを適当に判定すれば実用上問題ないと思うが
				// とりあえず面倒なのであとで。
			}

			return null;
			*/
		}

		#region ShellExecuteExとか

		const int SW_HIDE = 0;
		const int SW_SHOWNORMAL = 1;
		const int SW_SHOWMINIMIZED = 2;
		const int SW_SHOWMAXIMIZED = 3;
		const int SW_SHOWNOACTIVATE = 4;
		const int SW_SHOW = 5;
		const int SW_MINIMIZE = 6;
		const int SW_SHOWMINNOACTIVE = 7;
		const int SW_SHOWNA = 8;
		const int SW_RESTORE = 9;
		const int SW_SHOWDEFAULT = 10;
		const int SW_FORCEMINIMIZE = 11;

		const uint SEE_MASK_CLASSNAME = 0x00000001;
		const uint SEE_MASK_CLASSKEY = 0x00000003;
		const uint SEE_MASK_IDLIST = 0x00000004;
		const uint SEE_MASK_INVOKEIDLIST = 0x0000000c;
		const uint SEE_MASK_ICON = 0x00000010;
		const uint SEE_MASK_HOTKEY = 0x00000020;
		const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
		const uint SEE_MASK_CONNECTNETDRV = 0x00000080;
		const uint SEE_MASK_FLAG_DDEWAIT = 0x00000100;
		const uint SEE_MASK_DOENVSUBST = 0x00000200;
		const uint SEE_MASK_FLAG_NO_UI = 0x00000400; // エラーが発生してもエラーメッセージボックスを表示しない
		const uint SEE_MASK_UNICODE = 0x00004000;
		const uint SEE_MASK_NO_CONSOLE = 0x00008000; // 新しいコンソールを作成する？
		const uint SEE_MASK_HMONITOR = 0x00200000;
		const uint SEE_MASK_FLAG_LOG_USAGE = 0x04000000;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct SHELLEXECUTEINFO {
			public int cbSize;
			public uint fMask;
			public IntPtr hwnd;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpVerb;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpFile;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpParameters;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpDirectory;
			public int nShow;
			public IntPtr hInstApp;
			public IntPtr lpIDList;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpClass;
			public IntPtr hkeyClass;
			public uint dwHotKey;
			public IntPtr hIcon; // hMonitor
			public IntPtr hProcess;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO shinfo);

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll")]
		static extern int GetProcessId(IntPtr Process);

		public const uint ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000;
		public const uint BELOW_NORMAL_PRIORITY_CLASS = 0x00004000;
		public const uint HIGH_PRIORITY_CLASS = 0x00000080;
		public const uint IDLE_PRIORITY_CLASS = 0x00000040;
		public const uint NORMAL_PRIORITY_CLASS = 0x00000020;
		public const uint REALTIME_PRIORITY_CLASS = 0x00000100;

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

		#endregion
	}
}
