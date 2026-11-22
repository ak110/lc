using System.Globalization;
using System.Text;

namespace Launcher.Infrastructure;

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

    public void Debug(string category, string message) => Write("DEBUG", category, message);

    public void Info(string category, string message) => Write("INFO", category, message);

    public void Warn(string category, string message, string nextAction) =>
        Write("WARN", category, WithNextAction(message, nextAction));

    public void Error(string category, string message, string nextAction) =>
        Write("ERROR", category, WithNextAction(message, nextAction));

    public void Error(string category, Exception ex, string nextAction)
    {
        var message = $"{WithNextAction($"{ex.GetType().FullName}: {ex.Message}", nextAction)}{Environment.NewLine}{ex.StackTrace}";
        Write("ERROR", category, message);
    }

    static string WithNextAction(string message, string nextAction) => $"{message} 次の操作: {nextAction}";

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
#pragma warning disable CA1031
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
