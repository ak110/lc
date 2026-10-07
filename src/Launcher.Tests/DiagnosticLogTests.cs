using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

public sealed class DiagnosticLogTests : IDisposable
{
    readonly DirectoryInfo tempDir = Directory.CreateTempSubdirectory("launcher-diagnosticlog-tests-");
    readonly MutableTimeProvider clock = new();

    public void Dispose() => tempDir.Delete(recursive: true);

    [Fact]
    public void 書き込み_日跨ぎでファイルを切り替えて保持境界より古いログだけ削除する()
    {
        var writer = new DiagnosticLogWriter(tempDir.FullName, clock);
        writer.Info("Test", "前日");
        var expired = Path.Combine(tempDir.FullName, "expired.log");
        var boundary = Path.Combine(tempDir.FullName, "boundary.log");
        File.WriteAllText(expired, "古いログ");
        File.WriteAllText(boundary, "境界ログ");
        clock.Now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        File.SetLastWriteTimeUtc(expired, clock.Now.UtcDateTime.AddDays(-7).AddSeconds(-1));
        File.SetLastWriteTimeUtc(boundary, clock.Now.UtcDateTime.AddDays(-7));

        writer.Warn("Test", "当日");

        File.ReadAllText(Path.Combine(tempDir.FullName, "20260101.log")).Should().Contain("前日").And.NotContain("当日");
        File.ReadAllText(Path.Combine(tempDir.FullName, "20260102.log")).Should().Contain("[WARN] [Test] 当日");
        File.Exists(expired).Should().BeFalse();
        File.ReadAllText(boundary).Should().Be("境界ログ");
    }

    [Fact]
    public void 書き込み_毎分上限と次の窓の再開を独立インスタンスで管理する()
    {
        var writer = new DiagnosticLogWriter(tempDir.FullName, clock);
        for (var i = 0; i < 200; i++) writer.Info("Test", "記録");
        writer.Info("Test", "上限超過");
        var path = Path.Combine(tempDir.FullName, "20260101.log");
        File.ReadAllLines(path).Should().HaveCount(200);
        clock.Now = clock.Now.AddMinutes(1);
        writer.Info("Test", "再開");
        File.ReadAllLines(path).Should().HaveCount(201);
        File.ReadAllText(path).Should().Contain("再開").And.NotContain("上限超過");
        new DiagnosticLogWriter(tempDir.FullName, clock).Debug("Test", "独立");
        File.ReadAllText(path).Should().Contain("[DEBUG] [Test] 独立");
    }

    [Fact]
    public void ファビコン失敗_URLとメッセージを保存しない()
    {
        var writer = new DiagnosticLogWriter(tempDir.FullName, clock);
        var cache = new FaviconCache(Path.Combine(tempDir.FullName, "favicons"), writer.Warn);
        cache.Get("https://user:secret@[invalid/path?token=secret", true).Should().BeNull();
        var content = File.ReadAllText(Path.Combine(tempDir.FullName, "20260101.log"));
        content.Should().Contain("UriFormatException");
        content.Should().NotContain("https://").And.NotContain("secret").And.NotContain("invalid");
    }

    [Fact]
    public void 書き込み_利用不能な保存先で例外を漏らさない()
    {
        var blocked = Path.Combine(tempDir.FullName, "file");
        File.WriteAllText(blocked, "ファイル");
        var write = () => new DiagnosticLogWriter(blocked, clock).Error("Test", "失敗");
        write.Should().NotThrow();
        new DiagnosticLogWriter(null, clock).Info("Test", "無効");
    }

    [Fact]
    public void Error_例外型とメッセージとスタックを保存する()
    {
        var writer = new DiagnosticLogWriter(tempDir.FullName, clock);
        try
        {
            throw new InvalidOperationException("診断対象");
        }
        catch (InvalidOperationException ex)
        {
            writer.Error("Test", ex);
        }
        var content = File.ReadAllText(Path.Combine(tempDir.FullName, "20260101.log"));
        content.Should().Contain("[ERROR] [Test]").And.Contain("InvalidOperationException");
        content.Should().Contain("診断対象").And.Contain(nameof(Error_例外型とメッセージとスタックを保存する));
    }

    sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 23, 58, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
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
