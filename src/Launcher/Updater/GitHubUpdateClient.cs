#nullable disable
using System.Net.Http;
using System.Text.Json;

namespace Launcher.Updater;

/// <summary>
/// GitHub Releases APIを使った更新チェッククライアント
/// </summary>
public class GitHubUpdateClient {
    private static readonly HttpClient _httpClient = new() {
        DefaultRequestHeaders = {
            { "User-Agent", "Launcher-UpdateClient" },
            { "Accept", "application/vnd.github.v3+json" },
        },
    };

    private readonly UpdateConfig _config;

    public GitHubUpdateClient(UpdateConfig config) {
        _config = config;
    }

    /// <summary>
    /// 最新リリース情報を取得
    /// </summary>
    public async Task<GitHubRelease> GetLatestReleaseAsync() {
        if (!_config.IsEnabled) return null;

        string url = $"https://api.github.com/repos/{_config.Owner}/{_config.Repository}/releases/latest";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubRelease>(json);
    }

    /// <summary>
    /// 更新チェックが必要か判定
    /// </summary>
    public bool ShouldCheck(UpdateRecord record) {
        if (!_config.IsEnabled) return false;
        if (_config.CheckIntervalDays <= 0) return false;
        return (DateTime.Now - record.LastChecked).TotalDays >= _config.CheckIntervalDays;
    }

    /// <summary>
    /// 更新が利用可能か判定
    /// </summary>
    public bool IsUpdateAvailable(GitHubRelease release, UpdateRecord record) {
        if (release == null) return false;
        string currentVersion = GetCurrentVersion();
        if (string.IsNullOrEmpty(currentVersion)) return false;
        // スキップ済みバージョンなら無視
        if (release.TagName == record.SkippedVersion) return false;
        // 既知のバージョンと同じなら無視
        if (release.TagName == currentVersion) return false;
        return release.TagName != record.LastKnownVersion || record.LastKnownVersion != currentVersion;
    }

    /// <summary>
    /// ブラウザでリリースページを開く
    /// </summary>
    public static void OpenReleasePage(GitHubRelease release) {
        if (!string.IsNullOrEmpty(release?.HtmlUrl)) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = release.HtmlUrl,
                UseShellExecute = true,
            });
        }
    }

    /// <summary>
    /// 現在のバージョン (AssemblyInformationalVersion or AssemblyVersion)
    /// </summary>
    private static string GetCurrentVersion() {
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        if (assembly == null) return null;
        var infoVersion = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);
        if (infoVersion.Length > 0) {
            return ((System.Reflection.AssemblyInformationalVersionAttribute)infoVersion[0]).InformationalVersion;
        }
        return assembly.GetName().Version?.ToString();
    }
}
