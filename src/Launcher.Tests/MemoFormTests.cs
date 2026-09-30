using System.Reflection;
using System.Runtime.ExceptionServices;
using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

public sealed class MemoFormTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 保存失敗_入力と未保存状態を保持して再保存できる(bool readOnly) => RunInSta(() =>
    {
        string root = Path.Combine(Path.GetTempPath(), "lc_memo_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "memo.cfg");
        try
        {
            var data = new MemoData { Tabs = [new MemoTab { Name = "メモ", Text = "保存前" }] };
            data.SerializeToFile(path);
            using var form = CreateForm(data, () => data.SerializeToFile(path));
            form.Show();
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
                form.FlushPendingSave().Should().BeFalse();
                status.Text.Should().Contain("未保存");
                retry.Enabled.Should().BeTrue();
                editor.Text.Should().Be("保存できなかった日本語の本文");
                ConfigStore.DeserializeFromFile<MemoData>(path).Tabs[0].Text.Should().Be("保存前");
                Capture(form, readOnly ? "memo-readonly" : "memo-locked");
                form.HideIfVisible();
                editor.Text.Should().Be("保存できなかった日本語の本文");
            }
            File.SetAttributes(path, FileAttributes.Normal);
            form.Show();
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
            Directory.Delete(root, true);
        }
    });

    [Fact]
    public void 読込失敗で保存停止中_再保存しても原本を上書きせず本文を保持する() => RunInSta(() =>
    {
        string root = Path.Combine(Path.GetTempPath(), "lc_memo_blocked_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string baseName = Path.Combine(root, "memo");
        string path = baseName + ".memo.cfg";
        try
        {
            File.WriteAllText(path, "<broken>");
            var result = new ConfigFile<MemoData>(".memo.cfg").Load(baseName);
            result.Status.Should().Be(ConfigLoadStatus.Failed);
            var data = result.Value;
            using var form = CreateForm(data, () => data.SerializeToFile(path));
            form.Show();
            var editor = form.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<PlainRichTextBox>().Single();
            editor.Text = "退避する本文";
            form.FlushPendingSave().Should().BeFalse();
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
            Directory.Delete(root, true);
        }
    });

    static MemoForm CreateForm(MemoData data, Action save)
    {
        // ファイル保存先だけを差し替え、フォームの編集・保存・再保存は実処理を通す。
        var constructor = typeof(MemoForm).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null, [typeof(MemoData), typeof(Action)], modifiers: null)!;
        return (MemoForm)constructor.Invoke([data, save]);
    }

    static void Capture(Form form, string name)
    {
        if (Environment.GetEnvironmentVariable("LC_MEMO_SCREENSHOT_DIR") is not { Length: > 0 } outputDir) return;
        Directory.CreateDirectory(outputDir);
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(outputDir, name + ".png"));
    }

    static void RunInSta(Action action)
    {
        ExceptionDispatchInfo? exception = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) when ((exception = ExceptionDispatchInfo.Capture(ex)) is not null) { }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        exception?.Throw();
    }
}
