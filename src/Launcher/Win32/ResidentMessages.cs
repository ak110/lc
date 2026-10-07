using Launcher.Core;
using Launcher.Infrastructure;

namespace Launcher.Win32;

/// <summary>常駐プロセスとのメッセージ契約と送信処理。</summary>
public static class ResidentMessages
{
    public const int WM_APPMSG = WM.WM_APP + 0;
    public static readonly IntPtr WM_APPMSG_WPARAM = (IntPtr)0x11747b79; // 誤検出を防ぐためのダミー値。
    public static readonly IntPtr WM_APPMSG_SHOWHIDE = (IntPtr)0x14d94a96;
    public static readonly IntPtr WM_APPMSG_RELOAD = (IntPtr)0x338ca4c1;
    public static readonly IntPtr WM_APPMSG_RESTART = (IntPtr)0x6b60850f;
    public static readonly IntPtr WM_APPMSG_SHOWBUTTONLAUNCHER = (IntPtr)0x2a3f7c01;
    public static readonly IntPtr WM_APPMSG_SHOWMEMO = (IntPtr)0x5e2d9f13;

    /// <summary>
    /// 常駐プロセスへウィンドウメッセージを送信する。
    /// 失敗（常駐プロセスなし、ハンドル無効、ファイル不在など）はstderrへ出力するだけで無視する。
    /// </summary>
    /// <param name="message">送信メッセージID</param>
    /// <param name="wParam">wParam値</param>
    /// <param name="lParam">lParam値</param>
    /// <param name="label">ログ先頭に付けるラベル（例: "/close", "/restart", "コマンド登録"）</param>
    /// <param name="successDescription">成功時に続けて出力する説明（例: "終了メッセージを送信しました。"）</param>
    /// <param name="failureDescription">PostMessage失敗時に続けて出力する説明</param>
#pragma warning disable CA1031 // エントリポイントのIPC送信は失敗を無視する必要がある
    public static void TryPostMessageToResident(
        int message, IntPtr wParam, IntPtr lParam,
        string label, string successDescription, string failureDescription)
    {
        try
        {
            var result = Data.Load();
            if (result.Status == ConfigLoadStatus.Failed)
            {
                Console.Error.WriteLine($"{label}: 実行状態のファイルを読み込めませんでした。");
                return;
            }
            Data data = result.Value;
            WindowHelper window =
                new WindowHelper(checked((IntPtr)data.WindowHandle));
            if (window.PostMessage(message, wParam, lParam))
                Console.WriteLine($"{label}: {successDescription}");
            else
                Console.Error.WriteLine($"{label}: {failureDescription}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{label}: {ex.Message}");
        }
    }
#pragma warning restore CA1031

    public static bool Post(IntPtr window, IntPtr command) =>
        new WindowHelper(window).PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, command);
}
