using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// DiagnosticLogの主要挙動テスト。
/// ResetForTestingで一時ディレクトリへ出力先を差し替えて実際の書き込みを検証する。
/// </summary>
public sealed class DiagnosticLogTests : IDisposable
{
    readonly DirectoryInfo tempDir;

    public DiagnosticLogTests()
    {
        tempDir = Directory.CreateTempSubdirectory("launcher-diagnosticlog-tests-");
        DiagnosticLog.ResetForTesting(tempDir.FullName);
    }

    public void Dispose()
    {
        DiagnosticLog.ResetForTesting(null);
        tempDir.Delete(recursive: true);
    }

    [Fact]
    public void CurrentLogPath_一時ディレクトリ配下のcrashログを指す()
    {
        var path = DiagnosticLog.CurrentLogPath;

        path.Should().NotBeNull();
        Path.GetDirectoryName(path).Should().Be(tempDir.FullName);
        Path.GetFileName(path).Should().StartWith("crash-");
        Path.GetFileName(path).Should().EndWith(".log");
    }

    [Fact]
    public void Trace_ファイルへ書き込まれる()
    {
        var marker = Guid.NewGuid().ToString("N");
        DiagnosticLog.Trace("Test", marker);

        var content = File.ReadAllText(DiagnosticLog.CurrentLogPath!);
        content.Should().Contain(marker);
        content.Should().Contain("[Test]");
    }

    [Fact]
    public void TraceException_例外情報がログに含まれる()
    {
        var marker = Guid.NewGuid().ToString("N");
        var ex = new InvalidOperationException($"marker-{marker}");
        DiagnosticLog.TraceException("Test", ex);

        var content = File.ReadAllText(DiagnosticLog.CurrentLogPath!);
        content.Should().Contain($"marker-{marker}");
        content.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public void ResetForTesting_nullを渡すと利用不能状態になる()
    {
        DiagnosticLog.ResetForTesting(null);

        DiagnosticLog.CurrentLogPath.Should().BeNull();
        // 利用不能状態でもTrace呼び出し自体は例外を送出しない(no-opフォールバック)
        DiagnosticLog.Trace("Test", "unreachable");
    }
}
