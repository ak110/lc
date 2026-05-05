using FluentAssertions;
using Launcher.Core;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// Commandクラスのテスト
/// </summary>
public sealed class CommandTests
{
    // --- Clone ---

    [Fact]
    public void Clone_全プロパティがコピーされる()
    {
        var original = new Command
        {
            Name = "test",
            FileName = "test.exe",
            Param = "-v",
            WorkDir = @"C:\temp",
            Show = WindowStyle.Maximized,
            Priority = ProcessPriorityLevel.High,
            RunAsAdmin = true,
        };

        var clone = original.Clone();

        clone.Name.Should().Be("test");
        clone.FileName.Should().Be("test.exe");
        clone.Param.Should().Be("-v");
        clone.WorkDir.Should().Be(@"C:\temp");
        clone.Show.Should().Be(WindowStyle.Maximized);
        clone.Priority.Should().Be(ProcessPriorityLevel.High);
        clone.RunAsAdmin.Should().BeTrue();
    }

    [Fact]
    public void Clone_変更が元に影響しない()
    {
        var original = new Command { Name = "original", FileName = "a.exe" };
        var clone = original.Clone();

        clone.Name = "modified";
        original.Name.Should().Be("original");
    }

    // --- CompareTo ---

    [Fact]
    public void CompareTo_名前順にソートされる()
    {
        var a = new Command { Name = "alpha" };
        var b = new Command { Name = "beta" };

        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(a).Should().BePositive();
    }

