using Launcher.Infrastructure;

namespace Launcher.Core;

public sealed class Data : ConfigStore
{
    public long WindowHandle { get; set; }

    public DateTime SchedulerLastCheckTime { get; set; }

    #region Serialize/Deserialize

    public bool Save(Action<ConfigSaveFailure> notify, string? baseName = null) => Store.Save(this, notify, baseName);

    public static ConfigLoadResult<Data> Load(string? baseName = null) => Store.Load(baseName);

    public static ConfigFile<Data> Store { get; } = new(".dat", protectOriginal: false);

    #endregion
}
