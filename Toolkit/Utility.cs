using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Toolkit {
	/// <summary>
	/// ユーティリティ風味な関数とか置き場
	/// </summary>
	public static class Utility {
		/// <summary>
		/// CloneableなオブジェクトのリストのClone。
		/// </summary>
		/// <param name="list">複製元リスト</param>
		/// <returns>複製のリスト</returns>
		public static List<T> Clone<T>(List<T> list) where T : ICloneable {
			List<T> result = new List<T>();
			foreach (T t in list) result.Add((T)t.Clone());
			return result;
		}

		/// <summary>
		/// 現在のユーザーがAdmin権限持ってるならtrue
		/// </summary>
		public static bool IsUserAnAdmin() {
			try {
				return _IsUserAnAdmin();
			} catch (EntryPointNotFoundException) {
			}
			return false;
		}

		[DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin")]
		[return: MarshalAs(UnmanagedType.Bool)]
		extern static bool _IsUserAnAdmin();
	}
}
