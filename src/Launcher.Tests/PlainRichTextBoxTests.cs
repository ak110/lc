using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using FluentAssertions;
using Launcher.UI;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// PlainRichTextBoxのプレーンテキスト入出力を検証する。
/// </summary>
public sealed class PlainRichTextBoxTests
{
    [Theory]
    [InlineData(Keys.Control | Keys.V)]
    [InlineData(Keys.Shift | Keys.Insert)]
    public void ProcessCmdKey_貼り付けショートカットはプレーンテキストを挿入する(Keys keyData) =>
        RunInSta(() =>
        {
            int containsCount = 0;
            int getCount = 0;
            int setCount = 0;
            using var control = CreateControl(
                () =>
                {
                    containsCount++;
                    return true;
                },
                () =>
                {
                    getCount++;
                    return "plain";
                },
                _ => setCount++);
            control.Text = "before after";
            control.Select(7, 5);

            bool handled = InvokeProcessCmdKey(control, keyData);

            handled.Should().BeTrue();
            control.Text.Should().Be("before plain");
            containsCount.Should().Be(1);
            getCount.Should().Be(1);
            setCount.Should().Be(0);
        });

    [Theory]
    [InlineData(Keys.Control | Keys.C)]
    [InlineData(Keys.Control | Keys.Insert)]
    public void ProcessCmdKey_コピーショートカットは選択文字列だけを書き込む(Keys keyData) =>
        RunInSta(() =>
        {
            int containsCount = 0;
            int getCount = 0;
            int setCount = 0;
            string? copiedText = null;
            using var control = CreateControl(
                () =>
                {
                    containsCount++;
                    return true;
                },
                () =>
                {
                    getCount++;
                    return "unused";
                },
                text =>
                {
                    setCount++;
                    copiedText = text;
                });
            control.Text = "before after";
            control.Select(7, 5);

            bool handled = InvokeProcessCmdKey(control, keyData);

            handled.Should().BeTrue();
            setCount.Should().Be(1);
            copiedText.Should().Be("after");
            containsCount.Should().Be(0);
            getCount.Should().Be(0);
            control.Text.Should().Be("before after");
            control.SelectionStart.Should().Be(7);
            control.SelectionLength.Should().Be(5);
        });

    [Fact]
    public void WndProc_WM_PASTEはプレーンテキストを挿入する() =>
        RunInSta(() =>
        {
            int containsCount = 0;
            int getCount = 0;
            using var control = CreateControl(
                () =>
                {
                    containsCount++;
                    return true;
                },
                () =>
                {
                    getCount++;
                    return "plain";
                },
                _ => throw new InvalidOperationException());
            control.Text = "before after";
            control.Select(7, 5);

            InvokeWndProc(control, WM.WM_PASTE);

            control.Text.Should().Be("before plain");
            containsCount.Should().Be(1);
            getCount.Should().Be(1);
        });

    [Fact]
    public void WndProc_WM_COPYは選択文字列だけを書き込む() =>
        RunInSta(() =>
        {
            int setCount = 0;
            string? copiedText = null;
            using var control = CreateControl(
                () => throw new InvalidOperationException(),
                () => throw new InvalidOperationException(),
                text =>
                {
                    setCount++;
                    copiedText = text;
                });
            control.Text = "before after";
            control.Select(7, 5);

            InvokeWndProc(control, WM.WM_COPY);

            setCount.Should().Be(1);
            copiedText.Should().Be("after");
            control.Text.Should().Be("before after");
            control.SelectionStart.Should().Be(7);
            control.SelectionLength.Should().Be(5);
        });

    [Fact]
    public void WndProc_WM_COPYで空選択ならクリップボードを変更しない() =>
        RunInSta(() =>
        {
            int setCount = 0;
            using var control = CreateControl(
                () => throw new InvalidOperationException(),
                () => throw new InvalidOperationException(),
                _ => setCount++);
            control.Text = "before";
            control.Select(3, 0);

            InvokeWndProc(control, WM.WM_COPY);

            setCount.Should().Be(0);
            control.Text.Should().Be("before");
            control.SelectionStart.Should().Be(3);
            control.SelectionLength.Should().Be(0);
        });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void 貼り付け経路_クリップボード競合時は文書と選択範囲を変更しない(int route) =>
        RunInSta(() =>
        {
            using var control = CreateControl(
                () => true,
                () => throw new ExternalException("クリップボード競合"),
                _ => throw new InvalidOperationException());
            control.Text = "before after";
            control.Select(7, 5);

            Action act = () => InvokePasteRoute(control, route);

            act.Should().NotThrow();
            control.Text.Should().Be("before after");
            control.SelectionStart.Should().Be(7);
            control.SelectionLength.Should().Be(5);
        });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void コピー経路_クリップボード競合時は文書と選択範囲を変更しない(int route) =>
        RunInSta(() =>
        {
            using var control = CreateControl(
                () => throw new InvalidOperationException(),
                () => throw new InvalidOperationException(),
                _ => throw new ExternalException("クリップボード競合"));
            control.Text = "before after";
            control.Select(7, 5);

            Action act = () => InvokeCopyRoute(control, route);

            act.Should().NotThrow();
            control.Text.Should().Be("before after");
            control.SelectionStart.Should().Be(7);
            control.SelectionLength.Should().Be(5);
        });

