using FluentAssertions;
using Launcher.Win32;
using Xunit;
using SystemKeys = System.Windows.Forms.Keys;

namespace Launcher.Tests;

/// <summary>
/// KeyTableのテスト
/// </summary>
public sealed class KeyTableTests
{
    // --- VKeyToKeys / KeysToVKey 往復変換 ---

    [Theory]
    [InlineData(SystemKeys.A, KeyTable.Keys.A)]
    [InlineData(SystemKeys.Z, KeyTable.Keys.Z)]
    [InlineData(SystemKeys.D0, KeyTable.Keys.D0)]
    [InlineData(SystemKeys.D9, KeyTable.Keys.D9)]
    [InlineData(SystemKeys.F1, KeyTable.Keys.F1)]
    [InlineData(SystemKeys.F12, KeyTable.Keys.F12)]
    [InlineData(SystemKeys.Escape, KeyTable.Keys.Escape)]
    [InlineData(SystemKeys.Enter, KeyTable.Keys.Enter)]
    [InlineData(SystemKeys.Space, KeyTable.Keys.Space)]
    [InlineData(SystemKeys.NumPad0, KeyTable.Keys.Num0)]
    [InlineData(SystemKeys.Add, KeyTable.Keys.NumAdd)]
    [InlineData(SystemKeys.Subtract, KeyTable.Keys.NumSub)]
    [InlineData(SystemKeys.Multiply, KeyTable.Keys.NumMul)]
    [InlineData(SystemKeys.Divide, KeyTable.Keys.NumDiv)]
    [InlineData(SystemKeys.Left, KeyTable.Keys.Left)]
    [InlineData(SystemKeys.Right, KeyTable.Keys.Right)]
    [InlineData(SystemKeys.Up, KeyTable.Keys.Up)]
    [InlineData(SystemKeys.Down, KeyTable.Keys.Down)]
    public void VKeyToKeys_仮想キーコードから独自キーコードに変換できる(SystemKeys vkey, KeyTable.Keys expected)
    {
        KeyTable.VKeyToKeys(vkey).Should().Be(expected);
    }

    [Theory]
    [InlineData(KeyTable.Keys.A, SystemKeys.A)]
    [InlineData(KeyTable.Keys.Enter, SystemKeys.Enter)]
    [InlineData(KeyTable.Keys.F1, SystemKeys.F1)]
    [InlineData(KeyTable.Keys.Num0, SystemKeys.NumPad0)]
    public void KeysToVKey_独自キーコードから仮想キーコードに変換できる(KeyTable.Keys key, SystemKeys expected)
    {
        KeyTable.KeysToVKey(key).Should().Be(expected);
    }

    [Fact]
    public void VKeyToKeys_未登録キーはnullを返す()
    {
        KeyTable.VKeyToKeys(SystemKeys.LWin).Should().BeNull();
    }

    [Fact]
    public void KeysToVKey_nullはNoneを返す()
    {
        KeyTable.KeysToVKey((KeyTable.Keys?)null).Should().Be(SystemKeys.None);
    }

    // --- GetKeyName ---

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

    // --- GetKeyName (修飾キー付き) ---

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

    // --- GetKey ---

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

    // GetKey_未定義名のテストは省略（Debug.Failがテストホストで例外になるため）

    // --- GetKeyWithModifiers ---

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

    // GetKeyWithModifiers_不正なキー名のテストは省略（Debug.Failがテストホストで例外になるため）

    // --- GetVKey ---

    [Fact]
    public void GetVKey_キー名から仮想キーコードと修飾キーを返す()
    {
        var (vkey, mods) = KeyTable.GetVKey("Ctrl+A");

        vkey.Should().Be(SystemKeys.A);
        mods.Should().Be(KeyTable.Modifiers.Ctrl);
    }

    // --- GetKeyNames ---

    [Fact]
    public void GetKeyNames_マウスなしでキーボードキーのみ取得()
    {
        string[] names = KeyTable.GetKeyNames(false);

        // LClickの手前までの個数
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

    // --- 全キーにキー名が登録されているか ---

    [Fact]
    public void 全キーコードにキー名がマッピングされている()
    {
        for (int i = 0; i < (int)KeyTable.Keys.KeyCount; i++)
        {
            var key = (KeyTable.Keys)i;
            string? name = KeyTable.GetKeyName(key);
            name.Should().NotBeNullOrEmpty($"キー {key} にキー名が登録されていません");
        }
    }

    // --- 全キー名からキーコードへの逆変換が一致するか ---

    [Fact]
    public void キー名からキーコードへの逆変換が正しい()
    {
        string[] allNames = KeyTable.GetKeyNames(true);
        for (int i = 0; i < allNames.Length; i++)
        {
            var key = KeyTable.GetKey(allNames[i]);
            key.Should().Be((KeyTable.Keys)i, $"キー名 '{allNames[i]}' の逆変換が一致しません");
        }
    }

    // --- キーボードキーの仮想キーコード往復変換 ---

    [Fact]
    public void キーボードキーの仮想キーコード往復変換が正しい()
    {
        // マウスキーを除くキーボードキーのみ
        for (int i = 0; i < (int)KeyTable.Keys.LClick; i++)
        {
            var key = (KeyTable.Keys)i;
            var vkey = KeyTable.KeysToVKey(key);
            vkey.Should().NotBe(SystemKeys.None, $"キー {key} の仮想キーコード変換が失敗");

            var roundTrip = KeyTable.VKeyToKeys(vkey);
            roundTrip.Should().Be(key, $"キー {key} の往復変換が一致しません");
        }
    }
}
