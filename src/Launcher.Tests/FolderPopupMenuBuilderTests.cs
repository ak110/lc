using System.Reflection;
using FluentAssertions;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// FolderPopupMenuBuilderのテスト。
/// Shell拡張・Cursor.Positionに依存するInvokeShellExecuteDeferred/InvokeContextMenuDeferredは対象外。
/// </summary>
public sealed class FolderPopupMenuBuilderTests
{
    /// <summary>
    /// ContextMenuStripを実際に表示せずClosedイベントを発火させる。
    /// protected ToolStripDropDown.OnClosedをリフレクション経由で呼ぶ。
    /// </summary>
    static void RaiseClosed(ContextMenuStrip menu)
    {
        var onClosed = typeof(ToolStripDropDown).GetMethod(
            "OnClosed", BindingFlags.NonPublic | BindingFlags.Instance)!;
        onClosed.Invoke(menu, new object[]
        {
            new ToolStripDropDownClosedEventArgs(ToolStripDropDownCloseReason.CloseCalled),
        });
    }

    [Fact]
    public void Build_空フォルダは空項目のみ表示する()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            using var menu = builder.Build(tempDir.FullName);

            menu.Items.Count.Should().Be(1);
            menu.Items[0].Text.Should().Be("(空)");
            menu.Items[0].Enabled.Should().BeFalse();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_50件超過フォルダは先頭50件と省略件数を表示する()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            for (int i = 0; i < 55; i++)
            {
                File.WriteAllText(Path.Combine(tempDir.FullName, $"file{i:D3}.txt"), "");
            }

            using var menu = builder.Build(tempDir.FullName);

            menu.Items.Count.Should().Be(51);
            menu.Items[50].Text.Should().Be("(以下 5 件を省略)");
            menu.Items[50].Enabled.Should().BeFalse();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_ContextMenuStripのClosedイベントでiconLoaderが破棄される()
    {
        using var form = new Form();
        _ = form.Handle;
        var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir.FullName, "a.txt"), "");

            var menu = builder.Build(tempDir.FullName);
            // Closed イベント発火前は iconLoader は未破棄
            GetIconLoaderDisposed(builder).Should().BeFalse();

            RaiseClosed(menu);
            Application.DoEvents(); // SafeBeginInvoke経由でポストされたDisposeを処理する

            // Closed 発火後は SafeBeginInvoke 経由で builder.Dispose が呼ばれ iconLoader も破棄される
            GetIconLoaderDisposed(builder).Should().BeTrue();
        }
        finally
        {
            builder.Dispose();
            tempDir.Delete(recursive: true);
        }
    }

    /// <summary>
    /// FolderPopupMenuBuilder が保持する AsyncIconLoader の IsDisposed を
    /// リフレクション経由で取得する。単体テスト検証のみで使用する。
    /// </summary>
    static bool GetIconLoaderDisposed(FolderPopupMenuBuilder builder)
    {
        var iconLoaderField = typeof(FolderPopupMenuBuilder).GetField(
            "iconLoader", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var iconLoader = (Launcher.Win32.AsyncIconLoader)iconLoaderField.GetValue(builder)!;
        return iconLoader.IsDisposed;
    }

    [Fact]
    public void Dispose_二重呼び出しでも例外を送出しない()
    {
        using var form = new Form();
        _ = form.Handle;
        var builder = new FolderPopupMenuBuilder(form.Handle, form);

        try
        {
            Action act = () =>
            {
                builder.Dispose();
                builder.Dispose();
            };

            act.Should().NotThrow();
        }
        finally
        {
            builder.Dispose();
        }
    }
}
