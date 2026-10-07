using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class UnexpectedUiExceptionTests
{
    [Fact]
    public void WndProcの表示処理例外はログと可視owner付き報告画面へ渡る() => Run(() =>
    {
        using var data = CreateData();
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
            launcher.HideWindow();
            string marker = "WndProc-" + Guid.NewGuid().ToString("N");
            bool failed = false;
            launcher.VisibleChanged += (_, _) =>
            {
                if (!launcher.Visible || failed) return;
                failed = true;
                throw new InvalidOperationException(marker);
            };
            // 常駐プロセスが受け取る既存のSHOWHIDEメッセージを送る。
            ObserveReport(() => new WindowHelper(host.Handle).SendMessage(
                0x8000, (nint)0x11747b79, (nint)0x14d94a96), launcher, marker, host.PrepareShutdown);
            failed.Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });

    [Fact]
    public void ボタン表示の例外はログと可視owner付き報告画面へ渡る() => Run(() =>
    {
        using var data = CreateData();
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            var launcher = host.OwnedForms.OfType<ButtonLauncherForm>().Single();
            host.ButtonLauncherData.IsLocked = true;
            string marker = "Button-" + Guid.NewGuid().ToString("N");
            bool failed = false;
            launcher.VisibleChanged += (_, _) =>
            {
                if (!launcher.Visible || failed) return;
                failed = true;
                throw new InvalidOperationException(marker);
            };
            ObserveReport(launcher.ShowLauncher, launcher, marker, host.PrepareShutdown);
            failed.Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });

    [Fact]
    public void 実キーフックのハンドル取得例外は同期通知せずUIへ配送する() => Run(() =>
    {
        using var data = CreateData("F12");
        using var host = new HandleFailureHost(data.BaseName);
        try
        {
            host.Show();
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
            launcher.ShowWindow();
            string marker = "Hook-" + Guid.NewGuid().ToString("N");
            host.FailNextHandleCreation(marker);
            ObserveReport(() =>
            {
                keybd_event(0x7B, 0, 0, nint.Zero); // 設定で選択できるF12
                keybd_event(0x7B, 0, 2, nint.Zero);
            }, launcher, marker, host.PrepareShutdown);
            host.FailureRaised.Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });

    static UiHostData CreateData(string hotKey = "") => new(new Config
    {
        TrayIcon = false,
        HotKey = hotKey,
        WindowHideNoActive = false,
        ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight,
    });

    [Fact]
    public void コマンド実行の要求生成例外は通常の起動失敗通知と分けて報告する() => Run(() =>
    {
        using var data = CreateData();
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
            // 孤立サロゲートはパス正規化のString.NormalizeでArgumentExceptionになる。
            var command = new Command { Name = "受入コマンド", FileName = "invalid\uD800command.exe" };
            host.CommandList.Add(command);
            host.RefreshCommandLauncherFormCommandList();
            var input = (TextBox)launcher.Controls.Find("textBox1", searchAllChildren: true).Single();
            ObserveReport(() =>
            {
                launcher.ShowWindow();
                launcher.Visible.Should().BeTrue();
                input.Text = command.Name;
                input.Focus().Should().BeTrue();
                var execute = (Button)launcher.AcceptButton!;
                execute.Visible.Should().BeTrue();
                execute.Enabled.Should().BeTrue();
                execute.PerformClick();
            }, launcher, "System.ArgumentException", host.PrepareShutdown);
        }
        finally
        {
            host.CommandList.Commands.Clear();
            host.Close();
        }
    });

    static void ObserveReport(Action trigger, Form expectedOwner, string marker, Action shutdown)
    {
        string logDirectory = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "logs");
        var priorLogs = Directory.Exists(logDirectory)
            ? Directory.EnumerateFiles(logDirectory, "*.log").ToDictionary(path => path, path => File.ReadAllText(path).Length)
            : new Dictionary<string, int>();
        using var context = new ApplicationContext();
        using var timer = new System.Windows.Forms.Timer { Interval = 10 };
        var elapsed = Stopwatch.StartNew();
        bool triggered = false;
        bool triggering = false;
        bool shown = false;
        bool ownerMatches = false;
        bool ownerVisible = false;
        bool nativeDialog = false;
        bool windowClassFailure = false;
        bool loggedBeforeDisplay = false;
        bool messageMatches = false;
        Exception? escaped = null;
        bool completed = false;
        void Complete()
        {
            if (completed) return;
            completed = true;
            // 終了処理もメッセージループ内で検査し、既定の例外ダイアログへ漏らさない。
            try { shutdown(); }
#pragma warning disable CA1031 // 終了処理の失敗も試験結果へ返す
            catch (Exception ex) { escaped ??= ex; }
#pragma warning restore CA1031
            context.ExitThread();
        }
        timer.Tick += (_, _) =>
        {
            if (!triggered)
            {
                triggered = true;
                triggering = true;
                try { trigger(); }
#pragma warning disable CA1031 // メッセージループ内の失敗をループ終了後のアサーションへ返す
                catch (Exception ex) { escaped = ex; }
#pragma warning restore CA1031
                finally { triggering = false; }
                if (shown || escaped is not null) Complete();
                return;
            }
            EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
            {
                var name = new StringBuilder(256);
                if (GetClassName(window, name, name.Capacity) == 0)
                {
                    windowClassFailure = true;
                    return true;
                }
                if (name.ToString() == "#32770")
                {
                    nativeDialog = true;
                    PostMessage(window, 0x0010, nint.Zero, nint.Zero);
                }
                return true;
            }, nint.Zero);
            var report = Application.OpenForms.OfType<ErrorReporterForm>().SingleOrDefault();
            if (report is { Visible: true })
            {
                shown = report.Visible;
                ownerMatches = ReferenceEquals(report.Owner, expectedOwner);
                ownerVisible = expectedOwner.Visible;
                messageMatches = report.Controls.Cast<Control>().Any(control => control.Text.Contains(marker, StringComparison.Ordinal));
                loggedBeforeDisplay = Directory.Exists(logDirectory) && Directory.EnumerateFiles(logDirectory, "*.log")
                    .Any(path =>
                    {
                        string content = File.ReadAllText(path);
                        int start = priorLogs.GetValueOrDefault(path);
                        return content[Math.Min(start, content.Length)..].Contains(marker, StringComparison.Ordinal);
                    });
                report.DialogResult = DialogResult.Ignore;
                report.Close();
                if (!triggering) Complete();
            }
            if (elapsed.Elapsed > TimeSpan.FromSeconds(60))
            {
                Complete();
            }
        };
        timer.Start();
        Application.Run(context);
        escaped.Should().BeNull();
        shown.Should().BeTrue();
        ownerMatches.Should().BeTrue();
        ownerVisible.Should().BeTrue();
        messageMatches.Should().BeTrue();
        loggedBeforeDisplay.Should().BeTrue();
        nativeDialog.Should().BeFalse();
        windowClassFailure.Should().BeFalse();
    }

    static void Run(Action action) => UiAcceptance.Run(() =>
    {
        // Program.MainのAppBase.Initializeと同じUI例外配送をこのSTAへ登録する。
        // テストはホストだけを構築するため、起動時の登録と試験後の解除を明示する。
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException, threadScope: true);
        ThreadExceptionEventHandler handler = (_, e) => ErrorReporter.Instance.OnException(e.Exception);
        Application.ThreadException += handler;
        try { action(); }
        finally { Application.ThreadException -= handler; }
    });

    sealed class HandleFailureHost(string baseName) : ApplicationHostForm(baseName)
    {
        string? failureMessage;
        public bool FailureRaised { get; private set; }

        public void FailNextHandleCreation(string message)
        {
            DestroyHandle();
            failureMessage = message;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                // NativeWindow.Callback内へ投げるとWinFormsのJIT処理に捕捉される。
                // HWND生成前のmanaged getterで例外を発生させ、HookManagerの取得境界を検査する。
                if (failureMessage is not null)
                {
                    string message = failureMessage;
                    failureMessage = null;
                    FailureRaised = true;
                    throw new InvalidOperationException(message);
                }
                return base.CreateParams;
            }
        }
    }

    delegate bool EnumWindowCallback(nint window, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumThreadWindows(uint threadId, EnumWindowCallback callback, nint parameter);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(nint window, StringBuilder className, int count);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, nint extraInfo);
}