    [Fact]
    public void ProcessCmdKey_通常文字キーは基底処理へ委ねる() =>
        RunInSta(() =>
        {
            int clipboardOperationCount = 0;
            using var control = CreateControl(
                () =>
                {
                    clipboardOperationCount++;
                    return true;
                },
                () =>
                {
                    clipboardOperationCount++;
                    return "unused";
                },
                _ => clipboardOperationCount++);

            bool handled = InvokeProcessCmdKey(control, Keys.A);

            handled.Should().BeFalse();
            clipboardOperationCount.Should().Be(0);
        });

    [Fact]
    public void ComputeIndentReplacement_複数行選択の全行を2スペースインデントする()
    {
        string text = "aaa\nbbb\nccc";
        // 1行目末尾から3行目途中までを選択 (改行をまたぐ)
        string replaced = InvokeComputeIndent(text, selStart: 2, selLength: 7, dedent: false,
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
        string replaced = InvokeComputeIndent(text, selStart: 0, selLength: text.Length, dedent: false,
            out _, out _, out _);
        replaced.Should().Be("  aaa\n\n  bbb");
    }

    [Fact]
    public void ComputeIndentReplacement_Dedentは行頭スペースを最大2つ除去する()
    {
        string text = "    aaa\n bbb\nccc";
        string replaced = InvokeComputeIndent(text, selStart: 0, selLength: text.Length, dedent: true,
            out _, out _, out _);
        replaced.Should().Be("  aaa\nbbb\nccc");
    }

    [Fact]
    public void ComputeIndentReplacement_選択が次行冒頭で終わる場合は次行を含めない()
    {
        string text = "aaa\nbbb\nccc";
        // "aaa\n"だけを選択 (SelectionStart=0, Length=4)
        string replaced = InvokeComputeIndent(text, selStart: 0, selLength: 4, dedent: false,
            out int regionStart, out int regionLength, out _);
        regionStart.Should().Be(0);
        regionLength.Should().Be(4);
        replaced.Should().Be("  aaa\n");
    }

    static string InvokeComputeIndent(string text, int selStart, int selLength, bool dedent,
        out int regionStart, out int regionLength, out int newSelLength)
    {
        var method = typeof(PlainRichTextBox).GetMethod(
            "ComputeIndentReplacement", BindingFlags.Static | BindingFlags.NonPublic)!;
        object?[] args = [text, selStart, selLength, dedent, 0, 0, 0];
        string result = (string)method.Invoke(null, args)!;
        regionStart = (int)args[4]!;
        regionLength = (int)args[5]!;
        newSelLength = (int)args[6]!;
        return result;
    }

    static PlainRichTextBox CreateControl(
        Func<bool> containsText,
        Func<string> getText,
        Action<string> setText)
    {
        var constructor = typeof(PlainRichTextBox).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(Func<bool>), typeof(Func<string>), typeof(Action<string>)],
            modifiers: null)!;
        return (PlainRichTextBox)constructor.Invoke([containsText, getText, setText]);
    }

    static bool InvokeProcessCmdKey(PlainRichTextBox control, Keys keyData)
    {
        var method = typeof(PlainRichTextBox).GetMethod(
            "ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] arguments = [Message.Create(IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero), keyData];
        return (bool)method.Invoke(control, arguments)!;
    }

    static void InvokeWndProc(PlainRichTextBox control, int message)
    {
        var method = typeof(PlainRichTextBox).GetMethod(
            "WndProc", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] arguments = [Message.Create(IntPtr.Zero, message, IntPtr.Zero, IntPtr.Zero)];
        method.Invoke(control, arguments);
    }

    static void InvokePasteRoute(PlainRichTextBox control, int route)
    {
        if (route == 0)
        {
            InvokeProcessCmdKey(control, Keys.Control | Keys.V).Should().BeTrue();
        }
        else if (route == 1)
        {
            InvokeProcessCmdKey(control, Keys.Shift | Keys.Insert).Should().BeTrue();
        }
        else
        {
            InvokeWndProc(control, WM.WM_PASTE);
        }
    }

    static void InvokeCopyRoute(PlainRichTextBox control, int route)
    {
        if (route == 0)
        {
            InvokeProcessCmdKey(control, Keys.Control | Keys.C).Should().BeTrue();
        }
        else if (route == 1)
        {
            InvokeProcessCmdKey(control, Keys.Control | Keys.Insert).Should().BeTrue();
        }
        else
        {
            InvokeWndProc(control, WM.WM_COPY);
        }
    }

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
