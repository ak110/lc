namespace Launcher.Core;

/// <summary>
/// UI所有データのパスを取得し、背景計算の結果をUIスレッドで条件付き適用する。
/// 背景スレッドへ渡すのは<see cref="Values"/>の文字列だけとする。
/// </summary>
public sealed class EnvironmentReplacementBatch
{
    readonly List<PathValue> paths;

    EnvironmentReplacementBatch(List<PathValue> paths)
    {
        this.paths = paths;
        Values = paths.Select(path => path.Value).ToArray();
    }

    public IReadOnlyList<string?> Values { get; }

    /// <summary>UIスレッドでコマンドと予定の現在値を取得する。</summary>
    public static EnvironmentReplacementBatch Capture(CommandList commands, SchedulerData scheduler)
    {
        List<PathValue> paths = [];
        foreach (var command in commands.Commands)
        {
            paths.Add(new PathValue(command, false, command.FileName));
            paths.Add(new PathValue(command, true, command.WorkDir));
        }
        foreach (var task in scheduler.Items.SelectMany(item => item.Tasks))
        {
            paths.Add(new PathValue(task, false, task.FileName));
        }
        return new EnvironmentReplacementBatch(paths);
    }

    /// <summary>
    /// UIスレッドで結果を適用する。計算中に追加・変更された値があれば、次の計算が必要なことを返す。
    /// 削除・差し替え済みの実体と、取得時から変わった値は書き換えない。
    /// </summary>
    public bool Apply(CommandList commands, SchedulerData scheduler, IReadOnlyList<string?> replacements)
    {
        ArgumentNullException.ThrowIfNull(replacements);
        if (replacements.Count != paths.Count)
        {
            throw new ArgumentException("置換結果の数が取得した値の数と一致しません。", nameof(replacements));
        }
        var current = Capture(commands, scheduler).paths.ToDictionary(path => (path.Target, path.WorkDir));
        bool changed = current.Count != paths.Count;
        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            if (!current.TryGetValue((path.Target, path.WorkDir), out var latest) || latest.Value != path.Value)
            {
                changed = true;
                continue;
            }
            if (replacements[i] is not { } replacement)
            {
                continue;
            }
            if (path.Target is Command command)
            {
                if (path.WorkDir) command.WorkDir = replacement;
                else command.FileName = replacement;
            }
            else if (path.Target is SchedulerTask task)
            {
                task.FileName = replacement;
            }
        }
        return changed;
    }

    sealed record PathValue(object Target, bool WorkDir, string? Value);
}
