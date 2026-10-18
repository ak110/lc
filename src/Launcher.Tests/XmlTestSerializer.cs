using System.Xml.Serialization;

namespace Launcher.Tests;

/// <summary>
/// 設定データのXML表現を検査するための直列化。製品は<c>ConfigFile</c>経由でファイルへ読み書きするため、
/// 文字列やファイルを直接扱う処理はテストだけが持つ。
/// </summary>
internal static class XmlTestSerializer
{
    public static string SerializeToString<T>(T value)
    {
        using var writer = new StringWriter();
        new XmlSerializer(typeof(T)).Serialize(writer, value);
        return writer.ToString();
    }

    public static T DeserializeFromString<T>(string xml)
    {
        using var reader = new StringReader(xml);
        return (T)new XmlSerializer(typeof(T)).Deserialize(reader)!;
    }

    public static T DeserializeFromFile<T>(string fileName)
    {
        using var stream = File.OpenRead(fileName);
        return (T)new XmlSerializer(typeof(T)).Deserialize(stream)!;
    }
}
