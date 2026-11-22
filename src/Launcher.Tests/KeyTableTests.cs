using FluentAssertions;
using Launcher.Win32;
using Xunit;
using SystemKeys = System.Windows.Forms.Keys;

namespace Launcher.Tests;

public sealed class KeyTableTests
{
    [Theory]
    [InlineData(KeyTable.Keys.A, SystemKeys.A)]
    [InlineData(KeyTable.Keys.Enter, SystemKeys.Enter)]
    [InlineData(KeyTable.Keys.F1, SystemKeys.F1)]
    [InlineData(KeyTable.Keys.Num0, SystemKeys.NumPad0)]
    public void KeysToVKey_独自キーコードから仮想キーコードに変換できる(KeyTable.Keys key, SystemKeys expected)
    {
        KeyTable.KeysToVKey(key).Should().Be(expected);
    }

    [Theory]
    [InlineData(KeyTable.Keys.A, "A")]
    [InlineData(KeyTable.Keys.D1, "1")]
    [InlineData(KeyTable.Keys.F1, "F1")]
    [InlineData(KeyTable.Keys.Escape, "Esc")]
    [InlineData(KeyTable.Keys.Enter, "Enter")]
    [InlineData(KeyTable.Keys.Space, "Space")]
    [InlineData(KeyTable.Keys.Back, "BackSpace")]
    [InlineData(KeyTable.Keys.Minus, "-")]
    [InlineData(KeyTable.Keys.Plus, "+")]
    [InlineData(KeyTable.Keys.Up, "↑")]
    [InlineData(KeyTable.Keys.Left, "←")]
    [InlineData(KeyTable.Keys.Down, "↓")]
    [InlineData(KeyTable.Keys.Right, "→")]
    [InlineData(KeyTable.Keys.LClick, "左クリック")]
    [InlineData(KeyTable.Keys.RLClick, "右→左クリック")]
    public void GetKeyName_キーの表示名を正しく返す(KeyTable.Keys key, string expected)
    {
        KeyTable.GetKeyName(key).Should().Be(expected);
    }

    [Fact]
    public void GetKeyName_修飾キー付きの名前を返す()
    {
        KeyTable.GetKeyName(KeyTable.Keys.A, KeyTable.Modifiers.Ctrl).Should().Be("Ctrl+A");
    }

    [Fact]
    public void GetKeyName_複数修飾キーを連結する()
    {
        var mods = KeyTable.Modifiers.Ctrl | KeyTable.Modifiers.Alt | KeyTable.Modifiers.Shift;
        KeyTable.GetKeyName(KeyTable.Keys.Delete, mods).Should().Be("Ctrl+Alt+Shift+Delete");
    }

    [Fact]
    public void GetKeyName_Win修飾キーを含む()
    {
        var mods = KeyTable.Modifiers.Win;
        KeyTable.GetKeyName(KeyTable.Keys.E, mods).Should().Be("Win+E");
    }

    [Theory]
    [InlineData("A", KeyTable.Keys.A)]
    [InlineData("Enter", KeyTable.Keys.Enter)]
    [InlineData("Esc", KeyTable.Keys.Escape)]
    [InlineData("F12", KeyTable.Keys.F12)]
    [InlineData("左クリック", KeyTable.Keys.LClick)]
    public void GetKey_キー名から独自キーコードに変換できる(string name, KeyTable.Keys expected)
    {
        KeyTable.GetKey(name).Should().Be(expected);
    }

    [Fact]
    public void GetKeyWithModifiers_修飾キーなしのキー名をパースできる()
    {
        var (key, mods) = KeyTable.GetKeyWithModifiers("A");

        key.Should().Be(KeyTable.Keys.A);
        mods.Should().Be((KeyTable.Modifiers)0);
    }

    [Fact]
    public void GetKeyWithModifiers_Ctrl修飾キー付きをパースできる()
    {
        var (key, mods) = KeyTable.GetKeyWithModifiers("Ctrl+A");

        key.Should().Be(KeyTable.Keys.A);
        mods.Should().Be(KeyTable.Modifiers.Ctrl);
    }

    [Fact]
    public void GetKeyWithModifiers_複数修飾キー付きをパースできる()
    {
        var (key, mods) = KeyTable.GetKeyWithModifiers("Ctrl+Alt+Shift+Delete");

        key.Should().Be(KeyTable.Keys.Delete);
        mods.Should().Be(KeyTable.Modifiers.Ctrl | KeyTable.Modifiers.Alt | KeyTable.Modifiers.Shift);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Alt+")]
    [InlineData("Win+")]
    public void GetKeyWithModifiers_末尾キーが空なら未割り当てを返す(string hotKey)
    {
        var (key, mods) = KeyTable.GetKeyWithModifiers(hotKey);

        key.Should().BeNull();
        mods.Should().Be((KeyTable.Modifiers)0);
    }

    [Fact]
    public void GetKeyNames_マウスなしでキーボードキーのみ取得()
    {
        string[] names = KeyTable.GetKeyNames(false);

        names.Length.Should().Be((int)KeyTable.Keys.LClick);
        names.Should().Contain("A");
        names.Should().Contain("Enter");
        names.Should().NotContain("左クリック");
    }

    [Fact]
    public void GetKeyNames_マウスありで全キー取得()
    {
        string[] names = KeyTable.GetKeyNames(true);

        names.Length.Should().Be((int)KeyTable.Keys.KeyCount);
        names.Should().Contain("A");
        names.Should().Contain("左クリック");
        names.Should().Contain("左→右クリック");
    }

    [Fact]
    public void 全キーコードにキー名がマッピングされている()
    {
        for (int i = 0; i < (int)KeyTable.Keys.KeyCount; i++)
        {
            var key = (KeyTable.Keys)i;
            string? name = KeyTable.GetKeyName(key);
            name.Should().NotBeNullOrEmpty($"キー {key} にキー名が登録されていない");
        }
    }

    [Fact]
    public void キー名からキーコードへの逆変換が正しい()
    {
        string[] allNames = KeyTable.GetKeyNames(true);
        for (int i = 0; i < allNames.Length; i++)
        {
            var key = KeyTable.GetKey(allNames[i]);
            key.Should().Be((KeyTable.Keys)i, $"キー名 '{allNames[i]}' の逆変換が一致しない");
        }
    }

}
