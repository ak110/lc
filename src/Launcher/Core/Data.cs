#nullable disable
using Launcher.Infrastructure;
using Launcher.Updater;

namespace Launcher.Core;

public class Data : ConfigStore
{
    public long WindowHandle { get; set; } = 0;

    public UpdateRecord UpdateRecord { get; set; } = new UpdateRecord();

    #region Serialize/Deserialize

    /// <summary>
    /// 書き込み
    /// </summary>
    public void Serialize()
    {
        Serialize(".dat");
    }

    /// <summary>
    /// 読み込み
    /// </summary>
    public static Data Deserialize()
    {
        try
        {
            return Deserialize<Data>(".dat");
        }
        catch
        {
            return new Data();
        }
    }

    #endregion
}
