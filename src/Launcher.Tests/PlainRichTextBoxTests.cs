using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Launcher.Win32;
using Xunit;
using Xunit.Abstractions;

namespace Launcher.Tests;

[CollectionDefinition("Clipboard", DisableParallelization = true)]
public sealed class ClipboardTestsCollection { }

/// <summary>通常のコントロール入口と実クリップボードからプレーンテキスト入出力を検査する。</summary>
[Collection("Clipboard")]
public sealed class PlainRichTextBoxTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void 貼り付けは書式を除去して選択箇所を置換する(int route) => RunWithClipboard(() =>
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, "plain");
        data.SetData(DataFormats.Rtf, @"{\rtf1\ansi\b plain}");
        Clipboard.SetDataObject(data, true);
        using var form = new Form();
        using var control = new PlainRichTextBox { Text = "before after" };
        form.Controls.Add(control);
        form.Show();
        control.Select(7, 5);
        Paste(control, route);
        control.Text.Should().Be("before plain");
        control.Select(7, 5);
        using var font = control.SelectionFont;
        font.Should().NotBeNull();
        font!.Bold.Should().BeFalse();
    });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void コピーは選択文字列だけをプレーンテキストで格納する(int route) => RunWithClipboard(() =>
    {
        using var form = new Form();
        using var control = new PlainRichTextBox { Text = "before after" };
        form.Controls.Add(control);
        form.Show();
        control.Select(7, 5);
        Copy(control, route);
        Clipboard.GetText().Should().Be("after");
        Clipboard.ContainsData(DataFormats.Rtf).Should().BeFalse();
        control.Text.Should().Be("before after");
        control.SelectionStart.Should().Be(7);
        control.SelectionLength.Should().Be(5);
    });

    [Fact]
    public void 空選択のコピーはクリップボードを変更しない() => RunWithClipboard(() =>
    {
        Clipboard.SetText("before");
        using var control = new PlainRichTextBox { Text = "document" };
        control.Select(3, 0);
        new WindowHelper(control.Handle).SendMessage(WM.WM_COPY, IntPtr.Zero, IntPtr.Zero);
        Clipboard.GetText().Should().Be("before");
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void クリップボード競合時は文書と選択範囲を変更しない(bool copy) => RunWithClipboard(() =>
    {
        Clipboard.SetText("plain");
        using var control = new PlainRichTextBox { Text = "before after" };
        control.Select(7, 5);
        _ = control.Handle;
        WithLockedClipboard(() =>
        {
            Action act = () => new WindowHelper(control.Handle).SendMessage(
                copy ? WM.WM_COPY : WM.WM_PASTE, IntPtr.Zero, IntPtr.Zero);
            act.Should().NotThrow();
            control.Text.Should().Be("before after");
            control.SelectionStart.Should().Be(7);
            control.SelectionLength.Should().Be(5);
        });
    });

    static void Paste(PlainRichTextBox control, int route)
    {
        control.Focus();
        switch (route)
        {
            case 0: PreProcessShortcut(control, Keys.Control | Keys.V); break;
            case 1: PreProcessShortcut(control, Keys.Shift | Keys.Insert); break;
            case 2: control.Paste(); break;
            default: new WindowHelper(control.Handle).SendMessage(WM.WM_PASTE, IntPtr.Zero, IntPtr.Zero); break;
        }
    }

    static void Copy(PlainRichTextBox control, int route)
    {
        control.Focus();
        switch (route)
        {
            case 0: PreProcessShortcut(control, Keys.Control | Keys.C); break;
            case 1: PreProcessShortcut(control, Keys.Control | Keys.Insert); break;
            case 2: control.Copy(); break;
            default: new WindowHelper(control.Handle).SendMessage(WM.WM_COPY, IntPtr.Zero, IntPtr.Zero); break;
        }
    }

    static void PreProcessShortcut(Control control, Keys keys)
    {
        // 通常の入力前処理へ送り、キー状態はこのSTAだけで変更して元へ戻す。
        // SendKeysは前景の別アプリへ届きうるため、受入対象のコントロールへ限定する。
        var original = new byte[256];
        GetKeyboardState(original).Should().BeTrue();
        var state = new byte[256];
        if ((keys & Keys.Control) != 0) state[(int)Keys.ControlKey] = 0x80;
        if ((keys & Keys.Shift) != 0) state[(int)Keys.ShiftKey] = 0x80;
        try
        {
            SetKeyboardState(state).Should().BeTrue();
            var message = Message.Create(control.Handle, 0x0100, (nint)(keys & Keys.KeyCode), nint.Zero);
            control.PreProcessMessage(ref message).Should().BeTrue();
        }
        finally
        {
            SetKeyboardState(original).Should().BeTrue();
        }
    }

    static void WithLockedClipboard(Action action)
    {
        using var ready = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        bool locked = false;
        int lockError = 0;
        var thread = new Thread(() =>
        {
            using var owner = new Form();
            for (int attempt = 0; attempt < 100 && !locked; attempt++)
            {
                locked = OpenClipboard(owner.Handle);
                if (!locked)
                {
                    lockError = Marshal.GetLastPInvokeError();
                    Thread.Sleep(10);
                }
            }
            ready.Set();
            try { release.Wait(TimeSpan.FromSeconds(10)); }
            finally { if (locked) CloseClipboard(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        try
        {
            ready.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            locked.Should().BeTrue("競合の受入試験にはclipboardの取得が必要。Win32 error={0}", lockError);
            action();
        }
        finally
        {
            release.Set();
            thread.Join();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool OpenClipboard(IntPtr owner);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    static extern IntPtr GetOpenClipboardWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetKeyboardState([Out] byte[] state);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetKeyboardState(byte[] state);

    void RunWithClipboard(Action action) => RunInSta(() =>
    {
        var original = Clipboard.GetDataObject();
        ExceptionDispatchInfo? actionFailure = null;
        ExceptionDispatchInfo? restoreFailure = null;
        try
        {
            action();
            output.WriteLine("Clipboard.Action completed=true");
        }
        catch (Exception ex) when ((actionFailure = ExceptionDispatchInfo.Capture(ex)) is not null)
        {
            output.WriteLine($"Clipboard.Action completed=false exception={ex.GetType().Name}");
        }
        finally
        {
            long started = Stopwatch.GetTimestamp();
            try
            {
                if (original is not null) Clipboard.SetDataObject(original, true);
                else Clipboard.Clear();
                output.WriteLine($"Clipboard.Restore completed=true elapsedMs={Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0}");
            }
            catch (ExternalException ex) when ((restoreFailure = ExceptionDispatchInfo.Capture(ex)) is not null)
            {
                var window = GetOpenClipboardWindow();
                uint processId = 0;
                uint threadId = window == IntPtr.Zero ? 0 : GetWindowThreadProcessId(window, out processId);
                output.WriteLine($"Clipboard.Restore completed=false hresult=0x{ex.HResult:X8} elapsedMs={Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0} openWindow={window} process={processId} thread={threadId}");
            }
        }
        if (actionFailure is not null && restoreFailure is not null)
        {
            throw new AggregateException(actionFailure.SourceException, restoreFailure.SourceException);
        }
        actionFailure?.Throw();
        restoreFailure?.Throw();
    });

    [Fact]
    public void ComputeIndentReplacement_複数行選択の全行を2スペースインデントする()
    {
        string text = "aaa\nbbb\nccc";
        // 1行目末尾から3行目途中までを選択 (改行をまたぐ)
        string replaced = TextIndentation.ComputeIndentReplacement(text, selStart: 2, selLength: 7, dedent: false,
            out int regionStart, out int regionLength, out int newSelLength);

        regionStart.Should().Be(0);
        regionLength.Should().Be(text.Length);
        replaced.Should().Be("  aaa\n  bbb\n  ccc");
        newSelLength.Should().Be(replaced.Length);
    }

    [Fact]
    public void ComputeIndentReplacement_空行にはインデントを追加しない()
    {
        string text = "aaa\n\nbbb";
        string replaced = TextIndentation.ComputeIndentReplacement(text, selStart: 0, selLength: text.Length, dedent: false,
            out _, out _, out _);
        replaced.Should().Be("  aaa\n\n  bbb");
    }

    [Fact]
    public void ComputeIndentReplacement_Dedentは行頭スペースを最大2つ除去する()
    {
        string text = "    aaa\n bbb\nccc";
        string replaced = TextIndentation.ComputeIndentReplacement(text, selStart: 0, selLength: text.Length, dedent: true,
            out _, out _, out _);
        replaced.Should().Be("  aaa\nbbb\nccc");
    }

    [Fact]
    public void ComputeIndentReplacement_選択が次行冒頭で終わる場合は次行を含めない()
    {
        string text = "aaa\nbbb\nccc";
        // "aaa\n"だけを選択 (SelectionStart=0, Length=4)
        string replaced = TextIndentation.ComputeIndentReplacement(text, selStart: 0, selLength: 4, dedent: false,
            out int regionStart, out int regionLength, out _);
        regionStart.Should().Be(0);
        regionLength.Should().Be(4);
        replaced.Should().Be("  aaa\n");
    }

    [Fact]
    public void OnHandleCreated_ASCIIと日本語を同じフォントで表示する() =>
        RunInSta(() =>
        {
            using var font = new Font("Consolas", 11f);
            using var form = new Form();
            using var control = new PlainRichTextBox();
            form.Controls.Add(control);
            control.ApplyFont(font);
            form.Show();
            try
            {
                control.Text = "abc あいう def";

                for (int index = 0; index < control.TextLength; index++)
                {
                    control.Select(index, 1);
                    using var selectionFont = control.SelectionFont;
                    selectionFont.Should().NotBeNull();
                    selectionFont!.Name.Should().Be(font.Name, "位置{0}の文字書式", index);
                }
            }
            finally
            {
                form.Close();
            }
        });

    static void RunInSta(Action action)
    {
        ExceptionDispatchInfo? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex) when ((exception = ExceptionDispatchInfo.Capture(ex)) is not null)
            {
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        exception?.Throw();
    }
}
