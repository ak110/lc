using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Launcher.Win32;

namespace Launcher;

static class Program
{
    /// <summary>
    /// アプリケーションのメインエントリポイント。
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        DiagnosticLog.Info("App.Start",
            $"pid={Environment.ProcessId} version={Application.ProductVersion}");

        // UIスレッド以外で発生した未処理例外を捕捉する。
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var message = e.ExceptionObject is Exception ex
                ? $"未処理の例外が発生しました:\n{ex.Message}\n\n{ex.StackTrace}"
                : $"未処理の例外が発生しました:\n{e.ExceptionObject}";
            if (e.ExceptionObject is Exception exForLog)
            {
                DiagnosticLog.Error("App.HandleException", exForLog);
            }
            else
            {
                DiagnosticLog.Error("App.HandleException", message);
            }
            MessageBox.Show(message, "致命的なエラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

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
                ResidentMessages.TryPostMessageToResident(
                    WM.WM_CLOSE, IntPtr.Zero, IntPtr.Zero,
                    "/close",
                    "終了メッセージを送信しました。",
                    "メッセージの送信に失敗しました。");
                return;
            }
            else if (args[i] == "/restart")
            {
                ResidentMessages.TryPostMessageToResident(
                    ResidentMessages.WM_APPMSG, ResidentMessages.WM_APPMSG_WPARAM, ResidentMessages.WM_APPMSG_RESTART,
                    "/restart",
                    "再起動メッセージを送信しました。",
                    "メッセージの送信に失敗しました。");
                return;
            }
            else if (File.Exists(args[i]) || Directory.Exists(args[i]))
            {
                Command command = CommandFactory.FromFile(args[i]);
                new ReplaceEnvList(Config.Load().Value.ReplaceEnv).Replace(command);
                using var form = new EditCommandForm(command);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    bool saved = CommandList.AddAndSave(command,
                        failure => MessageBox.Show(failure.Message, AppVersion.Title,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning),
                        result => MessageBox.Show(
                            $"コマンド一覧({result.Kind})を読み込めないため、コマンドを追加しませんでした。\r\n原因: {result.Error?.Message}\r\n\r\n"
                                + "らんちゃを起動すると、読込失敗の通知からバックアップを使って復元できます。",
                            AppVersion.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning));
                    if (!saved)
                    {
                        exit = true;
                        continue;
                    }

                    ResidentMessages.TryPostMessageToResident(
                        ResidentMessages.WM_APPMSG, ResidentMessages.WM_APPMSG_WPARAM, ResidentMessages.WM_APPMSG_RELOAD,
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
