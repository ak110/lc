using System.IO;
using System.Xml;
using Launcher.Infrastructure;
using Launcher.Updater;

namespace Launcher.Core;

public sealed class Data : ConfigStore
{
    public long WindowHandle { get; set; }

    public UpdateRecord UpdateRecord { get; set; } = new UpdateRecord();

    /// <summary>スケジューラーの最終チェック時刻 (見逃し検出用)</summary>
    public DateTime SchedulerLastCheckTime { get; set; }

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
        catch (InvalidOperationException)
        {
            return new Data();
        }
        catch (XmlException)
        {
            return new Data();
        }
        catch (IOException)
        {
            return new Data();
        }
    }

    #endregion
}
