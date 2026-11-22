using Launcher.Core;
using Launcher.Infrastructure;

namespace Launcher.Win32;

public static class ResidentMessages
{
    public const int WM_APPMSG = WM.WM_APP + 0;
    public static readonly IntPtr WM_APPMSG_WPARAM = (IntPtr)0x11747b79;
    public static readonly IntPtr WM_APPMSG_SHOWHIDE = (IntPtr)0x14d94a96;
    public static readonly IntPtr WM_APPMSG_RELOAD = (IntPtr)0x338ca4c1;
    public static readonly IntPtr WM_APPMSG_RESTART = (IntPtr)0x6b60850f;
    public static readonly IntPtr WM_APPMSG_SHOWBUTTONLAUNCHER = (IntPtr)0x2a3f7c01;
    public static readonly IntPtr WM_APPMSG_SHOWMEMO = (IntPtr)0x5e2d9f13;

#pragma warning disable CA1031
    public static void TryPostMessageToResident(
        int message, IntPtr wParam, IntPtr lParam,
        string label, string successDescription, string failureDescription, string failureNextAction)
    {
        void ReportFailure(string description) => Console.Error.WriteLine($"{label}: {description} {failureNextAction}");

        try
        {
            var result = Data.Load();
            if (result.Status == ConfigLoadStatus.Failed)
            {
                ReportFailure("実行状態のファイルを読み込めませんでした。");
                return;
            }
            Data data = result.Value;
            WindowHelper window =
                new WindowHelper(checked((IntPtr)data.WindowHandle));
            if (window.PostMessage(message, wParam, lParam))
                Console.WriteLine($"{label}: {successDescription}");
            else
                ReportFailure(failureDescription);
        }
        catch (Exception ex)
        {
            ReportFailure(ex.Message);
        }
    }
#pragma warning restore CA1031

    public static bool Post(IntPtr window, IntPtr command) =>
        new WindowHelper(window).PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, command);
}
