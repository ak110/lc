using FluentAssertions;
using Launcher.Core;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// 管理者権限昇格時の起動情報加工 (<see cref="AdminElevationApplier"/>) のテスト。
/// </summary>
public sealed class AdminElevationApplierTests
{
    [Fact]
    public void Apply_RunAsはVerbをrunasに設定する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag value",
        };

        AdminElevationApplier.Apply(info, AdminElevation.RunAs, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.Verb.Should().Be("runas");
        info.FileName.Should().Be(@"C:\app.exe");
        info.Arguments.Should().Be("-flag value");
    }

    [Fact]
    public void Apply_RunAsCommandはrunasコマンドを呼ぶ形式に変換する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag value",
        };

        AdminElevationApplier.Apply(info, AdminElevation.RunAsCommand, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.FileName.Should().Be("runas");
        info.Arguments.Should().Be("/u:Admin \"\\\"C:\\app.exe\\\" -flag value\"");
        info.Verb.Should().BeNull();
    }

    [Fact]
    public void Apply_RunAsCommandは引数中の二重引用符をエスケープする()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-msg \"hello\"",
        };

        AdminElevationApplier.Apply(info, AdminElevation.RunAsCommand, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.FileName.Should().Be("runas");
        // 元 Arguments の " が \" にエスケープされる
        info.Arguments.Should().Be("/u:Admin \"\\\"C:\\app.exe\\\" -msg \\\"hello\\\"\"");
    }

    [Fact]
    public void Apply_VistaElevatorはエレベーターを介する形式に変換する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag value",
        };

        AdminElevationApplier.Apply(info, AdminElevation.VistaElevator, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        // 区切り文字 / が引数にも FileName にも含まれないので '/' が選ばれる。
        info.FileName.Should().Be(@"C:\elev.exe");
        info.Arguments.Should().Be(@"0/C:\app.exe/-flag value//");
        info.Verb.Should().BeNull();
    }

    [Fact]
    public void Apply_VistaElevatorは衝突しない区切り文字を選ぶ()
    {
        // FileName/Arguments に '/' が含まれるため、次の候補 ',' が使われる。
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app/x.exe",
            Arguments = "-url http://example",
        };

        AdminElevationApplier.Apply(info, AdminElevation.VistaElevator, runAsCommandLine: "/u:Admin", vECmdPath: @"C:\elev.exe");

        info.Arguments.Should().Be(@"0,C:\app/x.exe,-url http://example,,");
    }

    [Fact]
    public void Apply_VistaElevatorはvECmdPathを正規化する()
    {
        var info = new ShellProcessStartInfo
        {
            FileName = @"C:\app.exe",
            Arguments = "-flag",
        };

        // PathNormalize の挙動: / を \ に置換し、末尾の \ を除去する。
        AdminElevationApplier.Apply(info, AdminElevation.VistaElevator, runAsCommandLine: "", vECmdPath: @"C:/Tools/elev.exe/");

        info.FileName.Should().Be(@"C:\Tools\elev.exe");
    }
}
