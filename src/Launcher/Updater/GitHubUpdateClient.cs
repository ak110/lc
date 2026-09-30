using System.Net.Http;
using System.Text.Json;

namespace Launcher.Updater;

/// <summary>
/// GitHub Pages経由の更新チェッククライアント (APIレートリミット回避)
/// </summary>
public static class GitHubUpdateClient
{
    private const string VersionUrl = "https://ak110.github.io/lc/version.json";

    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = {
            { "User-Agent", "Launcher-UpdateClient" },
        },
    };

    /// <summary>
    /// 最新リリース情報を取得
    /// </summary>
    public static async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        var response = await _httpClient.GetAsync(VersionUrl).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<GitHubRelease>(json);
    }

    /// <summary>
    /// 更新が利用可能か判定
    /// </summary>
    public static bool IsUpdateAvailable(GitHubRelease? release)
    {
        if (release?.TagName is not { } tag || !tag.StartsWith('v')) return false;
        // 配布タグはvX.Y.Z。公開情報が古い場合にもダウングレードを案内しない。
        return Version.TryParse(tag.AsSpan(1), out var available)
            && available.Build >= 0 && available.Revision < 0
            && Version.TryParse(Infrastructure.AppVersion.VersionString, out var current)
            && available > current;
    }
}
