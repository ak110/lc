using System.IO;
using System.Xml.Serialization;
using Launcher.Core;

namespace Launcher.Tests;

internal sealed class UiHostData : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "lc-ui-" + Guid.NewGuid().ToString("N"));

    public string BaseName => Path.Combine(directory, "launcher");

    public UiHostData(Config config)
    {
        Directory.CreateDirectory(directory);
        Write(".cfg", config);
    }

    public void Write<T>(string extension, T value)
    {
        using var file = File.Create(BaseName + extension);
        new XmlSerializer(typeof(T)).Serialize(file, value);
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
