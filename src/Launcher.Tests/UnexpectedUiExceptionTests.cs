using System.Diagnostics;
using System.IO;
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
    public void WndProcの表示処理例外はログと可視owner付き報告画面へ渡る() => UiAcceptance.Run(() =>
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
                0x8000, (nint)0x11747b79, (nint)0x14d94a96), launcher, marker);
            failed.Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });

    [Fact]
    public void ボタン表示の例外はログと可視owner付き報告画面へ渡る() => UiAcceptance.Run(() =>
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
            ObserveReport(launcher.ShowLauncher, launcher, marker);
            failed.Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });

    [Fact]
    public void 実キーフックのハンドル取得例外は同期通知せずUIへ配送する() => UiAcceptance.Run(() =>
    {
        using var data = CreateData();
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
                keybd_event(0x87, 0, 0, nint.Zero); // F24
                keybd_event(0x87, 0, 2, nint.Zero);
            }, launcher, marker);
            host.FailureRaised.Should().BeTrue();
        }
        finally
        {
            host.Close();
        }
    });

    static UiHostData CreateData() => new(new Config
    {
        TrayIcon = false,
        HotKey = "F24",
        WindowHideNoActive = false,
        ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight,
    });

    [Fact]
    public void コマンド実行の要求生成例外は通常の起動失敗通知と分けて報告する() => UiAcceptance.Run(() =>
    {
        using var data = CreateData();
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
            var command = new Command { Name = "受入コマンド", FileName = "invalid\0command.exe" };
            host.CommandList.Add(command);
            host.RefreshCommandLauncherFormCommandList();
            var input = (TextBox)launcher.Controls.Find("textBox1", searchAllChildren: true).Single();
            input.Text = command.Name;
            input.Focus();
            ObserveReport(() => launcher.AcceptButton!.PerformClick(), launcher, "System.ArgumentException");
        }
        finally
        {
            host.CommandList.Commands.Clear();
            host.Close();
        }
    });

    internal static void ObserveReport(Action trigger, Form expectedOwner, string marker)
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
        bool loggedBeforeDisplay = false;
        bool messageMatches = false;
        Exception? escaped = null;
        timer.Tick += (_, _) =>
        {
            if (!triggered)
            {
                triggered = true;
                triggering = true;
                try { trigger(); }
                catch (Exception ex) { escaped = ex; }
                finally { triggering = false; }
                if (shown || escaped is not null) context.ExitThread();
                return;
            }
            EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
            {
                var name = new StringBuilder(256);
                GetClassName(window, name, name.Capacity);
                if (name.ToString() == "#32770")
                {
                    nativeDialog = true;
                    PostMessage(window, 0x0010, nint.Zero, nint.Zero);
                }
                return true;
            }, nint.Zero);
            var report = Application.OpenForms.OfType<ErrorReporterForm>().SingleOrDefault();
            if (report is not null)
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
                if (!triggering) context.ExitThread();
            }
            if (elapsed.Elapsed > TimeSpan.FromSeconds(60))
            {
                context.ExitThread();
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
    }

    sealed class HandleFailureHost(string baseName) : ApplicationHostForm(baseName)
    {
        string? failureMessage;
        public bool FailureRaised { get; private set; }

        public void FailNextHandleCreation(string message)
        {
            DestroyHandle();
            failureMessage = message;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (failureMessage is null) return;
            string message = failureMessage;
            failureMessage = null;
            FailureRaised = true;
            throw new InvalidOperationException(message);
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
