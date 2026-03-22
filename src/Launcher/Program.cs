using System.Windows.Forms;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Launcher.Win32;

namespace Launcher;

static class Program
{
    public const int WM_APPMSG = WM.WM_APP + 0;
    public static readonly IntPtr WM_APPMSG_WPARAM = (IntPtr)0x11747b79; // ←誤爆防止用ダミー。
    public static readonly IntPtr WM_APPMSG_SHOWHIDE = (IntPtr)0x14d94a96;
    public static readonly IntPtr WM_APPMSG_RELOAD = (IntPtr)0x338ca4c1;
    public static readonly IntPtr WM_APPMSG_RESTART = (IntPtr)0x6b60850f;
    public static readonly IntPtr WM_APPMSG_SHOWBUTTONLAUNCHER = (IntPtr)0x2a3f7c01;

    /// <summary>
    /// 更新後に残った.oldファイルを削除する
    /// </summary>
    static void CleanupOldFiles()
    {
        try
        {
            string? appDir = Path.GetDirectoryName(Application.ExecutablePath);
            if (appDir == null) return;
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
    /// アプリケーションのメイン エントリ ポイントです。
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // 未処理例外のキャッチ（UIスレッド以外で発生した例外用）
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var message = e.ExceptionObject is Exception ex
                ? $"未処理の例外が発生しました:\n{ex.Message}\n\n{ex.StackTrace}"
                : $"未処理の例外が発生しました:\n{e.ExceptionObject}";
            System.Diagnostics.Debug.WriteLine(message);
            MessageBox.Show(message, "致命的なエラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        // 更新後の.oldファイルをクリーンアップ
        CleanupOldFiles();

        using var app = new AppBase.Initializer();
        using var singleInstance = new SingleInstance();

        // WinExeでもコマンドプロンプトからの実行時に結果を表示するため、親コンソールにアタッチ
        if (args.Length > 0)
            NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);

        bool exit = false;
        // 引数の処理
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "/close")
            {
                // エントリポイントでのIPC送信: 常駐プロセスが存在しない場合など多様な失敗がありうる
#pragma warning disable CA1031 // エントリポイントのIPC送信は失敗を握りつぶす必要がある
                try
                {
                    Data data = Data.Deserialize();
                    WindowHelper window =
                        new WindowHelper(checked((IntPtr)data.WindowHandle));
                    if (window.PostMessage(WM.WM_CLOSE, IntPtr.Zero, IntPtr.Zero))
                        Console.WriteLine("/close: 終了メッセージを送信しました。");
                    else
                        Console.Error.WriteLine("/close: メッセージ送信に失敗しました。");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"/close: {ex.Message}");
                }
#pragma warning restore CA1031
                return;
            }
            else if (args[i] == "/restart")
            {
#pragma warning disable CA1031 // エントリポイントのIPC送信は失敗を握りつぶす必要がある
                try
                {
                    Data data = Data.Deserialize();
                    WindowHelper window =
                        new WindowHelper(checked((IntPtr)data.WindowHandle));
                    if (window.PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RESTART))
                        Console.WriteLine("/restart: 再起動メッセージを送信しました。");
                    else
                        Console.Error.WriteLine("/restart: メッセージ送信に失敗しました。");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"/restart: {ex.Message}");
                }
#pragma warning restore CA1031
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

#pragma warning disable CA1031 // エントリポイントのIPC送信は失敗を握りつぶす必要がある
                    try
                    {
                        Data data = Data.Deserialize();
                        WindowHelper window =
                            new WindowHelper(checked((IntPtr)data.WindowHandle));
                        if (window.PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RELOAD))
                            Console.WriteLine("コマンド登録: リロードメッセージを送信しました。");
                        else
                            Console.Error.WriteLine("コマンド登録: リロードメッセージの送信に失敗しました。");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"コマンド登録: リロード通知失敗: {ex.Message}");
                    }
#pragma warning restore CA1031
                }
                exit = true;
            }
            else
            {
                // とりあえず無視
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
        using var dummyForm = new DummyForm();
        Application.Run(dummyForm);
    }
}
