using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace Toolkit {
	/// <summary>
	/// Themis::CConfigとかのcfgファイルを読み込む
	/// </summary>
	public class LegacyConfigReader {
		Dictionary<string, string> data = new Dictionary<string, string>();

		/// <summary>
		/// ファイル名を指定して読み込み
		/// </summary>
		public LegacyConfigReader(string fileName)
			: this(File.Open(fileName, FileMode.Open, FileAccess.Read), true) {
		}
		/// <summary>
		/// streamから読み込み
		/// </summary>
		public LegacyConfigReader(Stream stream, bool toBeClose)
			: this(new StreamReader(stream, Encoding.Default), true) {
			if (toBeClose) stream.Dispose();
		}
		/// <summary>
		/// TextReaderから読み込み
		/// </summary>
		public LegacyConfigReader(TextReader reader, bool toBeClose) {
			while (true) {
				string line = reader.ReadLine();
				if (line == null) break;
				int n = line.IndexOf(" = ");
				if (0 <= n) {
					data[line.Substring(0, n)] = line.Substring(n + 3);
				}
			}
			if (toBeClose) reader.Dispose();
		}

		#region Dictionary系を適当に。

		public bool ContainsKey(string key) {
			return data.ContainsKey(key);
		}

		public Dictionary<string, string>.KeyCollection Keys {
			get { return data.Keys; }
		}

		public Dictionary<string, string>.ValueCollection Values {
			get { return data.Values; }
		}

		#endregion

		/// <summary>
		/// 一応Dictionaryの直接取得もアリにしてしまう。
		/// </summary>
		public Dictionary<string, string> Data {
			get { return data; }
		}

		/// <summary>
		/// bool値として取得
		/// </summary>
		public bool Bool(string n) {
			return bool.Parse(data[n]);
		}
		/// <summary>
		/// 整数として取得
		/// </summary>
		public int Num(string n) {
			return int.Parse(data[n]);
		}
		/// <summary>
		/// 浮動小数点数として取得
		/// </summary>
		public double Float(string n) {
			return double.Parse(data[n]);
		}
		/// <summary>
		/// エスケープシーケンスを含む文字列
		/// </summary>
		public string EscapedString(string n) {
			string str = data[n];
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < str.Length - 1; i++) {
				if (str[i] == '\\') {
					switch (str[i + 1]) {
					case 'r': builder.Append('\r'); break;
					case 'n': builder.Append('\n'); break;
					case '0': builder.Append('\0'); break;
					case '\\': builder.Append('\\'); break;
					default:
						builder.Append('\\');
						builder.Append(str[i + 1]);
						break;
					}
					i++;
				} else {
					builder.Append(str[i]);
				}
			}
			if (1 <= str.Length) {
				builder.Append(str[str.Length - 1]);
			}
			return builder.ToString();
		}
		/// <summary>
		/// 改行を含まない文字列として取得
		/// </summary>
		public string Indirect(string n) {
			return data[n];
		}
		/// <summary>
		/// Pointとして取得
		/// </summary>
		public Point Point(string n) {
			string[] s = n.Split(',');
			if (s.Length != 2) throw new FileLoadException("設定ファイルの項目 '" + n + "' の字句解析に失敗しました");
			return new Point(int.Parse(s[0].Trim()), int.Parse(s[1].Trim()));
		}
		/// <summary>
		/// Sizeとして取得
		/// </summary>
		public Size Size(string n) {
			string[] s = n.Split(',');
			if (s.Length != 2) throw new FileLoadException("設定ファイルの項目 '" + n + "' の字句解析に失敗しました");
			return new Size(int.Parse(s[0].Trim()), int.Parse(s[1].Trim()));
		}
		/// <summary>
		/// Rectangleとして取得
		/// </summary>
		public Rectangle Rect(string n) {
			string[] s = n.Split(',');
			if (s.Length != 4) throw new FileLoadException("設定ファイルの項目 '" + n + "' の字句解析に失敗しました");
			return new Rectangle(
				int.Parse(s[0].Trim()),
				int.Parse(s[1].Trim()),
				int.Parse(s[2].Trim()),
				int.Parse(s[3].Trim()));
		}
	}
}
