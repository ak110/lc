using System.Net.Http;
using System.Text.Json;

namespace Launcher.Updater;

/// <summary>
/// GitHub Releases APIを使った更新チェッククライアント
/// </summary>
public static class GitHubUpdateClient
{
    private const string ApiUrl = "https://api.github.com/repos/ak110/lc/releases/latest";

    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = {
            { "User-Agent", "Launcher-UpdateClient" },
            { "Accept", "application/vnd.github.v3+json" },
        },
    };

    /// <summary>
    /// 最新リリース情報を取得
    /// </summary>
    public static async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        var response = await _httpClient.GetAsync(ApiUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubRelease>(json);
    }

    /// <summary>
    /// 更新が利用可能か判定
    /// </summary>
    public static bool IsUpdateAvailable(GitHubRelease? release, UpdateRecord record)
    {
        if (release == null) return false;
        string currentVersion = Infrastructure.AppVersion.TagName;
        // スキップ済みバージョンなら無視
        if (release.TagName == record.SkippedVersion) return false;
        // 既知のバージョンと同じなら無視
        if (release.TagName == currentVersion) return false;
        return release.TagName != record.LastKnownVersion || record.LastKnownVersion != currentVersion;
    }

    /// <summary>
    /// ブラウザでリリースページを開く
    /// </summary>
    public static void OpenReleasePage(GitHubRelease release)
    {
        if (!string.IsNullOrEmpty(release?.HtmlUrl))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = release.HtmlUrl,
                UseShellExecute = true,
            });
        }
    }

}
