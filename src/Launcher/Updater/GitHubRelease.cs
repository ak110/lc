using System.Text.Json.Serialization;

namespace Launcher.Updater;

/// <summary>
/// リリース情報モデル（GitHub Pages上のversion.jsonから取得）
/// </summary>
public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;
}
