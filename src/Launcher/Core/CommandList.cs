using Launcher.Infrastructure;

namespace Launcher.Core;

[System.Diagnostics.DebuggerDisplay("Count = {Count}")]
public sealed class CommandList : ConfigStore, ICloneable
{
    public List<Command> Commands { get; set; } = new List<Command>();

    public int Count
    {
        get { return Commands.Count; }
    }

    public void ReplaceContents(CommandList loaded)
    {
        if (ReferenceEquals(this, loaded)) return;
        var retained = Commands.GroupBy(Definition)
            .ToDictionary(group => group.Key, group => new Queue<Command>(group));
        var replacements = loaded.Commands.Select(command =>
            retained.TryGetValue(Definition(command), out var matches) && matches.TryDequeue(out var existing)
                ? existing : command).ToList();
        Commands.Clear();
        Commands.AddRange(replacements);
    }

    static (string Name, string File, string Param, string Directory, WindowStyle Show,
        ProcessPriorityLevel Priority, bool Admin) Definition(Command command) =>
        (command.Name, Environment.ExpandEnvironmentVariables(command.FileName), command.Param,
            Environment.ExpandEnvironmentVariables(command.WorkDir ?? string.Empty),
            command.Show, command.Priority, command.RunAsAdmin);

    public CommandList Clone()
    {
        CommandList copy = (CommandList)MemberwiseClone();
        copy.Commands = Commands.ConvertAll(c => (Command)c.Clone());
        return copy;
    }

    #region ICloneable メンバ

    object ICloneable.Clone()
    {
        return Clone();
    }

    #endregion

    #region Serialize/Deserialize

    public bool Save(Action<ConfigSaveFailure> notify, string? baseName = null)
    {
        Commands.Sort();
        return Store.Save(this, notify, baseName);
    }

    public static bool AddAndSave(Command command, Action<ConfigSaveFailure> notify,
        Action<ConfigLoadResult<CommandList>> loadFailed, string? baseName = null)
        => Store.ModifyAndSave(list => list.Add(command), notify, loadFailed, baseName);

    public static ConfigLoadResult<CommandList> Load(string? baseName = null) => Store.Load(baseName);

    public static ConfigFile<CommandList> Store { get; } = new(".cmd.cfg", LoadLegacy);

    static CommandList LoadLegacy(byte[] content)
    {
        using var stream = new MemoryStream(content);
        return LoadFrom(new LegacyConfigReader(stream, false));
    }

    #endregion

    public static CommandList LoadFrom(LegacyConfigReader reader)
    {
        var list = new CommandList();
        int n;
        if (reader.ContainsKey("_") &&
            int.TryParse(reader.Indirect("_"), out n))
        {
            for (int i = 0; i < n; i++)
            {
                string key = i.ToString("d3");
                Command cmd = Command.LoadFrom(null,
                    reader.EscapedString(key));
                list.Commands.Add(cmd);
            }
        }
        else
        {
            foreach (string key in reader.Keys)
            {
                Command cmd = Command.LoadFrom(key,
                    reader.EscapedString(key));
                list.Commands.Add(cmd);
            }
        }
        list.Commands.Sort();
        return list;
    }

    public IEnumerable<Command> FindMatch(string input, Config config)
    {
        if (string.IsNullOrEmpty(input))
        {
            return [.. Commands];
        }
        return Commands
            .Select(x => new { Command = x, Score = x.GetMatchScore(input, config) })
            .Where(x => 0 < x.Score)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Command);
    }

    public void Add(Command command)
    {
        Commands.Add(command);
        Commands.Sort();
    }
}
