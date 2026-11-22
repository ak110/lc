using System.Reflection;

namespace Launcher.Infrastructure;

public static class AppVersion
{
    public static string VersionString { get; }

    public static string Title { get; }

    static AppVersion()
    {
        // 起動ホストではなく製品のAssemblyInformationalVersionを読む。
        var assembly = typeof(AppVersion).Assembly;
        string? version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (version is not null)
        {
            int plusIndex = version.IndexOf('+');
            if (plusIndex >= 0) version = version[..plusIndex];
        }
        VersionString = version ?? assembly.GetName().Version?.ToString(3) ?? "unknown";
        Title = "らんちゃ v" + VersionString;
    }
}
