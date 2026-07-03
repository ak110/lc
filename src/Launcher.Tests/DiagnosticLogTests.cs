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
    public void CurrentLogPath_一時ディレクトリ配下の日付ログを指す()
    {
        var path = DiagnosticLog.CurrentLogPath;

        path.Should().NotBeNull();
        Path.GetDirectoryName(path).Should().Be(tempDir.FullName);
        Path.GetFileName(path).Should().MatchRegex(@"^\d{8}\.log$");
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

    [Fact]
    public void MigrateLegacyDirectory_旧crashlogが存在しない場合はno_opとなる()
    {
        using var exeDir = new TempDirectory("launcher-migrate-tests-");

        DiagnosticLog.MigrateLegacyDirectory(exeDir.Path);

        Directory.Exists(Path.Combine(exeDir.Path, "logs")).Should().BeFalse();
        Directory.Exists(Path.Combine(exeDir.Path, "crash-log")).Should().BeFalse();
    }

    [Fact]
    public void MigrateLegacyDirectory_logs未作成なら丸ごとlogsへ改名する()
    {
        using var exeDir = new TempDirectory("launcher-migrate-tests-");
        var legacy = Directory.CreateDirectory(Path.Combine(exeDir.Path, "crash-log"));
        File.WriteAllText(Path.Combine(legacy.FullName, "20260101.log"), "old");

        DiagnosticLog.MigrateLegacyDirectory(exeDir.Path);

        var target = Path.Combine(exeDir.Path, "logs");
        Directory.Exists(target).Should().BeTrue();
        Directory.Exists(legacy.FullName).Should().BeFalse();
        File.ReadAllText(Path.Combine(target, "20260101.log")).Should().Be("old");
    }

    [Fact]
    public void MigrateLegacyDirectory_logs既存時はlog個別移動と空crashlog削除で完了する()
    {
        using var exeDir = new TempDirectory("launcher-migrate-tests-");
        var legacy = Directory.CreateDirectory(Path.Combine(exeDir.Path, "crash-log"));
        var target = Directory.CreateDirectory(Path.Combine(exeDir.Path, "logs"));
        File.WriteAllText(Path.Combine(legacy.FullName, "20260101.log"), "old");
        File.WriteAllText(Path.Combine(target.FullName, "20260202.log"), "new");

        DiagnosticLog.MigrateLegacyDirectory(exeDir.Path);

        Directory.Exists(legacy.FullName).Should().BeFalse();
        File.ReadAllText(Path.Combine(target.FullName, "20260101.log")).Should().Be("old");
        File.ReadAllText(Path.Combine(target.FullName, "20260202.log")).Should().Be("new");
    }

    [Fact]
    public void MigrateLegacyDirectory_移動先で同名ファイルがある場合は元ファイルを残す()
    {
        using var exeDir = new TempDirectory("launcher-migrate-tests-");
        var legacy = Directory.CreateDirectory(Path.Combine(exeDir.Path, "crash-log"));
        var target = Directory.CreateDirectory(Path.Combine(exeDir.Path, "logs"));
        File.WriteAllText(Path.Combine(legacy.FullName, "20260101.log"), "old");
        File.WriteAllText(Path.Combine(target.FullName, "20260101.log"), "new");

        DiagnosticLog.MigrateLegacyDirectory(exeDir.Path);

        File.ReadAllText(Path.Combine(target.FullName, "20260101.log")).Should().Be("new");
        Directory.Exists(legacy.FullName).Should().BeTrue();
        File.ReadAllText(Path.Combine(legacy.FullName, "20260101.log")).Should().Be("old");
    }

    /// <summary>
    /// テスト用の一時ディレクトリを`using`パターンで扱うラッパー。
    /// try/finallyでの削除処理を各テストから排除する目的で用意する。
    /// </summary>
    sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory(string prefix)
        {
            Path = Directory.CreateTempSubdirectory(prefix).FullName;
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
