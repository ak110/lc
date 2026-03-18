using System;
using System.Collections.Generic;
using System.Text;
using SystemKeys = System.Windows.Forms.Keys;

namespace Toolkit.Windows {
	/// <summary>
	/// 自前定義のキーコードと、仮想キーコードの相互変換などを行う。
	/// </summary>
	public static class KeyTable {
		/// <summary>
		/// 修飾キー
		/// </summary>
		[Flags]
		public enum Modifiers {
			Ctrl = 0x01,
			Alt = 0x02,
			Shift = 0x04,
			Win = 0x08,
		}

		/// <summary>
		/// キー。
		/// </summary>
		public enum Keys {
			// 英字(26)
			A, B, C, D, E, F,
			G, H, I, J, K, L,
			M, N, O, P, Q, R,
			S, T, U, V, W, X, Y, Z,
			// 数字(10)
			D1, D2, D3, D4, D5, D6, D7, D8, D9, D0,
			// ファンクション(12)
			F1, F2, F3, F4, F5, F6,
			F7, F8, F9, F10, F11, F12,
			// 特殊(20)
			Escape, Pause, Back,
			Minus, Plus, Tab, Enter,
			Insert, Delete, Home, End, PageUp, PageDown,
			Comma, Period, Space,
			Up, Left, Down, Right,
			// テンキー(16)
			Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,
			NumAdd, NumSub, NumMul, NumDiv,
			// マウス(8)
			LClick, LDouble, RClick, RDouble,
			MClick, MDouble, MUp, MDown,
			X1Click, X1Double, X2Click, X2Double,   // 4,5ボタン
			RLClick, LRClick,   //右押しつつ左クリック, 左押しつつ右クリック
			// キーの数
			KeyCount,
		}

		static Dictionary<SystemKeys, Keys> vkeyToKeys = new Dictionary<SystemKeys, Keys>();
		static Dictionary<Keys, string> keysToKeyName = new Dictionary<Keys, string>();
		static Dictionary<string, Keys> keyNameToKeys = new Dictionary<string, Keys>();

		#region 初期化。

