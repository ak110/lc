using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class MemoFormTests
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
                ObserveNotice(() => form.FlushPendingSave().Should().BeFalse(), form);
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
            // 失敗アサーションの後にも終了確認を出さず、所有する画面とタイマーを回収する。
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
            ObserveNotice(() => host = new ApplicationHostForm(data.BaseName));
            host.Should().NotBeNull();
            host!.ShowHideMemo();
            var form = host.OwnedForms.OfType<MemoForm>().Single();
            var editor = form.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<PlainRichTextBox>().Single();
            editor.Text = "退避する本文";
            ObserveNotice(() => form.FlushPendingSave().Should().BeFalse(), form);
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

    static void ObserveNotice(Action trigger, Form? expectedOwner = null)
    {
        // MessageBoxのネストしたループで動く。同じ専用STAのアプリ通知だけにOKを送る。
        uint threadId = GetCurrentThreadId();
        using var timer = new System.Windows.Forms.Timer { Interval = 10 };
        var acknowledged = new HashSet<nint>();
        bool ownerMatches = true;
        bool classReadFailed = false;
        bool postFailed = false;
        timer.Tick += (_, _) =>
        {
            EnumThreadWindows(threadId, (window, _) =>
            {
                var name = new StringBuilder(256);
                if (GetClassName(window, name, name.Capacity) == 0)
                {
                    classReadFailed = true;
                    return true;
                }
                if (name.ToString() != "#32770" || !IsWindowVisible(window)) return true;
                var title = new StringBuilder(256);
                if (GetWindowText(window, title, title.Capacity) == 0
                    || title.ToString() != AppVersion.Title || GetDlgItem(window, 1) == nint.Zero)
                    return true;
                if (!acknowledged.Add(window)) return true;
                if (expectedOwner is not null)
                    ownerMatches &= GetWindow(window, 4) == expectedOwner.Handle; // GW_OWNER
                postFailed |= !PostMessage(window, 0x0111, (nint)1, nint.Zero); // WM_COMMAND / IDOK
                return true;
            }, nint.Zero);
        };
        timer.Start();
        try { trigger(); }
        finally { timer.Stop(); }
        acknowledged.Should().NotBeEmpty("通常の通知が表示される");
        ownerMatches.Should().BeTrue("保存失敗通知のownerはメモ画面である");
        classReadFailed.Should().BeFalse();
        postFailed.Should().BeFalse();
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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(nint window, StringBuilder text, int count);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    static extern nint GetDlgItem(nint dialog, int id);

    [DllImport("user32.dll")]
    static extern nint GetWindow(nint window, uint command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
