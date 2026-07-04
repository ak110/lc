using System.Text;
using System.Windows.Forms;

namespace Launcher.Infrastructure;

/// <summary>
/// 永続診断ログ。AV等CLR corrupted-state exceptionでもプロセス終了前にログを残す用途。
/// 詳細は.claude/rules/win32-interop.md「AccessViolationクラッシュの診断」節を参照。
/// レベル別APIの使い分けは.claude/rules/logging.md「出力API」節を参照する。
/// </summary>
public static class DiagnosticLog
{
    static readonly Lock writeLock = new();
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
        if (exeDir is not null)
        {
            MigrateLegacyDirectory(exeDir);
        }
        InitializeLogDirectory(exeDir is null ? null : Path.Combine(exeDir, "logs"));
    }

    /// <summary>
    /// 旧`crash-log`ディレクトリを`logs`へ一度きり移行する。
    /// 移動先で同名ファイルが既存の場合はそのファイルの移動を飛ばす（既存ログを保護する方針）。
    /// 例外が発生した場合は移行処理を中断する（診断機構が本体クラッシュ原因にならない方針）。
    /// テスト経路から呼び出せるようpublicで公開する。
    /// </summary>
    public static void MigrateLegacyDirectory(string exeDir)
    {
        try
        {
            var legacy = Path.Combine(exeDir, "crash-log");
            var target = Path.Combine(exeDir, "logs");
            if (!Directory.Exists(legacy)) return;
            if (!Directory.Exists(target))
            {
                Directory.Move(legacy, target);
                return;
            }
            foreach (var file in Directory.GetFiles(legacy, "*.log"))
            {
                var dest = Path.Combine(target, Path.GetFileName(file));
                if (File.Exists(dest)) continue;
                File.Move(file, dest);
            }
            if (!Directory.EnumerateFileSystemEntries(legacy).Any())
            {
                Directory.Delete(legacy);
            }
        }
#pragma warning disable CA1031 // 例外を捕捉し移行処理を中断する（診断機構が本体クラッシュ原因にならない方針）
        catch (Exception)
#pragma warning restore CA1031
        {
        }
    }

    static void InitializeLogDirectory(string? logDir)
    {
        try
        {
            if (logDir is null) return;
            logDirectory = logDir;
            Directory.CreateDirectory(logDirectory);
            currentLogPath = Path.Combine(logDirectory, $"{DateTime.Now:yyyyMMdd}.log");
            CleanupOldLogs();
        }
#pragma warning disable CA1031 // 例外を捕捉し初期化を中断してno-op状態へフォールバックする（診断機構が本体クラッシュ原因にならない方針）
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

    /// <summary>詳細トレース。通常運用では出さない、原因調査用の細粒度ログ。</summary>
    public static void Debug(string category, string message) => Write("DEBUG", category, message);

    /// <summary>ユーザー操作・重要な状態遷移。運用時に残す通常ログ。</summary>
    public static void Info(string category, string message) => Write("INFO", category, message);

    /// <summary>想定外だが継続可能な事象。原因確認候補として残す。</summary>
    public static void Warn(string category, string message) => Write("WARN", category, message);

    /// <summary>例外・失敗の記録（メッセージ版）。</summary>
    public static void Error(string category, string message) => Write("ERROR", category, message);

    /// <summary>例外・失敗の記録（例外版）。型・メッセージ・スタックトレースを書き込む。</summary>
    public static void Error(string category, Exception ex)
    {
        var message = $"{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
        Write("ERROR", category, message);
    }

    static void Write(string level, string category, string message)
    {
        WriteLine(level, category, message);
        System.Diagnostics.Debugger.Log(0, category, message + Environment.NewLine);
    }

    static void WriteLine(string level, string category, string message)
    {
        if (currentLogPath is null) return;
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{category}] {message}{Environment.NewLine}";
        try
        {
            // 任意のスレッドから同時多発しうるため、
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
#pragma warning disable CA1031 // 例外を捕捉しログ書き込みを中断する（診断機構が本体クラッシュ原因にならない方針）
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
            foreach (var file in Directory.GetFiles(logDirectory, "*.log"))
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
