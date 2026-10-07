using System.Globalization;
using System.Text;

namespace Launcher.Infrastructure;

/// <summary>保存先ごとに日付切り替え、保持期間、書き込み上限を管理する診断ログ。</summary>
public sealed class DiagnosticLogWriter(string? logDirectory, TimeProvider? timeProvider = null)
{
    readonly Lock writeLock = new();
    readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    DateOnly? lastWriteDate;
    DateTimeOffset? windowStart;
    int linesInWindow;
    const int PerMinuteLimit = 200;
    const int RetentionDays = 7;

    static string GetLogFileName(DateTime now) => now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log";

    static bool IsExpired(DateTime lastWriteTime, DateTime now) => lastWriteTime < now.AddDays(-RetentionDays);

    /// <summary>詳細トレース。通常運用では出力しない、原因調査用の細粒度ログ。</summary>
    public void Debug(string category, string message) => Write("DEBUG", category, message);

    /// <summary>ユーザー操作・重要な状態遷移。運用時に残す通常ログ。</summary>
    public void Info(string category, string message) => Write("INFO", category, message);

    /// <summary>想定外だが継続可能な事象。原因確認候補として残す。</summary>
    public void Warn(string category, string message) => Write("WARN", category, message);

    /// <summary>例外・失敗の記録（メッセージ版）。</summary>
    public void Error(string category, string message) => Write("ERROR", category, message);

    /// <summary>例外・失敗の記録（例外版）。型・メッセージ・スタックトレースを書き込む。</summary>
    public void Error(string category, Exception ex)
    {
        var message = $"{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
        Write("ERROR", category, message);
    }

    void Write(string level, string category, string message)
    {
        WriteLine(level, category, message);
        System.Diagnostics.Debugger.Log(0, category, message + Environment.NewLine);
    }

    void WriteLine(string level, string category, string message)
    {
        if (logDirectory is null) return;
        try
        {
            lock (writeLock)
            {
                var utcNow = clock.GetUtcNow();
                var now = TimeZoneInfo.ConvertTime(utcNow, clock.LocalTimeZone).DateTime;
                var date = DateOnly.FromDateTime(now);
                Directory.CreateDirectory(logDirectory);
                if (lastWriteDate != date)
                {
                    CleanupOldLogs(utcNow.UtcDateTime);
                    lastWriteDate = date;
                }
                if (windowStart is null || utcNow - windowStart.Value >= TimeSpan.FromMinutes(1))
                {
                    windowStart = utcNow;
                    linesInWindow = 0;
                }
                if (linesInWindow >= PerMinuteLimit) return;
                linesInWindow++;

                var line = $"{now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{category}] {message}{Environment.NewLine}";
                using var stream = new FileStream(
                    Path.Combine(logDirectory, GetLogFileName(now)), FileMode.Append, FileAccess.Write, FileShare.Read,
                    bufferSize: 4096, options: FileOptions.WriteThrough);
                var bytes = Encoding.UTF8.GetBytes(line);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
        }
#pragma warning disable CA1031 // 診断機構の失敗によってアプリを終了させない
        catch (Exception)
#pragma warning restore CA1031
        {
        }
    }

    void CleanupOldLogs(DateTime now)
    {
        try
        {
            foreach (var file in Directory.GetFiles(logDirectory!, "*.log"))
            {
                if (IsExpired(File.GetLastWriteTimeUtc(file), now))
                {
                    try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
