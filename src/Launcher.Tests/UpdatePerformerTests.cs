using FluentAssertions;
using Launcher.Updater;
using Xunit;

namespace Launcher.Tests;

public class UpdatePerformerTests
{
    [Fact]
    public void GenerateBatchScript_親プロセス待機コードが含まれる()
    {
        var files = new List<string> { "test.exe", "test.dll" };
        var result = UpdatePerformer.GenerateBatchScript(
            1234, @"C:\app", @"C:\temp\update", @"C:\app\test.exe", files);

        result.Should().Contain("tasklist /FI \"PID eq 1234\"");
        result.Should().Contain("find \"1234\"");
        result.Should().Contain("goto WAIT_DONE");
    }

    [Fact]
    public void GenerateBatchScript_ZIP内ファイルのみリネーム対象()
    {
        var files = new List<string> { "app.exe", "lib.dll" };
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", files);

        // ZIP内のファイルのみが.oldリネーム対象
        result.Should().Contain(@"rename ""C:\app\app.exe"" ""app.exe.old""");
        result.Should().Contain(@"rename ""C:\app\lib.dll"" ""lib.dll.old""");
        // ユーザーデータ(.cfgなど)はリネーム対象に含まれない
        result.Should().NotContain(".cfg");
    }

    [Fact]
    public void GenerateBatchScript_xcopyでファイルコピー()
    {
        var files = new List<string> { "app.exe" };
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", files);

        result.Should().Contain(@"xcopy /E /Y /I ""C:\temp\update\*"" ""C:\app\""");
    }

    [Fact]
    public void GenerateBatchScript_アプリ起動コマンドが含まれる()
    {
        var files = new List<string> { "app.exe" };
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", files);

        result.Should().Contain(@"start """" ""C:\app\app.exe""");
    }

    [Fact]
    public void GenerateBatchScript_oldファイル削除コマンドが含まれる()
    {
        var files = new List<string> { "app.exe", "sub\\lib.dll" };
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", files);

        result.Should().Contain(@"del /F /Q ""C:\app\app.exe.old""");
        result.Should().Contain(@"del /F /Q ""C:\app\sub\lib.dll.old""");
    }

    [Fact]
    public void GenerateBatchScript_一時ディレクトリ削除が含まれる()
    {
        var files = new List<string> { "app.exe" };
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", files);

        result.Should().Contain(@"rd /S /Q ""C:\temp\update""");
        result.Should().Contain(@"del /F /Q ""C:\temp\update.zip""");
    }

    [Fact]
    public void GenerateBatchScript_echoオフとexit()
    {
        var files = new List<string> { "app.exe" };
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", files);

        result.Should().StartWith("@echo off");
        result.Should().Contain("exit");
    }
}
