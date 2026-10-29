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
                XmlTestSerializer.DeserializeFromFile<MemoData>(path).Tabs[0].Text.Should().Be("保存前");
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
            XmlTestSerializer.DeserializeFromFile<MemoData>(path).Tabs[0].Text.Should().Be("保存できなかった日本語の本文");
            Capture(form, readOnly ? "memo-readonly-restored" : "memo-locked-restored");

            editor.Text = "再編集した本文";
            form.FlushPendingSave().Should().BeTrue();
            XmlTestSerializer.DeserializeFromFile<MemoData>(path).Tabs[0].Text.Should().Be("再編集した本文");
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

    [Fact]
    public void 編集操作_ショートカットで行のソートと大文字小文字変換を行い1回の取り消しで戻る() => UiAcceptance.Run(() =>
    {
        using var data = CreateData();
        data.Write(".memo.cfg", new MemoData { Tabs = [new MemoTab { Name = "メモ", Text = "head\nc\nB\na\ntail" }] });
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            host.ShowHideMemo();
            var form = host.OwnedForms.OfType<MemoForm>().Single();
            var editor = form.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<PlainRichTextBox>().Single();
            var editMenu = form.MainMenuStrip!.Items.OfType<ToolStripMenuItem>().Single(item => item.Name == "editMenu");
            editMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Name == "sortLinesMenuItem")
                .ShortcutKeys.Should().Be(Keys.F9);

            // 選択の無いF9は何もしない。
            editor.Select(0, 0);
            UiAcceptance.PreProcessShortcut(editor, Keys.F9);
            editor.Text.Should().Be("head\nc\nB\na\ntail");

            // 「c」の途中から「a」の途中までを選択し、行単位で並べ替える。
            editor.Select(6, 4);
            UiAcceptance.PreProcessShortcut(editor, Keys.F9);
            editor.Text.Should().Be("head\nB\na\nc\ntail");
            (editor.SelectionStart, editor.SelectionLength).Should().Be((5, 5));
            editor.Undo();
            editor.Text.Should().Be("head\nc\nB\na\ntail");

            editor.Select(0, 2);
            UiAcceptance.PreProcessShortcut(editor, Keys.Control | Keys.U);
            editor.Text.Should().Be("HEad\nc\nB\na\ntail");
            (editor.SelectionStart, editor.SelectionLength).Should().Be((0, 2));

            editor.Select(0, 4);
            UiAcceptance.PreProcessShortcut(editor, Keys.Control | Keys.L);
            editor.Text.Should().Be("head\nc\nB\na\ntail");
            (editor.SelectionStart, editor.SelectionLength).Should().Be((0, 4));
            editor.Undo();
            editor.Text.Should().Be("HEad\nc\nB\na\ntail");
        }
        finally
        {
            foreach (var form in host.OwnedForms.OfType<MemoForm>()) form.Dispose();
            host.PrepareShutdown();
            host.Close();
        }
    });

    [Fact]
    public void 表示メニュー_拡大縮小とCtrl0で現在タブの倍率を変更し既定に戻す() => UiAcceptance.Run(() =>
    {
        using var data = CreateData();
        data.Write(".memo.cfg", new MemoData { Tabs = [new MemoTab { Name = "メモ", Text = "本文" }] });
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            host.ShowHideMemo();
            var form = host.OwnedForms.OfType<MemoForm>().Single();
            var editor = form.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<PlainRichTextBox>().Single();
            var viewMenu = form.MainMenuStrip!.Items.OfType<ToolStripMenuItem>().Single(item => item.Name == "viewMenu");
            var items = viewMenu.DropDownItems.OfType<ToolStripMenuItem>().ToDictionary(item => item.Name!);
            (items["zoomInMenuItem"].ShortcutKeyDisplayString, items["zoomOutMenuItem"].ShortcutKeyDisplayString,
                items["zoomResetMenuItem"].ShortcutKeyDisplayString).Should().Be(("Ctrl++", "Ctrl+-", "Ctrl+0"));

            items["zoomInMenuItem"].PerformClick();
            editor.ZoomFactor.Should().BeApproximately(1.1f, 0.001f);
            items["zoomOutMenuItem"].PerformClick();
            items["zoomOutMenuItem"].PerformClick();
            editor.ZoomFactor.Should().BeApproximately(0.9f, 0.001f);
            items["zoomResetMenuItem"].PerformClick();
            editor.ZoomFactor.Should().BeApproximately(1f, 0.001f);

            // 各配列とテンキーのキーで拡大・縮小する。
            foreach (var keys in new[] { Keys.Control | Keys.Oemplus, Keys.Control | Keys.Shift | Keys.Oemplus, Keys.Control | Keys.Add })
            {
                UiAcceptance.PreProcessShortcut(editor, keys);
            }
            editor.ZoomFactor.Should().BeApproximately(1.3f, 0.001f);
            UiAcceptance.PreProcessShortcut(editor, Keys.Control | Keys.OemMinus);
            UiAcceptance.PreProcessShortcut(editor, Keys.Control | Keys.Subtract);
            editor.ZoomFactor.Should().BeApproximately(1.1f, 0.001f);

            // WinFormsのキャッシュ値を既定にしてから、Ctrl+ホイールと同じくRichEditへ直接倍率を設定する。
            // キャッシュ値と同じ値の設定を無視する経路でも、既定へ戻ることを確かめる。
            items["zoomResetMenuItem"].PerformClick();
            foreach (var keys in new[] { Keys.Control | Keys.D0, Keys.Control | Keys.NumPad0 })
            {
                SendMessage(editor.Handle, EM_SETZOOM, 3, 2);
                UiAcceptance.PreProcessShortcut(editor, keys);
                SendMessage(editor.Handle, EM_GETZOOM, out int numerator, out int denominator);
                (numerator == denominator || numerator == 0).Should().BeTrue($"{keys}で既定の倍率へ戻る（{numerator}/{denominator}）");
            }
            editor.Text.Should().Be("本文");
        }
        finally
        {
            foreach (var form in host.OwnedForms.OfType<MemoForm>()) form.Dispose();
            host.PrepareShutdown();
            host.Close();
        }
    });

    [Theory]
    [InlineData("置換(&R)", "x\ty\n1\\z\nx2z")]
    [InlineData("すべて置換(&A)", "x\ty\n1\\z\nx\ty\n2\\z")]
    public void 置換_正規表現の置換後文字列のエスケープと後方参照を本文へ反映する(string button, string expected) => UiAcceptance.Run(() =>
    {
        using var data = CreateData();
        data.Write(".memo.cfg", new MemoData { Tabs = [new MemoTab { Name = "メモ", Text = "x1z\nx2z" }] });
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            host.ShowHideMemo();
            var form = host.OwnedForms.OfType<MemoForm>().Single();
            var editor = form.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<PlainRichTextBox>().Single();
            var editMenu = form.MainMenuStrip!.Items.OfType<ToolStripMenuItem>().Single(item => item.Name == "editMenu");
            editMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Name == "replaceMenuItem").PerformClick();
            var dialog = form.OwnedForms.OfType<FindReplaceDialog>().Single();
            var boxes = dialog.Controls.OfType<TextBox>().OrderBy(box => box.TabIndex).ToList();
            boxes[0].Text = @"x(\d)z";
            boxes[1].Text = @"x\ty\n$1\\z";
            dialog.Controls.OfType<CheckBox>().Single(box => box.Text == "正規表現(&E)").Checked = true;
            editor.Select(0, 3);

            dialog.Controls.OfType<Button>().Single(b => b.Text == button).PerformClick();

            editor.Text.Should().Be(expected);
        }
        finally
        {
            foreach (var form in host.OwnedForms.OfType<MemoForm>()) form.Dispose();
            host.PrepareShutdown();
            host.Close();
        }
    });

    [Fact]
    public void 余白_上下はタブページの余白で設けスクロールする書式矩形は左右だけを縮める() => UiAcceptance.Run(() =>
    {
        // RichEditはスクロール時に書式矩形の外を再描画しないため、
        // 上下の余白を書式矩形の外に置くとスクロール前の文字の画素が余白に残る。
        using var data = CreateData();
        data.Write(".memo.cfg", new MemoData { Tabs = [new MemoTab { Name = "メモ", Text = string.Join("\n", Enumerable.Range(1, 200)) }] });
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            host.ShowHideMemo();
            var form = host.OwnedForms.OfType<MemoForm>().Single();
            var page = form.Controls.OfType<TabControl>().Single().TabPages[0];
            var editor = page.Controls.OfType<PlainRichTextBox>().Single();

            AssertPadding(page, editor);
            using var font = new Font(editor.Font.FontFamily, editor.Font.Size * 2);
            editor.ApplyFont(font);
            AssertPadding(page, editor);
        }
        finally
        {
            foreach (var form in host.OwnedForms.OfType<MemoForm>()) form.Dispose();
            host.PrepareShutdown();
            host.Close();
        }

        static void AssertPadding(TabPage page, PlainRichTextBox editor)
        {
            int pad = Math.Max(1, editor.Font.Height / 2);
            page.Padding.Should().Be(new Padding(0, pad, 0, pad));
            page.BackColor.Should().Be(editor.BackColor);
            SendMessage(editor.Handle, 0x00B2, nint.Zero, out var rect); // EM_GETRECT
            (rect.Left, rect.Top, rect.Right, rect.Bottom).Should().Be(
                (pad, 0, editor.ClientSize.Width - pad, editor.ClientSize.Height));
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

    [StructLayout(LayoutKind.Sequential)]
    struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    static extern nint SendMessage(nint window, uint message, nint wParam, out Rect lParam);

    // RichEditの倍率メッセージ (Windows SDK 10.0.19041.0 の richedit.h)
    const uint EM_GETZOOM = 0x04E0;
    const uint EM_SETZOOM = 0x04E1;

    [DllImport("user32.dll")]
    static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    static extern nint SendMessage(nint window, uint message, out int wParam, out int lParam);

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
