namespace Launcher.Infrastructure;

/// <summary>永続診断ログの共通出力先。</summary>
public static class DiagnosticLog
{
    static readonly DiagnosticLogWriter writer = CreateWriter();

    static DiagnosticLogWriter CreateWriter()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (exeDir is not null) MigrateLegacyDirectory(exeDir);
        return new DiagnosticLogWriter(exeDir is null ? null : Path.Combine(exeDir, "logs"));
    }

    public static void Debug(string category, string message) => writer.Debug(category, message);
    public static void Info(string category, string message) => writer.Info(category, message);
    public static void Warn(string category, string message) => writer.Warn(category, message);
    public static void Error(string category, string message) => writer.Error(category, message);
    public static void Error(string category, Exception ex) => writer.Error(category, ex);

    /// <summary>旧crash-logのログをlogsへ移し、移動先の既存ログを保護する。</summary>
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

}
