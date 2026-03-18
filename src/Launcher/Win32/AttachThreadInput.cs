#nullable disable
using System.Runtime.InteropServices;

namespace Launcher.Win32;

/// <summary>
/// WinAPI AttachThreadInput()のラッパー
/// </summary>
public class AttachThreadInput : IDisposable {
	/// <summary>
	/// スレッドID
	/// </summary>
	int foreground, current;

	/// <summary>
	/// アタッチ。
	/// </summary>
	public AttachThreadInput() {
		foreground = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
		current = GetCurrentThreadId();
		if (current != foreground) {
			bool result = AttachThreadInput_(current, foreground, true);
			System.Diagnostics.Debug.Assert(result, new System.ComponentModel.Win32Exception().Message);
		}
	}

	/// <summary>
	/// デタッチ。
	/// </summary>
	public void Dispose() {
		if (current != foreground) {
			bool result = AttachThreadInput_(current, foreground, false);
			System.Diagnostics.Debug.Assert(result, new System.ComponentModel.Win32Exception().Message);
		}
	}

	#region WinAPI

	[DllImport("kernel32.dll")]
	extern static int GetCurrentThreadId();

	[DllImport("user32.dll")]
	extern static int GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

	[DllImport("user32.dll")]
	extern static IntPtr GetForegroundWindow();

	[DllImport("user32.dll", EntryPoint = "AttachThreadInput")]
	[return: MarshalAs(UnmanagedType.Bool)]
	extern static bool AttachThreadInput_(int idAttach, int idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

	#endregion
}
