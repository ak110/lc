using System.Text.Json;
using Launcher.Infrastructure;

namespace Launcher.Updater;

/// <summary>
/// GitHub Pages経由の更新チェッククライアント (APIレートリミット回避)
/// </summary>
public static class GitHubUpdateClient
{
    private const string VersionUrl = "https://ak110.github.io/lc/version.json";

    public static async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        using var request = SharedHttpClient.CreateUpdateRequest(VersionUrl);
        using var response = await SharedHttpClient.Instance.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<GitHubRelease>(json);
    }

    public static bool IsUpdateAvailable(GitHubRelease? release) =>
        IsUpdateAvailable(release, Infrastructure.AppVersion.VersionString);

    public static bool IsUpdateAvailable(GitHubRelease? release, string currentVersionString)
    {
        if (release?.TagName is not { } tag || !tag.StartsWith('v')) return false;
        return Version.TryParse(tag.AsSpan(1), out var available)
            && available.Build >= 0 && available.Revision < 0
            && Version.TryParse(currentVersionString, out var current)
            && available > current;
    }
}
