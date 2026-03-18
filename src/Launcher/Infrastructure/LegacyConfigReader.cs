using System.Drawing;
using System.Text;

namespace Launcher.Infrastructure;

/// <summary>
/// Themis::CConfig的なcfgファイルを読み込む
/// </summary>
public class LegacyConfigReader
{
    Dictionary<string, string> data = new Dictionary<string, string>();

    /// <summary>
    /// ファイル名を指定して読み込む
    /// </summary>
    public LegacyConfigReader(string fileName)
        : this(File.Open(fileName, FileMode.Open, FileAccess.Read), true)
    {
    }
    /// <summary>
    /// streamから読み込む
    /// </summary>
#pragma warning disable CA2000 // StreamReaderの所有権はthis()に移譲される
    public LegacyConfigReader(Stream stream, bool toBeClose)
        : this(new StreamReader(stream, Encoding.Default), true)
#pragma warning restore CA2000
    {
        if (toBeClose) stream.Dispose();
    }
    /// <summary>
    /// TextReaderから読み込む
    /// </summary>
    public LegacyConfigReader(TextReader reader, bool toBeClose)
    {
        while (true)
        {
            string? line = reader.ReadLine();
            if (line == null) break;
            int n = line.IndexOf(" = ");
            if (0 <= n)
            {
                data[line.Substring(0, n)] = line.Substring(n + 3);
            }
        }
        if (toBeClose) reader.Dispose();
    }

    #region Dictionary系は適当に。

    public bool ContainsKey(string key)
    {
        return data.ContainsKey(key);
    }

    public Dictionary<string, string>.KeyCollection Keys
    {
        get { return data.Keys; }
    }

    public Dictionary<string, string>.ValueCollection Values
    {
        get { return data.Values; }
    }

    #endregion

    /// <summary>
    /// 一応Dictionaryの直接取得も、開きにしておきます。
    /// </summary>
    public Dictionary<string, string> Data
    {
        get { return data; }
    }

    /// <summary>
    /// bool値として取得
    /// </summary>
    public bool Bool(string n)
    {
        return bool.Parse(data[n]);
    }
    /// <summary>
    /// 整数として取得
    /// </summary>
    public int Num(string n)
    {
        return int.Parse(data[n]);
    }
    /// <summary>
    /// 倍精度浮動小数点として取得
    /// </summary>
    public double Float(string n)
    {
        return double.Parse(data[n]);
    }
    /// <summary>
    /// エスケープシーケンスを含む文字列
    /// </summary>
    public string EscapedString(string n)
    {
        string str = data[n];
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < str.Length - 1; i++)
        {
            if (str[i] == '\\')
            {
                switch (str[i + 1])
                {
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
            }
            else
            {
                builder.Append(str[i]);
            }
        }
        if (1 <= str.Length)
        {
            builder.Append(str[str.Length - 1]);
        }
        return builder.ToString();
    }
    /// <summary>
    /// 改行を含まない文字列として取得
    /// </summary>
    public string Indirect(string n)
    {
        return data[n];
    }
    /// <summary>
    /// Pointとして取得
    /// </summary>
    public static Point Point(string n)
    {
        string[] s = n.Split(',');
        if (s.Length != 2) throw new FileLoadException("設定ファイルの項目 '" + n + "' の自動解析に失敗しました");
        return new Point(int.Parse(s[0].Trim()), int.Parse(s[1].Trim()));
    }
    /// <summary>
    /// Sizeとして取得
    /// </summary>
    public static Size Size(string n)
    {
        string[] s = n.Split(',');
        if (s.Length != 2) throw new FileLoadException("設定ファイルの項目 '" + n + "' の自動解析に失敗しました");
        return new Size(int.Parse(s[0].Trim()), int.Parse(s[1].Trim()));
    }
    /// <summary>
    /// Rectangleとして取得
    /// </summary>
    public static Rectangle Rect(string n)
    {
        string[] s = n.Split(',');
        if (s.Length != 4) throw new FileLoadException("設定ファイルの項目 '" + n + "' の自動解析に失敗しました");
        return new Rectangle(
            int.Parse(s[0].Trim()),
            int.Parse(s[1].Trim()),
            int.Parse(s[2].Trim()),
            int.Parse(s[3].Trim()));
    }
}
