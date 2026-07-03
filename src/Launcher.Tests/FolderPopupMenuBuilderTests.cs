using System.ComponentModel;
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
        onClosed.Invoke(menu, [
            new ToolStripDropDownClosedEventArgs(ToolStripDropDownCloseReason.CloseCalled),
        ]);
    }

    /// <summary>
    /// ToolStripMenuItemを実際に表示せずDropDownOpeningイベントを発火させる。
    /// .NET 10のToolStripDropDownItemではDropDownOpeningの発火は
    /// OnDropDownShow(EventArgs)が担う（内部の`s_dropDownOpeningEvent`キー経由）。
    /// protectedメソッドをリフレクション経由で呼ぶ。
    /// </summary>
    static void RaiseDropDownOpening(ToolStripMenuItem item)
    {
        var method = typeof(ToolStripDropDownItem).GetMethod(
            "OnDropDownShow", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(item, [EventArgs.Empty]);
    }

    /// <summary>
    /// ContextMenuStripを実際に表示せずKeyDownイベントを発火させる。
    /// protected Control.OnKeyDownをリフレクション経由で呼ぶ。
    /// </summary>
    static void RaiseKeyDown(ContextMenuStrip menu, KeyEventArgs e)
    {
        var method = typeof(Control).GetMethod(
            "OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(menu, [e]);
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

    /// <summary>
    /// ToolStripMenuItemにMouseUpハンドラが登録されているかを判定する。
    /// `ToolStripItem`の静的キーには`MouseUp`と`MouseDown`が並存するため、
    /// 名前に`Down`を含むフィールドは除外して`MouseUp`のみを対象とする。
    /// </summary>
    static bool HasMouseUpHandler(ToolStripMenuItem item) =>
        HasEventHandler(typeof(ToolStripItem), "MouseUp", "Down", item);

    /// <summary>
    /// Control（および派生ContextMenuStrip・ToolStripDropDown）のKeyDownイベント登録有無を判定する。
    /// `PreviewKeyDown`用のキーも同時に存在するため、名前に`Preview`を含むフィールドは除外する。
    /// </summary>
    static bool HasKeyDownHandler(Component target) =>
        HasEventHandler(typeof(Control), "KeyDown", "Preview", target);

    /// <summary>
    /// 静的イベントキー（`s_xxxEvent`等）を名前で走査して取得し、
    /// `Component.Events`（`EventHandlerList`）経由でハンドラ登録有無を判定する。
    /// .NET 10 WinFormsのバージョン間の命名差異に耐えるため、大文字小文字を無視した部分一致で走査する。
    /// </summary>
    /// <param name="declaringType">静的イベントキーが宣言される型（例: `ToolStripItem`・`Control`）</param>
    /// <param name="keyName">キー名に含まれるべき部分文字列（例: `MouseUp`・`KeyDown`）</param>
    /// <param name="excludeName">キー名から除外する部分文字列（例: `Preview`）。不要ならnull</param>
    /// <param name="target">ハンドラ登録先のインスタンス</param>
    static bool HasEventHandler(Type declaringType, string keyName, string? excludeName, Component target)
    {
        var eventKeyField = declaringType
            .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(f => f.FieldType == typeof(object)
                && f.Name.Contains(keyName, StringComparison.OrdinalIgnoreCase)
                && (excludeName is null
                    || !f.Name.Contains(excludeName, StringComparison.OrdinalIgnoreCase)));
        var eventKey = eventKeyField.GetValue(null)!;
        var eventsProperty = typeof(Component).GetProperty(
            "Events", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var events = (EventHandlerList)eventsProperty.GetValue(target)!;
        return events[eventKey] is not null;
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

    [Fact]
    public void Build_サブフォルダ展開時に_このフォルダを開く_項目は生成されない()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var subDir = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub"));
            File.WriteAllText(Path.Combine(subDir.FullName, "a.txt"), "");

            using var menu = builder.Build(tempDir.FullName);
            var subItem = (ToolStripMenuItem)menu.Items[0];
            RaiseDropDownOpening(subItem);

            subItem.DropDownItems.Cast<ToolStripItem>()
                .Select(i => i.Text)
                .Should().NotContain("(このフォルダを開く)");
            subItem.DropDownItems.Cast<ToolStripItem>()
                .Select(i => i.Text)
                .Should().Contain("a.txt");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_ファイル項目とサブフォルダ項目にMouseUpハンドラが登録される()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub"));
            File.WriteAllText(Path.Combine(tempDir.FullName, "a.txt"), "");

            using var menu = builder.Build(tempDir.FullName);

            foreach (ToolStripItem item in menu.Items)
            {
                if (item is not ToolStripMenuItem menuItem) continue;
                HasMouseUpHandler(menuItem).Should().BeTrue(
                    $"項目 {item.Text} にMouseUpハンドラが必要");
            }
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_50件ちょうどのフォルダは省略項目を表示しない()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            for (int i = 0; i < 50; i++)
            {
                File.WriteAllText(Path.Combine(tempDir.FullName, $"file{i:D3}.txt"), "");
            }

            using var menu = builder.Build(tempDir.FullName);

            menu.Items.Count.Should().Be(50);
            menu.Items.Cast<ToolStripItem>().Select(i => i.Text)
                .Should().NotContain(s => s.StartsWith("(以下"));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_DropDownOpening二回発火でも項目は再展開されない()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var subDir = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub"));
            File.WriteAllText(Path.Combine(subDir.FullName, "a.txt"), "");

            using var menu = builder.Build(tempDir.FullName);
            var subItem = (ToolStripMenuItem)menu.Items[0];
            RaiseDropDownOpening(subItem);
            int countAfterFirst = subItem.DropDownItems.Count;

            RaiseDropDownOpening(subItem);
            subItem.DropDownItems.Count.Should().Be(countAfterFirst);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_トップメニューにKeyDownハンドラが登録される()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir.FullName, "a.txt"), "");

            using var menu = builder.Build(tempDir.FullName);

            HasKeyDownHandler(menu).Should().BeTrue();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Build_サブメニュー展開時にサブメニューへKeyDownハンドラが登録される()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var subDir = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "sub"));
            File.WriteAllText(Path.Combine(subDir.FullName, "a.txt"), "");

            using var menu = builder.Build(tempDir.FullName);
            var subItem = (ToolStripMenuItem)menu.Items[0];

            HasKeyDownHandler(subItem.DropDown).Should().BeFalse(
                "サブメニュー展開前はKeyDownハンドラ未登録である");

            RaiseDropDownOpening(subItem);

            HasKeyDownHandler(subItem.DropDown).Should().BeTrue(
                "サブメニュー展開後はサブメニュー内EnterのためKeyDownハンドラが登録される");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void KeyDown_Enter以外のキーは早期returnで無反応となる()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir.FullName, "a.txt"), "");
            using var menu = builder.Build(tempDir.FullName);

            var e = new KeyEventArgs(Keys.A);
            RaiseKeyDown(menu, e);

            e.Handled.Should().BeFalse();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void KeyDown_選択項目なしのEnter押下では処理せず無反応となる()
    {
        using var form = new Form();
        _ = form.Handle;
        using var builder = new FolderPopupMenuBuilder(form.Handle, form);
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir.FullName, "a.txt"), "");
            using var menu = builder.Build(tempDir.FullName);

            var e = new KeyEventArgs(Keys.Return);
            RaiseKeyDown(menu, e);

            // 選択項目が無い状態でのEnter押下はハンドラ内foreachを最後まで抜けるため
            // e.Handledは既定値のまま
            e.Handled.Should().BeFalse();
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
