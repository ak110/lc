#nullable disable

namespace Launcher.Updater;

/// <summary>
/// 更新チェックの記録
/// </summary>
[Serializable]
public class UpdateRecord
{
    /// <summary>
    /// 最後にチェックした日時
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 最後に確認したバージョン (tag_name)
    /// </summary>
    public string LastKnownVersion { get; set; } = "";

    /// <summary>
    /// スキップしたバージョン
    /// </summary>
    public string SkippedVersion { get; set; } = "";
}
