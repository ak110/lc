namespace Launcher.Tests;

/// <summary>
/// 共通の診断ログ（実行ファイルのフォルダーの<c>logs/</c>）へ、観測開始後に追記された行を読む。
/// 他のテストも同じログへ書くため、検査は対象を一意に識別できる値で行を絞って行う。
/// </summary>
internal sealed class DiagnosticLogDelta
{
    readonly string directory = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "logs");
    readonly Dictionary<string, long> prior;

    DiagnosticLogDelta()
    {
        prior = Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.log").ToDictionary(path => path, path => new FileInfo(path).Length)
            : [];
    }

    public static DiagnosticLogDelta Start() => new();

    /// <summary>観測開始後に追記された全行。</summary>
    public IReadOnlyList<string> Lines()
    {
        if (!Directory.Exists(directory)) return [];
        List<string> lines = [];
        foreach (var path in Directory.EnumerateFiles(directory, "*.log"))
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(Math.Min(prior.GetValueOrDefault(path), stream.Length), SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            lines.AddRange(reader.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        }
        return lines;
    }
}
