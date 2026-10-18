using System.Text;

namespace Launcher.Infrastructure;

/// <summary>
/// Themis::CConfig 形式の cfg ファイルを読み込む。
/// </summary>
public sealed class LegacyConfigReader
{
    Dictionary<string, string> data = new Dictionary<string, string>();

    /// <summary>
    /// stream から読み込む。
    /// </summary>
#pragma warning disable CA2000 // StreamReader の所有権は this() に委譲される。
    public LegacyConfigReader(Stream stream, bool toBeClose)
        : this(new StreamReader(stream, Encoding.Default), true)
#pragma warning restore CA2000
    {
        if (toBeClose) stream.Dispose();
    }
    /// <summary>
    /// TextReader から読み込む。
    /// </summary>
    public LegacyConfigReader(TextReader reader, bool toBeClose)
    {
        while (true)
        {
            string? line = reader.ReadLine();
            if (line is null) break;
            int n = line.IndexOf(" = ");
            if (0 <= n)
            {
                data[line.Substring(0, n)] = line.Substring(n + 3);
            }
        }
        if (toBeClose) reader.Dispose();
    }

    #region Dictionary 互換のメンバ

    public bool ContainsKey(string key)
    {
        return data.ContainsKey(key);
    }

    public Dictionary<string, string>.KeyCollection Keys
    {
        get { return data.Keys; }
    }

    #endregion

    /// <summary>
    /// bool 値として取得する。
    /// </summary>
    public bool Bool(string n)
    {
        return bool.Parse(data[n]);
    }
    /// <summary>
    /// 整数として取得する。
    /// </summary>
    public int Num(string n)
    {
        return int.Parse(data[n]);
    }
    /// <summary>
    /// エスケープシーケンスを展開した文字列として取得する。
    /// </summary>
    public string EscapedString(string n)
    {
        string str = data[n];
        var builder = new StringBuilder();
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
    /// 改行を含まない文字列として取得する。
    /// </summary>
    public string Indirect(string n)
    {
        return data[n];
    }
}