    [Fact]
    public void CompareTo_同名なら0()
    {
        var a = new Command { Name = "same" };
        var b = new Command { Name = "same" };

        a.CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void CompareTo_nullは後ろ()
    {
        var a = new Command { Name = "test" };

        a.CompareTo((Command)null!).Should().BePositive();
    }

    [Fact]
    public void CompareTo_両方Nameがnullなら0()
    {
        var a = new Command { Name = null! };
        var b = new Command { Name = null! };

        a.CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void CompareTo_片方Nameがnullなら前()
    {
        var withName = new Command { Name = "test" };
        var withNull = new Command { Name = null! };

        withNull.CompareTo(withName).Should().BeNegative();
        withName.CompareTo(withNull).Should().BePositive();
    }

    // --- IComparable (object) ---

    [Fact]
    public void CompareTo_objectインターフェース()
    {
        var a = new Command { Name = "alpha" };
        object b = new Command { Name = "beta" };

        a.CompareTo(b).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_Command以外のobjectはnull扱い()
    {
        var a = new Command { Name = "test" };
        a.CompareTo("not a command").Should().BePositive();
    }

    // --- LoadFrom ---

    [Fact]
    public void LoadFrom_名前指定で基本フィールドを読み込む()
    {
        var data = "test.exe\n-param\nC:\\work\n0\n3";
        var cmd = Command.LoadFrom("mycmd", data);

        cmd.Name.Should().Be("mycmd");
        cmd.FileName.Should().Be("test.exe");
        cmd.Param.Should().Be("-param");
        cmd.WorkDir.Should().Be(@"C:\work");
        cmd.Show.Should().Be(WindowStyle.Normal);
        cmd.Priority.Should().Be(ProcessPriorityLevel.Normal);
    }

    [Fact]
    public void LoadFrom_名前null時はデータの1行目が名前()
    {
        var data = "myname\ntest.exe\n\n\n0\n3";
        var cmd = Command.LoadFrom(null, data);

        cmd.Name.Should().Be("myname");
        cmd.FileName.Should().Be("test.exe");
    }

    [Fact]
    public void LoadFrom_WindowStyleの各値()
    {
        var data = "test.exe\n\n\n2\n3"; // Show=2 → Maximized
        var cmd = Command.LoadFrom("test", data);

        cmd.Show.Should().Be(WindowStyle.Maximized);
    }

    [Fact]
    public void LoadFrom_Priority上限は5()
    {
        var data = "test.exe\n\n\n0\n99"; // Priority >= 5 → 5 (Idle)
        var cmd = Command.LoadFrom("test", data);

        cmd.Priority.Should().Be(ProcessPriorityLevel.Idle);
    }

    // --- GetMatchScore / IsMatch ---

    [Fact]
    public void GetMatchScore_先頭一致でスコアが正()
    {
        var cmd = new Command { Name = "notepad" };
        var config = new Config { CommandIgnoreCase = true };

        cmd.GetMatchScore("note", config).Should().BePositive();
    }

    [Fact]
    public void IsMatch_完全一致でtrue()
    {
        var cmd = new Command { Name = "notepad" };
        var config = new Config { CommandIgnoreCase = true };

        cmd.IsMatch("notepad", config).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_不完全一致でfalse()
    {
        var cmd = new Command { Name = "notepad" };
        var config = new Config { CommandIgnoreCase = true };

        cmd.IsMatch("note", config).Should().BeFalse();
    }

    // --- デフォルト値 ---

    [Fact]
    public void デフォルト値が正しい()
    {
        var cmd = new Command();

        cmd.Name.Should().BeEmpty();
        cmd.FileName.Should().BeEmpty();
        cmd.Param.Should().BeEmpty();
        cmd.WorkDir.Should().BeNull();
        cmd.Show.Should().Be(WindowStyle.Normal);
        cmd.Priority.Should().Be(ProcessPriorityLevel.Normal);
        cmd.RunAsAdmin.Should().BeFalse();
        cmd.IconIndex.Should().Be(-1);
    }

    // --- ApplyAdminElevation ---

    [Fact]
    public void ApplyAdminElevation_RunAsはVerbをrunasに設定する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag value",
        };

        Command.ApplyAdminElevation(info, AdminElevation.RunAs, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.Verb.Should().Be("runas");
        info.FileName.Should().Be(@"C:\app.exe");
        info.Arguments.Should().Be("-flag value");
    }

    [Fact]
    public void ApplyAdminElevation_RunAsCommandはrunasコマンドを呼ぶ形式に変換する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag value",
        };

        Command.ApplyAdminElevation(info, AdminElevation.RunAsCommand, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.FileName.Should().Be("runas");
        info.Arguments.Should().Be("/u:Admin \"\\\"C:\\app.exe\\\" -flag value\"");
        info.Verb.Should().BeNull();
    }

    [Fact]
    public void ApplyAdminElevation_RunAsCommandは引数中の二重引用符をエスケープする()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-msg \"hello\"",
        };

        Command.ApplyAdminElevation(info, AdminElevation.RunAsCommand, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.FileName.Should().Be("runas");
        // 元 Arguments の " が \" にエスケープされる
        info.Arguments.Should().Be("/u:Admin \"\\\"C:\\app.exe\\\" -msg \\\"hello\\\"\"");
    }

    [Fact]
    public void ApplyAdminElevation_VistaElevatorはエレベーターを介する形式に変換する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag value",
        };

        Command.ApplyAdminElevation(info, AdminElevation.VistaElevator, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        // 区切り文字 / が引数にも FileName にも含まれないので '/' が選ばれる。
        info.FileName.Should().Be(@"C:\elev.exe");
        info.Arguments.Should().Be(@"0/C:\app.exe/-flag value//");
        info.Verb.Should().BeNull();
    }

    [Fact]
    public void ApplyAdminElevation_VistaElevatorは衝突しない区切り文字を選ぶ()
    {
        // FileName/Arguments に '/' が含まれるため、次の候補 ',' が使われる。
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app/x.exe",
            Arguments = "-url http://example",
        };

        Command.ApplyAdminElevation(info, AdminElevation.VistaElevator, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.Arguments.Should().Be(@"0,C:\app/x.exe,-url http://example,,");
    }

    [Fact]
    public void ApplyAdminElevation_VistaElevatorはvECmdPathを正規化する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag",
        };

        // PathNormalize の挙動: / を \ に置換し、末尾の \ を除去する。
        Command.ApplyAdminElevation(info, AdminElevation.VistaElevator, runAsCommandLine: "", vECmdPath: @"C:/Tools/elev.exe/");

        info.FileName.Should().Be(@"C:\Tools\elev.exe");
    }
}
