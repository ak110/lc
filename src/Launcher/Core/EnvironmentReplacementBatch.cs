namespace Launcher.Core;

public sealed class EnvironmentReplacementBatch
{
    readonly List<PathValue> paths;

    EnvironmentReplacementBatch(List<PathValue> paths)
    {
        this.paths = paths;
        Values = paths.Select(path => path.Value).ToArray();
    }

    public IReadOnlyList<string?> Values { get; }

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
