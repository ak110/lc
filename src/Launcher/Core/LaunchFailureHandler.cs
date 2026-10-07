using System.ComponentModel;

namespace Launcher.Core;

/// <summary>起動境界のキャンセルと失敗を分類して通知先へ渡す。</summary>
public static class LaunchFailureHandler
{
    public static bool Execute(Action launch, Action<Exception> log, Action<Exception> notify)
    {
        try
        {
            launch();
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return false;
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
        {
            log(ex);
            notify(ex);
            return false;
        }
    }
}
