using System.Windows.Forms;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Launcher.Win32;

namespace Launcher;

static class Program
{
    public const int WM_APPMSG = WM.WM_APP + 0;
    public static readonly IntPtr WM_APPMSG_WPARAM = (IntPtr)0x11747b79; // 誤検出を防ぐためのダミー値。
    public static readonly IntPtr WM_APPMSG_SHOWHIDE = (IntPtr)0x14d94a96;
    public static readonly IntPtr WM_APPMSG_RELOAD = (IntPtr)0x338ca4c1;
    public static readonly IntPtr WM_APPMSG_RESTART = (IntPtr)0x6b60850f;
    public static readonly IntPtr WM_APPMSG_SHOWBUTTONLAUNCHER = (IntPtr)0x2a3f7c01;

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
    static void TryPostMessageToResident(
        int message, IntPtr wParam, IntPtr lParam,
        string label, string successDescription, string failureDescription)
    {
        try
        {
            Data data = Data.Deserialize();
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

    /// <summary>
    /// 更新後に残った.oldファイルを削除する。
    /// </summary>
    static void CleanupOldFiles()
    {
        try
        {
            string? appDir = Path.GetDirectoryName(Application.ExecutablePath);
            if (appDir is null) return;
            foreach (var file in Directory.GetFiles(appDir, "*.old"))
            {
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException)
        {
            // クリーンアップ失敗は無視
        }
        catch (UnauthorizedAccessException)
        {
            // クリーンアップ失敗は無視
        }
    }

    /// <summary>
    /// アプリケーションのメインエントリポイント。
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // UIスレッド以外で発生した未処理例外を捕捉する。
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var message = e.ExceptionObject is Exception ex
                ? $"未処理の例外が発生しました:\n{ex.Message}\n\n{ex.StackTrace}"
                : $"未処理の例外が発生しました:\n{e.ExceptionObject}";
            System.Diagnostics.Debug.WriteLine(message);
            MessageBox.Show(message, "致命的なエラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        // 更新後に残った .old ファイルを削除する。
        CleanupOldFiles();

        using var app = new AppBase.Initializer();
        using var singleInstance = new SingleInstance();

        // WinExe でもコマンドプロンプトから実行した際に結果を表示するため、親コンソールへアタッチする。
        if (args.Length > 0)
            NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);

        bool exit = false;
        // 引数の処理
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "/close")
            {
                TryPostMessageToResident(
                    WM.WM_CLOSE, IntPtr.Zero, IntPtr.Zero,
                    "/close",
                    "終了メッセージを送信しました。",
                    "メッセージの送信に失敗しました。");
                return;
            }
            else if (args[i] == "/restart")
            {
                TryPostMessageToResident(
                    WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RESTART,
                    "/restart",
                    "再起動メッセージを送信しました。",
                    "メッセージの送信に失敗しました。");
                return;
            }
            else if (File.Exists(args[i]) || Directory.Exists(args[i]))
            {
                Command command = Command.FromFile(args[i]);
                new ReplaceEnvList(Config.Deserialize().ReplaceEnv).Replace(command);
                using var form = new EditCommandForm(command);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    CommandList commandList = CommandList.Deserialize(".cmd.cfg");
                    commandList.Add(command);
                    commandList.Serialize(".cmd.cfg");

                    TryPostMessageToResident(
                        WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RELOAD,
                        "コマンド登録",
                        "リロードメッセージを送信しました。",
                        "リロードメッセージの送信に失敗しました。");
                }
                exit = true;
            }
            else
            {
                // 認識できない引数は無視する。
            }
        }

        if (exit) return;

        if (!singleInstance.FirstRun)
        {
            SingleInstance.SetActive();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var applicationHostForm = new ApplicationHostForm();
        Application.Run(applicationHostForm);
    }
}
