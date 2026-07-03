using System.Text;
using System.Windows.Forms;

namespace Launcher.Infrastructure;

/// <summary>
/// 永続診断ログ。AV等CLR corrupted-state exceptionでもプロセス終了前にログを残す用途。
/// 詳細は.claude/rules/win32-interop.md「AccessViolationクラッシュの診断」節を参照。
/// </summary>
public static class DiagnosticLog
{
    static readonly object writeLock = new();
    static string? logDirectory;
    static string? currentLogPath;
    static long linesInLastMinute;
    static long linesLastMinuteStartTicks;

    const long PerMinuteWindowTicks = 60 * TimeSpan.TicksPerSecond;
    const int PerMinuteLimit = 200;
    const int RetentionDays = 7;

    static DiagnosticLog()
    {
        var exeDir = Path.GetDirectoryName(Application.ExecutablePath);
        InitializeLogDirectory(exeDir is null ? null : Path.Combine(exeDir, "crash-log"));
    }

    static void InitializeLogDirectory(string? logDir)
    {
        try
        {
            if (logDir is null) return;
            logDirectory = logDir;
            Directory.CreateDirectory(logDirectory);
            currentLogPath = Path.Combine(logDirectory, $"crash-{DateTime.Now:yyyyMMdd}.log");
            CleanupOldLogs();
        }
#pragma warning disable CA1031 // 初期化失敗時はno-opへフォールバック
        catch (Exception)
#pragma warning restore CA1031
        {
            logDirectory = null;
            currentLogPath = null;
        }
    }

    /// <summary>
    /// ログ出力先を指定ディレクトリへ差し替える。主にテストからの利用を想定する。
    /// プロジェクト方針として<c>InternalsVisibleTo</c>を使わないため、publicとして公開する。
    /// <paramref name="logDirectoryOverride"/>がnullの場合は利用不能状態（no-op）にする。
    /// </summary>
    public static void ResetForTesting(string? logDirectoryOverride)
    {
        linesInLastMinute = 0;
        linesLastMinuteStartTicks = 0;
        if (logDirectoryOverride is null)
        {
            logDirectory = null;
            currentLogPath = null;
            return;
        }
        InitializeLogDirectory(logDirectoryOverride);
    }

    /// <summary>
    /// 現在のログファイルパス（利用不能時null）。
    /// </summary>
    public static string? CurrentLogPath => currentLogPath;

    /// <summary>
    /// カテゴリー付き1行ログを書き込む。
    /// </summary>
    public static void Trace(string category, string message)
    {
        WriteLine(category, message);
        System.Diagnostics.Debugger.Log(0, category, message + Environment.NewLine);
    }

    /// <summary>
    /// 例外情報（型・メッセージ・スタックトレース）をログへ書き込む。
    /// </summary>
    public static void TraceException(string category, Exception ex)
    {
        var message = $"{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
        WriteLine(category, message);
        System.Diagnostics.Debugger.Log(0, category, message + Environment.NewLine);
    }

    static void WriteLine(string category, string message)
    {
        if (currentLogPath is null) return;
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{category}] {message}{Environment.NewLine}";
        try
        {
            // Traceは任意のスレッドから同時多発しうるため、
            // 1分ウィンドウ内の書き込み数制限もwriteLockの範囲内で判定し競合状態を防ぐ。
            lock (writeLock)
            {
                var now = DateTime.UtcNow.Ticks;
                if (now - linesLastMinuteStartTicks > PerMinuteWindowTicks)
                {
                    linesLastMinuteStartTicks = now;
                    linesInLastMinute = 0;
                }
                if (linesInLastMinute >= PerMinuteLimit) return;
                linesInLastMinute++;

                using var stream = new FileStream(
                    currentLogPath, FileMode.Append, FileAccess.Write, FileShare.Read,
                    bufferSize: 4096, options: FileOptions.WriteThrough);
                var bytes = Encoding.UTF8.GetBytes(line);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
        }
#pragma warning disable CA1031 // ログ失敗はサイレント（診断機構が本体クラッシュ原因になってはならない）
        catch (Exception)
#pragma warning restore CA1031
        {
        }
    }

    static void CleanupOldLogs()
    {
        if (logDirectory is null) return;
        try
        {
            var cutoff = DateTime.Now.AddDays(-RetentionDays);
            foreach (var file in Directory.GetFiles(logDirectory, "crash-*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                {
                    try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
