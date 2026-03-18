#nullable disable
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
            string appDir = Path.GetDirectoryName(Application.ExecutablePath);
            foreach (var file in Directory.GetFiles(appDir, "*.old"))
            {
                try { File.Delete(file); } catch { }
            }
        }
        catch
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
            var ex = e.ExceptionObject as Exception;
            var message = ex != null
                ? $"未処理の例外が発生しました:\n{ex.Message}\n\n{ex.StackTrace}"
                : $"未処理の例外が発生しました:\n{e.ExceptionObject}";
            System.Diagnostics.Debug.WriteLine(message);
            MessageBox.Show(message, "致命的なエラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        // 更新後の.oldファイルをクリーンアップ
        CleanupOldFiles();

        using (AppBase.Initializer app = new AppBase.Initializer())
        using (SingleInstance singleInstance = new SingleInstance())
        {
            bool exit = false;
            // 引数の処理
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "/close")
                {
                    try
                    {
                        Data data = Data.Deserialize();
                        WindowHelper window =
                            new WindowHelper(checked((IntPtr)data.WindowHandle));
                        window.PostMessage(WM.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                    catch
                    {
                        // とりあえずエラーは無視。
                    }
                    return;
                }
                else if (args[i] == "/restart")
                {
                    try
                    {
                        Data data = Data.Deserialize();
                        WindowHelper window =
                            new WindowHelper(checked((IntPtr)data.WindowHandle));
                        window.PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RESTART);
                    }
                    catch
                    {
                        // とりあえずエラーは無視。
                    }
                    return;
                }
                else if (File.Exists(args[i]) || Directory.Exists(args[i]))
                {
                    Command command = Command.FromFile(args[i]);
                    new ReplaceEnvList(Config.Deserialize().ReplaceEnv).Replace(command);
                    using (EditCommandForm form = new EditCommandForm(command))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            CommandList commandList = CommandList.Deserialize(".cmd.cfg");
                            commandList.Add(command);
                            commandList.Serialize(".cmd.cfg");

                            try
                            {
                                Data data = Data.Deserialize();
                                WindowHelper window =
                                    new WindowHelper(checked((IntPtr)data.WindowHandle));
                                window.PostMessage(WM_APPMSG, WM_APPMSG_WPARAM, WM_APPMSG_RELOAD);
                            }
                            catch
                            {
                                // とりあえずエラーは無視。
                            }
                        }
                        exit = true;
                    }
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
}
