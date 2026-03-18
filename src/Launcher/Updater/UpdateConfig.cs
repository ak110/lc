#nullable disable
using System.Xml.Serialization;

namespace Launcher.Updater;

/// <summary>
/// GitHub Releases連携の更新設定
/// </summary>
[Serializable]
public class UpdateConfig
{
    /// <summary>
    /// GitHubリポジトリのオーナー
    /// </summary>
    public string Owner { get; set; } = "ak110";

    /// <summary>
    /// GitHubリポジトリ名
    /// </summary>
    public string Repository { get; set; } = "lc";

    /// <summary>
    /// 更新チェック間隔（日数）。0で無効
    /// </summary>
    public int CheckIntervalDays { get; set; } = 7;

    /// <summary>
    /// 更新チェックが有効かどうか
    /// </summary>
    [XmlIgnore]
    public bool IsEnabled => !string.IsNullOrEmpty(Owner) && !string.IsNullOrEmpty(Repository) && CheckIntervalDays > 0;
}
