using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;
using Xunit.Abstractions;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class MemoFormTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 保存失敗_入力と未保存状態を保持して再保存できる(bool readOnly) => UiAcceptance.Run(() =>
    {
        using var data = CreateData();
        string path = data.BaseName + ".memo.cfg";
        data.Write(".memo.cfg", new MemoData { Tabs = [new MemoTab { Name = "メモ", Text = "保存前" }] });
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            host.ShowHideMemo();
            var form = host.OwnedForms.OfType<MemoForm>().Single();
            var editor = form.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<PlainRichTextBox>().Single();
            var statusStrip = form.Controls.OfType<StatusStrip>().Single();
            form.Controls.OfType<TabControl>().Single().Bottom.Should().BeLessThanOrEqualTo(statusStrip.Top);
            var status = statusStrip.Items.OfType<ToolStripStatusLabel>().Single();
            var retry = statusStrip.Items.OfType<ToolStripButton>().Single();

            using (var fileLock = readOnly ? null : new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (readOnly) File.SetAttributes(path, FileAttributes.ReadOnly);
                editor.Text = "保存できなかった日本語の本文";
                status.Text.Should().Contain("未保存");
                ObserveNotice(() => form.FlushPendingSave().Should().BeFalse(), readOnly ? "読み取り専用" : "共有違反", data.BaseName, form);
                status.Text.Should().Contain("未保存");
                retry.Enabled.Should().BeTrue();
                editor.Text.Should().Be("保存できなかった日本語の本文");
                ConfigStore.DeserializeFromFile<MemoData>(path).Tabs[0].Text.Should().Be("保存前");
                Capture(form, readOnly ? "memo-readonly" : "memo-locked");
                form.HideIfVisible();
                editor.Text.Should().Be("保存できなかった日本語の本文");
            }
            File.SetAttributes(path, FileAttributes.Normal);
            host.ShowHideMemo();
            if (readOnly)
                retry.PerformClick();
            else
                form.FlushPendingSave().Should().BeTrue("タイマー停止後も未保存の内容を最終保存する");
            status.Text.Should().Be("保存済み");
            retry.Enabled.Should().BeFalse();
            ConfigStore.DeserializeFromFile<MemoData>(path).Tabs[0].Text.Should().Be("保存できなかった日本語の本文");
            Capture(form, readOnly ? "memo-readonly-restored" : "memo-locked-restored");

            editor.Text = "再編集した本文";
            form.FlushPendingSave().Should().BeTrue();
            ConfigStore.DeserializeFromFile<MemoData>(path).Tabs[0].Text.Should().Be("再編集した本文");
        }
        finally
        {
            if (File.Exists(path)) File.SetAttributes(path, FileAttributes.Normal);
            // アサーション失敗時にも所有する画面とタイマーを先に回収する。
            foreach (var form in host.OwnedForms.OfType<MemoForm>()) form.Dispose();
            host.PrepareShutdown();
            host.Close();
        }
    });

    [Fact]
    public void 読込失敗で保存停止中_再保存しても原本を上書きせず本文を保持する() => UiAcceptance.Run(() =>
    {
        using var data = CreateData();
        string path = data.BaseName + ".memo.cfg";
        File.WriteAllText(path, "<broken>");
        ApplicationHostForm? host = null;
        try
        {
            // 起動時の読込通知も通常のホスト構築から発生させる。
            ObserveNotice(() => host = new ApplicationHostForm(data.BaseName), "破損XMLの起動通知", data.BaseName);
            host.Should().NotBeNull();
            host!.ShowHideMemo();
            var form = host.OwnedForms.OfType<MemoForm>().Single();
            var editor = form.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<PlainRichTextBox>().Single();
            editor.Text = "退避する本文";
            ObserveNotice(() => form.FlushPendingSave().Should().BeFalse(), "破損XMLの保存停止通知", data.BaseName, form);
            var strip = form.Controls.OfType<StatusStrip>().Single();
            strip.Items.OfType<ToolStripStatusLabel>().Single().Text.Should().Contain("コピー");
            strip.Items.OfType<ToolStripButton>().Single().Enabled.Should().BeFalse();
            var menu = form.MainMenuStrip!.Items.OfType<ToolStripMenuItem>().First();
            menu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Name == "saveMenuItem").Enabled.Should().BeFalse();
            form.FlushPendingSave().Should().BeFalse();
            File.ReadAllText(path).Should().Be("<broken>");
            editor.Text.Should().Be("退避する本文");
            Capture(form, "memo-blocked");
        }
        finally
        {
            if (host is not null)
            {
                foreach (var form in host.OwnedForms.OfType<MemoForm>()) form.Dispose();
                host.PrepareShutdown();
                host.Close();
                host.Dispose();
            }
        }
    });

    static UiHostData CreateData() => new(new Config
    {
        HideFirst = true,
        TrayIcon = false,
        HotKey = "",
        MemoHotKey = "",
        ReplaceEnv = [],
        WindowHideNoActive = false,
        ButtonLauncherActivation = ButtonLauncherActivation.Disabled,
    });

    void ObserveNotice(Action trigger, string scenario, string baseName, Form? expectedOwner = null)
    {
        // 通知を作成したSTAで観測・アクティブ化し、実際のOKボタンをクリックする。
        uint threadId = GetCurrentThreadId();
        uint processId = (uint)Environment.ProcessId;
        nint ownerHandle = expectedOwner?.Handle ?? nint.Zero;
        string expectedTitle = AppVersion.Title;
        string? logPath = null;
        if (Environment.GetEnvironmentVariable("LC_MEMO_SCREENSHOT_DIR") is { Length: > 0 } outputDir)
        {
            Directory.CreateDirectory(outputDir);
            logPath = Path.Combine(outputDir, $"memo-notice-{processId}-{threadId}.log");
        }
        void Log(string message)
        {
            string line = $"{DateTimeOffset.UtcNow:O} [{scenario}] {message}";
            // 中止されたケースの出力も残すため、各行を閉じてからxUnitへ渡す。
            if (logPath is not null) File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
            output.WriteLine(line);
        }
        Log($"メモ通知観測開始: process={processId} thread={threadId} owner={ownerHandle} baseName=[{baseName}]");
        using var timer = new System.Windows.Forms.Timer { Interval = 10 };
        int timerTicks = 0;
        var acknowledged = new HashSet<nint>();
        bool classReadFailed = false;
        bool sendFailed = false;
        bool timedOut = false;
        bool triggerReturned = false;
        var existing = new HashSet<nint>();
        EnumThreadWindows(threadId, (window, _) => { existing.Add(window); return true; }, nint.Zero);
        var observations = new Dictionary<nint, string>();
        var elapsed = Stopwatch.StartNew();
        timer.Tick += (_, _) =>
        {
            timerTicks++;
            if (elapsed.Elapsed >= TimeSpan.FromSeconds(30))
            {
                timedOut = true;
                timer.Stop();
                Log($"メモ通知観測期限: acknowledged={acknowledged.Count} timerTicks={timerTicks}");
                return;
            }
            EnumThreadWindows(threadId, (window, _) =>
            {
                if (existing.Contains(window) || acknowledged.Contains(window)
                    || GetWindowThreadProcessId(window, out uint windowProcess) != threadId
                    || windowProcess != processId) return true;
                var name = new StringBuilder(256);
                if (GetClassName(window, name, name.Capacity) == 0)
                {
                    classReadFailed = true;
                    return true;
                }
                if (name.ToString() != "#32770" || !IsWindowVisible(window)) return true;
                var title = new StringBuilder(256);
                bool titleMatch = GetWindowText(window, title, title.Capacity) > 0 && title.ToString() == expectedTitle;
                bool ownerMatch = ownerHandle == nint.Zero || GetWindow(window, 4) == ownerHandle; // GW_OWNER
                nint button = FindNoticeButton(window, threadId, processId, out string buttons);
                string observation = $"titleMatch={titleMatch} ownerMatch={ownerMatch} buttons=[{buttons}]";
                if (observations.GetValueOrDefault(window) != observation)
                {
                    observations[window] = observation;
                    Log($"メモ通知: hwnd={window} {observation}");
                }
                if (!titleMatch || !ownerMatch || button == nint.Zero) return true;
                SetActiveWindow(window);
                if (GetActiveWindow() != window || GetParent(button) != window
                    || GetWindowThreadProcessId(window, out windowProcess) != threadId || windowProcess != processId
                    || GetWindowThreadProcessId(button, out uint buttonProcess) != threadId || buttonProcess != processId
                    || !IsWindowVisible(button) || !IsWindowEnabled(button)) return true;
                // MessageBoxの戻り値IDOKと実コントロールIDは一致しない場合がある。
                // IDを送らず、確認した唯一のOKボタン自身に通常クリックを届ける。
                bool sent = PostMessage(button, 0x00F5, nint.Zero, nint.Zero); // BM_CLICK
                sendFailed |= !sent;
                if (sent) acknowledged.Add(window);
                Log($"メモ通知OK: hwnd={window} button={button} sent={sent}");
                return true;
            }, nint.Zero);
        };
        timer.Start();
        try
        {
            trigger();
            triggerReturned = true;
        }
        finally
        {
            timer.Stop();
            Log($"メモ通知観測終了: acknowledged={acknowledged.Count} timerTicks={timerTicks} timedOut={timedOut} triggerReturned={triggerReturned}");
        }
        acknowledged.Should().NotBeEmpty("通常の通知が表示される");
        foreach (nint window in acknowledged)
            IsWindow(window).Should().BeFalse("OK操作後に通知が閉じ、通常の保存操作へ戻る");
        classReadFailed.Should().BeFalse();
        sendFailed.Should().BeFalse();
        timedOut.Should().BeFalse("通知の観測とOK処理が完了する");
    }

    static nint FindNoticeButton(nint dialog, uint threadId, uint processId, out string description)
    {
        // 親0での全window列挙を避け、所有通知の子孫だけを読み取る。
        description = "";
        if (dialog == nint.Zero) return nint.Zero;
        var children = new List<string>();
        int buttonCount = 0;
        nint button = nint.Zero;
        EnumChildWindows(dialog, (child, _) =>
        {
            if (GetWindowThreadProcessId(child, out uint childProcess) != threadId
                || childProcess != processId || GetParent(child) != dialog
                || !IsWindowVisible(child) || !IsWindowEnabled(child)) return true;
            var name = new StringBuilder(256);
            if (GetClassName(child, name, name.Capacity) == 0
                || !name.ToString().Equals("Button", StringComparison.OrdinalIgnoreCase)) return true;
            buttonCount++;
            var caption = new StringBuilder(256);
            if (GetWindowText(child, caption, caption.Capacity) > 0 && caption.ToString() == "OK") button = child;
            children.Add($"hwnd={child} id={GetDlgCtrlID(child)} caption=[{caption}]");
            return true;
        }, nint.Zero);
        children.Sort(StringComparer.Ordinal);
        description = string.Join("; ", children);
        return buttonCount == 1 ? button : nint.Zero;
    }

    static void Capture(Form form, string name)
    {
        if (Environment.GetEnvironmentVariable("LC_MEMO_SCREENSHOT_DIR") is not { Length: > 0 } outputDir) return;
        Directory.CreateDirectory(outputDir);
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(outputDir, name + ".png"));
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
    static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(nint window, StringBuilder text, int count);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumChildWindows(nint parent, EnumWindowCallback callback, nint parameter);

    [DllImport("user32.dll")]
    static extern nint GetParent(nint window);

    [DllImport("user32.dll")]
    static extern int GetDlgCtrlID(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowEnabled(nint window);

    [DllImport("user32.dll")]
    static extern nint GetWindow(nint window, uint command);

    [DllImport("user32.dll")]
    static extern nint SetActiveWindow(nint window);

    [DllImport("user32.dll")]
    static extern nint GetActiveWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
