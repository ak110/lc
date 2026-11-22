using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

public class Command : ICloneable, IComparable<Command>, IComparable
{
    public Command() { }

    protected Command(Command source)
    {
        foreach (var property in CopyProperties)
            property.SetValue(this, property.GetValue(source));
    }

    private static readonly System.Reflection.PropertyInfo[] CopyProperties = typeof(Command)
        .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
        .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
        .ToArray();

    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Param { get; set; } = string.Empty;
    public string? WorkDir { get; set; }
    public WindowStyle Show { get; set; } = WindowStyle.Normal;
    public ProcessPriorityLevel Priority { get; set; } = ProcessPriorityLevel.Normal;
    public bool RunAsAdmin { get; set; }

    [XmlIgnore]
    public int IconIndex { get; set; } = -1;

    public Command Clone()
    {
        Command copy = (Command)MemberwiseClone();
        return copy;
    }

    #region ICloneable メンバ

    object ICloneable.Clone()
    {
        return Clone();
    }

    #endregion

    #region IComparable<Command> メンバ

    public int CompareTo(Command? other)
    {
        if (other is null) return 1;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    #endregion

    #region IComparable メンバ

    public int CompareTo(object? obj)
    {
        return CompareTo(obj as Command);
    }

    #endregion

    public static ShellProcessStartInfo? OpenDirectory(Config config, string? resolvedPath, IntPtr owner = default)
    {
        string? path = resolvedPath;
        if (File.Exists(path) || Directory.Exists(path))
        {
            return LaunchRequestBuilder.Create(config.OpenParentFiler,
                $"{config.OpenParentFilerParam1}{path}{config.OpenParentFilerParam2}", owner: owner);
        }
        while (!string.IsNullOrEmpty(path))
        {
            path = Path.GetDirectoryName(path);
            if (path is not null && Directory.Exists(path))
                return config.OpenDirByFiler
                    ? LaunchRequestBuilder.Create(config.Filer, path, path, owner: owner)
                    : LaunchRequestBuilder.Create(path, owner: owner);
        }
        return null;
    }

    public ShellProcessStartInfo Execute(string input, Config config, IntPtr owner, bool isAdministrator)
    {
        string? args = "";
        if (!string.IsNullOrEmpty(input)) ParseInput(input, config, out _, out args);
        var info = LaunchRequestBuilder.Create(FileName, $"{Param} {args}", WorkDir, Show, Priority, owner);
        if (config.OpenDirByFiler && Directory.Exists(info.FileName))
        {
            info.Arguments = info.FileName;
            info.FileName = PathHelper.PathNormalize(config.Filer);
        }
        if (RunAsAdmin && !isAdministrator)
            AdminElevationApplier.Apply(info, config.RunAsAdminType, config.RunAsCommandLine, config.VECmdPath);
        return info;
    }

    private static readonly string[] LineSeparators = ["\r\n", "\n"];

    public static Command LoadFrom(string? name, string data)
    {
        var cmd = new Command();
        string[] list = data.Split(LineSeparators, StringSplitOptions.None);
        int i = 0;
        cmd.Name = name ?? list[i++];
        cmd.FileName = list[i++];
        cmd.Param = list[i++];
        cmd.WorkDir = list[i++];
        int showVal;
        if (!int.TryParse(list[i++], out showVal)) showVal = 0;
        cmd.Show = (WindowStyle)showVal;
        int priorityVal;
        if (!int.TryParse(list[i++], out priorityVal)) priorityVal = 3;
        if (5 <= priorityVal)
        {
            priorityVal = 5;
        }
        cmd.Priority = (ProcessPriorityLevel)priorityVal;
        return cmd;
    }

    public bool ParseInput(string input, Config config, out string commandName, out string? arguments)
    {
        return CommandMatcher.ParseInput(Name, input, config, out commandName, out arguments);
    }

    public int GetMatchScore(string input, Config config)
    {
        return CommandMatcher.GetMatchScore(Name, input, config);
    }

}