		/// <summary>
		/// 初期化。
		/// </summary>
		static KeyTable() {
			// 英字(26)
			_c(SystemKeys.A, Keys.A); _c(SystemKeys.B, Keys.B); _c(SystemKeys.C, Keys.C); _c(SystemKeys.D, Keys.D);
			_c(SystemKeys.E, Keys.E); _c(SystemKeys.F, Keys.F); _c(SystemKeys.G, Keys.G); _c(SystemKeys.H, Keys.H);
			_c(SystemKeys.I, Keys.I); _c(SystemKeys.J, Keys.J); _c(SystemKeys.K, Keys.K); _c(SystemKeys.L, Keys.L);
			_c(SystemKeys.M, Keys.M); _c(SystemKeys.N, Keys.N); _c(SystemKeys.O, Keys.O); _c(SystemKeys.P, Keys.P);
			_c(SystemKeys.Q, Keys.Q); _c(SystemKeys.R, Keys.R); _c(SystemKeys.S, Keys.S); _c(SystemKeys.T, Keys.T);
			_c(SystemKeys.U, Keys.U); _c(SystemKeys.V, Keys.V); _c(SystemKeys.W, Keys.W); _c(SystemKeys.X, Keys.X);
			_c(SystemKeys.Y, Keys.Y); _c(SystemKeys.Z, Keys.Z);
			// 数字(10)
			_c(SystemKeys.D0, Keys.D0); _c(SystemKeys.D1, Keys.D1); _c(SystemKeys.D2, Keys.D2); _c(SystemKeys.D3, Keys.D3);
			_c(SystemKeys.D4, Keys.D4); _c(SystemKeys.D5, Keys.D5); _c(SystemKeys.D6, Keys.D6); _c(SystemKeys.D7, Keys.D7);
			_c(SystemKeys.D8, Keys.D8); _c(SystemKeys.D9, Keys.D9);
			// ファンクション(12)
			_c(SystemKeys.F1, Keys.F1); _c(SystemKeys.F2, Keys.F2); _c(SystemKeys.F3, Keys.F3); _c(SystemKeys.F4, Keys.F4);
			_c(SystemKeys.F5, Keys.F5); _c(SystemKeys.F6, Keys.F6); _c(SystemKeys.F7, Keys.F7); _c(SystemKeys.F8, Keys.F8);
			_c(SystemKeys.F9, Keys.F9); _c(SystemKeys.F10, Keys.F10); _c(SystemKeys.F11, Keys.F11); _c(SystemKeys.F12, Keys.F12);
			// 特殊(20)
			_c(SystemKeys.Escape, Keys.Escape); _c(SystemKeys.Pause, Keys.Pause); _c(SystemKeys.Back, Keys.Back);
			_c(SystemKeys.Oemplus, Keys.Plus); _c(SystemKeys.OemMinus, Keys.Minus); _c(SystemKeys.Tab, Keys.Tab);
			_c(SystemKeys.Enter, Keys.Enter); _c(SystemKeys.Insert, Keys.Insert); _c(SystemKeys.Delete, Keys.Delete);
			_c(SystemKeys.Home, Keys.Home); _c(SystemKeys.End, Keys.End); _c(SystemKeys.PageUp, Keys.PageUp);
			_c(SystemKeys.PageDown, Keys.PageDown); _c(SystemKeys.Oemcomma, Keys.Comma); _c(SystemKeys.OemPeriod, Keys.Period);
			_c(SystemKeys.Space, Keys.Space); _c(SystemKeys.Left, Keys.Left); _c(SystemKeys.Up, Keys.Up);
			_c(SystemKeys.Right, Keys.Right); _c(SystemKeys.Down, Keys.Down);
			// テンキー(16)
			_c(SystemKeys.NumPad0, Keys.Num0); _c(SystemKeys.NumPad1, Keys.Num1); _c(SystemKeys.NumPad2, Keys.Num2);
			_c(SystemKeys.NumPad3, Keys.Num3); _c(SystemKeys.NumPad4, Keys.Num4); _c(SystemKeys.NumPad5, Keys.Num5);
			_c(SystemKeys.NumPad6, Keys.Num6); _c(SystemKeys.NumPad7, Keys.Num7); _c(SystemKeys.NumPad8, Keys.Num8);
			_c(SystemKeys.NumPad9, Keys.Num9); _c(SystemKeys.Multiply, Keys.NumMul); _c(SystemKeys.Add, Keys.NumAdd);
			_c(SystemKeys.Subtract, Keys.NumSub);
			_c(SystemKeys.Divide, Keys.NumDiv);
			// 英字(26)
			_n(Keys.A, "A"); _n(Keys.B, "B"); _n(Keys.C, "C"); _n(Keys.D, "D");
			_n(Keys.E, "E"); _n(Keys.F, "F"); _n(Keys.G, "G"); _n(Keys.H, "H");
			_n(Keys.I, "I"); _n(Keys.J, "J"); _n(Keys.K, "K"); _n(Keys.L, "L");
			_n(Keys.M, "M"); _n(Keys.N, "N"); _n(Keys.O, "O"); _n(Keys.P, "P");
			_n(Keys.Q, "Q"); _n(Keys.R, "R"); _n(Keys.S, "S"); _n(Keys.T, "T");
			_n(Keys.U, "U"); _n(Keys.V, "V"); _n(Keys.W, "W"); _n(Keys.X, "X");
			_n(Keys.Y, "Y"); _n(Keys.Z, "Z");
			// 数字(10)
			_n(Keys.D1, "1"); _n(Keys.D2, "2"); _n(Keys.D3, "3"); _n(Keys.D4, "4");
			_n(Keys.D5, "5"); _n(Keys.D6, "6"); _n(Keys.D7, "7"); _n(Keys.D8, "8");
			_n(Keys.D9, "9"); _n(Keys.D0, "0");
			// ファンクション(12)
			_n(Keys.F1, "F1"); _n(Keys.F2, "F2"); _n(Keys.F3, "F3");
			_n(Keys.F4, "F4"); _n(Keys.F5, "F5"); _n(Keys.F6, "F6");
			_n(Keys.F7, "F7"); _n(Keys.F8, "F8"); _n(Keys.F9, "F9");
			_n(Keys.F10, "F10"); _n(Keys.F11, "F11"); _n(Keys.F12, "F12");
			// 特殊(20)
			_n(Keys.Escape, "Esc"); _n(Keys.Pause, "Pause"); _n(Keys.Back, "BackSpace");
			_n(Keys.Minus, "-"); _n(Keys.Plus, "+"); _n(Keys.Tab, "Tab");
			_n(Keys.Enter, "Enter"); _n(Keys.Insert, "Insert"); _n(Keys.Delete, "Delete");
			_n(Keys.Home, "Home"); _n(Keys.End, "End"); _n(Keys.PageUp, "PageUp");
			_n(Keys.PageDown, "PageDown"); _n(Keys.Comma, ","); _n(Keys.Period, "."); _n(Keys.Space, "Space");
			_n(Keys.Up, "↑"); _n(Keys.Left, "←"); _n(Keys.Down, "↓"); _n(Keys.Right, "→");
			// テンキー(16)
			_n(Keys.Num0, "Num0"); _n(Keys.Num1, "Num1"); _n(Keys.Num2, "Num2");
			_n(Keys.Num3, "Num3"); _n(Keys.Num4, "Num4"); _n(Keys.Num5, "Num5");
			_n(Keys.Num6, "Num6"); _n(Keys.Num7, "Num7"); _n(Keys.Num8, "Num8");
			_n(Keys.Num9, "Num9"); _n(Keys.NumAdd, "Num +"); _n(Keys.NumSub, "Num -");
			_n(Keys.NumMul, "Num *"); _n(Keys.NumDiv, "Num /");
			// マウス(14)
			_n(Keys.LClick, "左クリック"); _n(Keys.LDouble, "左ダブルクリック");
			_n(Keys.RClick, "右クリック"); _n(Keys.RDouble, "右ダブルクリック");
			_n(Keys.MClick, "ホイールクリック"); _n(Keys.MDouble, "ホイールダブルクリック");
			_n(Keys.MUp, "ホイール(上)"); _n(Keys.MDown, "ホイール(下)");
			_n(Keys.X1Click, "４ボタンクリック"); _n(Keys.X1Double, "４ボタンダブルクリック");
			_n(Keys.X2Click, "５ボタンクリック"); _n(Keys.X2Double, "５ボタンダブルクリック");
			_n(Keys.RLClick, "右押しつつ左クリック"); _n(Keys.LRClick, "左押しつつ右クリック");
		}

