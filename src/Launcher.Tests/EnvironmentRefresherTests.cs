using FluentAssertions;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

public sealed class EnvironmentRefresherTests
{
    static Dictionary<string, string> Registry(params (string Name, string Value)[] vars) =>
        vars.ToDictionary(v => v.Name, v => v.Value, StringComparer.OrdinalIgnoreCase);

    static Dictionary<string, string?> Current(params (string Name, string? Value)[] vars) =>
        vars.ToDictionary(v => v.Name, v => v.Value, StringComparer.OrdinalIgnoreCase);

    static HashSet<string> Tracked(params string[] names) => new(names, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void 追加された変数と値が変わった変数を設定する()
    {
        var diff = EnvironmentRefresher.ComputeRefreshDiff(
            Registry(("NEW", "1"), ("CHANGED", "b")),
            Current(("NEW", null), ("CHANGED", "a")),
            Tracked("CHANGED"));

        diff.Updates.Should().BeEquivalentTo(new KeyValuePair<string, string?>[] { new("NEW", "1"), new("CHANGED", "b") });
        diff.TrackedNames.Should().BeEquivalentTo("NEW", "CHANGED");
    }

    [Fact]
    public void 値の大文字小文字の違いは変更として扱う()
    {
        var diff = EnvironmentRefresher.ComputeRefreshDiff(
            Registry(("FOO", "A")),
            Current(("FOO", "a")),
            Tracked("FOO"));

        diff.Updates.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string?>("FOO", "A"));
    }

    [Fact]
    public void 大文字小文字だけが異なる変数名は追跡済みとして扱い削除しない()
    {
        var diff = EnvironmentRefresher.ComputeRefreshDiff(
            Registry(("Path", "x")),
            Current(("Path", "x")),
            Tracked("PATH"));

        diff.Updates.Should().BeEmpty();
        diff.TrackedNames.Should().Contain("PATH");
    }

    [Fact]
    public void レジストリから消えた追跡済みの変数を削除する()
    {
        var diff = EnvironmentRefresher.ComputeRefreshDiff(
            Registry(),
            Current(("GONE", "1")),
            Tracked("GONE"));

        diff.Updates.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string?>("GONE", null));
        diff.TrackedNames.Should().BeEmpty();
    }

    [Fact]
    public void 追跡していない起動時の変数は削除しない()
    {
        var diff = EnvironmentRefresher.ComputeRefreshDiff(
            Registry(("FOO", "a")),
            Current(("FOO", "a"), ("USERNAME", "user")),
            Tracked("FOO"));

        diff.Updates.Should().BeEmpty();
    }
}
