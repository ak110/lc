using Launcher.Infrastructure;

namespace Launcher.Core;

public sealed class Data : ConfigStore
{
    public long WindowHandle { get; set; }

    /// <summary>スケジューラーの最終チェック時刻 (見逃し検出用)</summary>
    public DateTime SchedulerLastCheckTime { get; set; }

    #region Serialize/Deserialize

    /// <summary>
    /// 書き込み
    /// </summary>
    public bool Save(Action<ConfigSaveFailure> notify, string? baseName = null) => Store.Save(this, notify, baseName);

    /// <summary>
    /// 読み込み
    /// </summary>
    public static ConfigLoadResult<Data> Load(string? baseName = null) => Store.Load(baseName);

    public static ConfigFile<Data> Store { get; } = new(".dat", protectOriginal: false);

    #endregion
}
