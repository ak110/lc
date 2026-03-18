#nullable disable
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Launcher.Win32 {
	/// <summary>
	/// 環境変数とかその他いろいろ。
	/// </summary>
	public static class ShellEnvironment {
		/// <summary>
		/// GetFolderPath()用enum。
		/// </summary>
		/// <remarks>
		/// System.Environmentのにはないものなので。
		/// </remarks>
		public enum SpecialFolder {
			CommonStartup,
		}

		/// <summary>
		/// システムの固定フォルダへのパスを取得
		/// </summary>
		public static string GetFolderPath(SpecialFolder folder) {
			IMalloc malloc = null;
			SHGetMalloc(out malloc);

			IntPtr idl = IntPtr.Zero;
			switch (folder) {
			case SpecialFolder.CommonStartup:
				SHGetSpecialFolderLocation(IntPtr.Zero, CSIDL_COMMON_STARTUP, out idl);
				break;
			}

			if (idl == IntPtr.Zero) {
				throw new IOException("特殊フォルダパスの取得に失敗");
			}

			StringBuilder path = new StringBuilder(512);
			SHGetPathFromIDList(idl, path);

			malloc.Free(idl);

			return path.ToString();
		}

		#region SHGetSpecialFolderLocationとかいろいろ

		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000002-0000-0000-C000-000000000046")]
		public interface IMalloc
		{
			[PreserveSig] IntPtr Alloc([In] int cb);
			[PreserveSig] IntPtr Realloc([In] IntPtr pv, [In] int cb);
			[PreserveSig] void Free([In] IntPtr pv);
			[PreserveSig] int GetSize([In] IntPtr pv);
			[PreserveSig] int DidAlloc(IntPtr pv);
			[PreserveSig] void HeapMinimize();
		}

		[DllImport("shell32.dll")]
		static extern int SHGetMalloc(out IMalloc ppMalloc);

		[DllImport("shell32.dll")]
		static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, int nFolder, out IntPtr ppidl);

		[DllImport("shell32.dll")]
		static extern int SHGetPathFromIDList(IntPtr pidl, StringBuilder Path);

		const int CSIDL_DESKTOP = 0x0000;
		const int CSIDL_INTERNET = 0x0001;
		const int CSIDL_PROGRAMS = 0x0002;
		const int CSIDL_CONTROLS = 0x0003;
		const int CSIDL_PRINTERS = 0x0004;
		const int CSIDL_PERSONAL = 0x0005;
		const int CSIDL_FAVORITES = 0x0006;
		const int CSIDL_STARTUP = 0x0007;
		const int CSIDL_RECENT = 0x0008;
		const int CSIDL_SENDTO = 0x0009;
		const int CSIDL_BITBUCKET = 0x000a;
		const int CSIDL_STARTMENU = 0x000b;
		const int CSIDL_MYDOCUMENTS = 0x000c;
		const int CSIDL_MYMUSIC = 0x000d;
		const int CSIDL_MYVIDEO = 0x000e;

		const int CSIDL_DESKTOPDIRECTORY = 0x0010;
		const int CSIDL_DRIVES = 0x0011;
		const int CSIDL_NETWORK = 0x0012;
		const int CSIDL_NETHOOD = 0x0013;
		const int CSIDL_FONTS = 0x0014;
		const int CSIDL_TEMPLATES = 0x0015;
		const int CSIDL_COMMON_STARTMENU = 0x0016;
		const int CSIDL_COMMON_PROGRAMS = 0x0017;
		const int CSIDL_COMMON_STARTUP = 0x0018;
		const int CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019;
		const int CSIDL_APPDATA = 0x001a;
		const int CSIDL_PRINTHOOD = 0x001b;
		const int CSIDL_LOCAL_APPDATA = 0x001c;
		const int CSIDL_ALTSTARTUP = 0x001d;
		const int CSIDL_COMMON_ALTSTARTUP = 0x001e;
		const int CSIDL_COMMON_FAVORITES = 0x001f;

		const int CSIDL_INTERNET_CACHE = 0x0020;
		const int CSIDL_COOKIES = 0x0021;
		const int CSIDL_HISTORY = 0x0022;
		const int CSIDL_COMMON_APPDATA = 0x0023;
		const int CSIDL_WINDOWS = 0x0024;
		const int CSIDL_SYSTEM = 0x0025;
		const int CSIDL_PROGRAM_FILES = 0x0026;
		const int CSIDL_MYPICTURES = 0x0027;
		const int CSIDL_PROFILE = 0x0028;
		const int CSIDL_SYSTEMX86 = 0x0029;
		const int CSIDL_PROGRAM_FILESX86 = 0x002a;
		const int CSIDL_PROGRAM_FILES_COMMON = 0x002b;
		const int CSIDL_PROGRAM_FILES_COMMONX86 = 0x002c;
		const int CSIDL_COMMON_TEMPLATES = 0x002d;
		const int CSIDL_COMMON_DOCUMENTS = 0x002e;
		const int CSIDL_COMMON_ADMINTOOLS = 0x002f;

		const int CSIDL_ADMINTOOLS = 0x0030;
		const int CSIDL_CONNECTIONS = 0x0031;
		const int CSIDL_COMMON_MUSIC = 0x0035;
		const int CSIDL_COMMON_PICTURES = 0x0036;
		const int CSIDL_COMMON_VIDEO = 0x0037;
		const int CSIDL_RESOURCES = 0x0038;
		const int CSIDL_RESOURCES_LOCALIZED = 0x0039;
		const int CSIDL_COMMON_OEM_LINKS = 0x003a;
		const int CSIDL_CDBURN_AREA = 0x003b;
		const int CSIDL_COMPUTERSNEARME = 0x003d;

		#endregion
	}
}