		static void _c(SystemKeys vkey, Keys key) {
			vkeyToKeys.Add(vkey, key);
		}

		static void _n(Keys key, string name) {
			keysToKeyName.Add(key, name);
			keyNameToKeys.Add(name, key);
		}

		#endregion

		/// <summary>
		/// キー名を全部列挙
		/// </summary>
		public static string[] GetKeyNames(bool mouse) {
			string[] result = new string[mouse ? 
				(int)Keys.KeyCount : (int)Keys.LClick];
			for (int i = 0; i < result.Length; i++) {
				result[i] = GetKeyName((Keys)i);
			}
			return result;
		}

		/// <summary>
		/// Keysからキーの名前を取得
		/// </summary>
		public static string GetKeyName(Keys key) {
			string name;
			if (keysToKeyName.TryGetValue(key, out name)) {
				return name;
			} else {
				System.Diagnostics.Debug.Fail("謎のキーコード：" + key);
				return null;
			}
		}

		/// <summary>
		/// Keysからキーの名前を取得
		/// </summary>
		public static string GetKeyName(Keys key, Modifiers modifiers) {
			StringBuilder str = new StringBuilder();
			if ((modifiers & Modifiers.Ctrl) != 0) {
				str.Append("Ctrl+");
			}
			if ((modifiers & Modifiers.Alt) != 0) {
				str.Append("Alt+");
			}
			if ((modifiers & Modifiers.Shift) != 0) {
				str.Append("Shift+");
			}
			if ((modifiers & Modifiers.Win) != 0) {
				str.Append("Win+");
			}
			str.Append(GetKeyName(key));
			return str.ToString();
		}

