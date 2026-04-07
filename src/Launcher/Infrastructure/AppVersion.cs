using System.Reflection;

namespace Launcher.Infrastructure;

/// <summary>
/// アプリケーションのバージョン情報を一元管理する
/// </summary>
public static class AppVersion
{
    /// <summary>バージョン文字列 (例: "1.0.0")</summary>
    public static string VersionString { get; }

    /// <summary>リリースタグと同形式 (例: "v1.0.0")</summary>
    public static string TagName { get; }

    /// <summary>タイトルバー表示用 (例: "らんちゃ v1.0.0")</summary>
    public static string Title { get; }

    static AppVersion()
    {
        // AssemblyInformationalVersionから取得 (+commithash部分は除去)
        var assembly = Assembly.GetEntryAssembly();
        string? version = assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (version is not null)
        {
            int plusIndex = version.IndexOf('+');
            if (plusIndex >= 0) version = version[..plusIndex];
        }
        VersionString = version ?? assembly?.GetName().Version?.ToString(3) ?? "unknown";
        TagName = "v" + VersionString;
        Title = "らんちゃ v" + VersionString;
    }
}
