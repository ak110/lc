using FluentAssertions;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// EnvironmentRefresherのテスト。
/// Refresh()自体はレジストリと現プロセス環境を触るためテスト対象外。
/// Explorer互換のマージ規則を実装するBuildExpectedEnv (純粋関数) のみ検証する。
/// </summary>
public sealed class EnvironmentRefresherTests
{
    static KeyValuePair<string, string> Kv(string k, string v) => new(k, v);

    [Fact]
    public void ユーザー変数がシステム変数を上書きする()
    {
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [Kv("FOO", "a")],
            [Kv("FOO", "b")]);

        result["FOO"].Should().Be("b");
    }

    [Fact]
    public void PATH連結_システム先頭ユーザー末尾()
    {
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [Kv("Path", @"C:\sys")],
            [Kv("Path", @"C:\user")]);

        result["Path"].Should().Be(@"C:\sys;C:\user");
    }

    [Fact]
    public void PATH連結_システム末尾セミコロンを吸収()
    {
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [Kv("Path", @"C:\sys;")],
            [Kv("Path", @"C:\user")]);

        result["Path"].Should().Be(@"C:\sys;C:\user");
    }

    [Fact]
    public void PATH_ユーザーのみの場合はそのまま()
    {
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [],
            [Kv("Path", @"C:\user")]);

        result["Path"].Should().Be(@"C:\user");
    }

    [Fact]
    public void PATH_システムのみの場合はそのまま()
    {
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [Kv("Path", @"C:\sys")],
            []);

        result["Path"].Should().Be(@"C:\sys");
    }

    [Fact]
    public void PATHEXT連結()
    {
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [Kv("PATHEXT", ".COM;.EXE")],
            [Kv("PATHEXT", ".PY")]);

        result["PATHEXT"].Should().Be(".COM;.EXE;.PY");
    }

    [Fact]
    public void PATH連結は大文字小文字を無視する()
    {
        // システム側は "path"、ユーザー側は "PATH" という想定
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [Kv("path", @"C:\sys")],
            [Kv("PATH", @"C:\user")]);

        // 結合が成立する
        result["Path"].Should().Be(@"C:\sys;C:\user");
    }

    [Fact]
    public void REG_EXPAND_SZの埋め込み変数が展開される()
    {
        // %SystemRoot% は通常 C:\Windows を指す
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [],
            [Kv("MY_TEST_VAR", @"%SystemRoot%\foo")]);

        result["MY_TEST_VAR"].Should().Be(systemRoot + @"\foo");
    }

    [Fact]
    public void 非PATH変数はユーザー値で上書きされる()
    {
        var result = EnvironmentRefresher.BuildExpectedEnv(
            [Kv("JAVA_HOME", @"C:\java-system")],
            [Kv("JAVA_HOME", @"C:\java-user")]);

        result["JAVA_HOME"].Should().Be(@"C:\java-user");
    }
}