		/// <summary>
		/// キーの名前からKeysの取得
		/// </summary>
		public static Pair<Keys?, Modifiers> GetKeyWithModifiers(string str) {
			Modifiers modifiers = 0;
			List<string> m = new List<string>(str.Split('+')); // 手抜き。
			if (m.Contains("Ctrl")) {
				modifiers = Modifiers.Ctrl;
			}
			if (m.Contains("Alt")) {
				modifiers |= Modifiers.Alt;
			}
			if (m.Contains("Shift")) {
				modifiers |= Modifiers.Shift;
			}
			if (m.Contains("Win")) {
				modifiers |= Modifiers.Win;
			}
			Keys? key = GetKey(m[m.Count - 1]);
			return key.HasValue ?
				new Pair<Keys?, Modifiers>(key.Value, modifiers) :
				new Pair<Keys?, Modifiers>(null, 0);
		}

		/// <summary>
		/// キーの名前からKeysの取得
		/// </summary>
		public static Keys? GetKey(string str) {
			Keys key;
			if (keyNameToKeys.TryGetValue(str, out key)) {
				return key;
			} else {
				System.Diagnostics.Debug.Fail("未定義のキー名っぽい：" + str);
				return null;
			}
		}

		/// <summary>
		/// キーの名前からOSの仮想キーコードの取得
		/// 遅いので注意。
		/// </summary>
		public static Pair<SystemKeys, Modifiers> GetVKey(string str) {
			Pair<Keys?, Modifiers> p = GetKeyWithModifiers(str);
			SystemKeys n = p.First.HasValue ? KeysToVKey(p.First.Value) : SystemKeys.None;
			return new Pair<SystemKeys, Modifiers>(n, p.Second);
		}

		/// <summary>
		/// OSの仮想キーコードからKeysへの変換。無ければnull。
		/// </summary>
		public static Keys? VKeyToKeys(SystemKeys vkey) {
			Keys key;
			if (vkeyToKeys.TryGetValue(vkey, out key)) {
				return key;
			} else {
				return null;
			}
		}

		/// <summary>
		/// KeysからOSの仮想キーコードへの変換。無ければNone。
		/// 遅いので注意。
		/// </summary>
		public static SystemKeys KeysToVKey(Keys? key) {
			return key.HasValue ? KeysToVKey(key.Value) : SystemKeys.None;
		}

		/// <summary>
		/// KeysからOSの仮想キーコードへの変換。無ければNone。
		/// 遅いので注意。
		/// </summary>
		public static SystemKeys KeysToVKey(Keys key) {
			foreach (KeyValuePair<SystemKeys, Keys> p in vkeyToKeys) {
				if (p.Value == key) {
					return p.Key;
				}
			}
			return SystemKeys.None;
		}

		/// <summary>
		/// 現在の修飾キー状態を取得(現在のスレッドに属する窓がある場合)
		/// </summary>
		public static Modifiers GetModifiers()  {
			Modifiers n = 0;
			if (GetKeyState(SystemKeys.ControlKey) < 0) { n = Modifiers.Ctrl; }
			if (GetKeyState(SystemKeys.Menu) < 0) { n |= Modifiers.Alt; }
			if (GetKeyState(SystemKeys.ShiftKey) < 0) { n |= Modifiers.Shift; }
			if (GetKeyState(SystemKeys.LWin) < 0 || GetKeyState(SystemKeys.RWin) < 0) {
				n |= Modifiers.Win;
			}
			return n;
		}
		/// <summary>
		/// 現在の修飾キー状態を取得(窓無しスレッド版)
		/// </summary>
		public static Modifiers GetModifiersAsync() {
			// GetModifiers()をコピペして、GetKeyState → GetAsyncKeyStateしただけ。
			Modifiers n = 0;
			if (GetAsyncKeyState(SystemKeys.ControlKey) < 0) { n = Modifiers.Ctrl; }
			if (GetAsyncKeyState(SystemKeys.Menu) < 0) { n |= Modifiers.Alt; }
			if (GetAsyncKeyState(SystemKeys.ShiftKey) < 0) { n |= Modifiers.Shift; }
			if (GetAsyncKeyState(SystemKeys.LWin) < 0 || GetAsyncKeyState(SystemKeys.RWin) < 0) {
				n |= Modifiers.Win;
			}
			return n;
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		extern static short GetKeyState(SystemKeys vKey);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		extern static short GetAsyncKeyState(SystemKeys vKey);
	}
}
