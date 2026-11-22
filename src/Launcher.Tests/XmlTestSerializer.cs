using System.Xml.Serialization;

namespace Launcher.Tests;

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
