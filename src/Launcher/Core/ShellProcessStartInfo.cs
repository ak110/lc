namespace Launcher.Core;

public sealed class ShellProcessStartInfo
{
    public string? Arguments { get; set; }
    public string? FileName { get; set; }
    public string? Verb { get; set; }
    public string? WorkingDirectory { get; set; }
    public WindowStyle WindowStyle { get; set; } = WindowStyle.Normal;
    public ProcessPriorityLevel Priority { get; set; } = ProcessPriorityLevel.Normal;
    public bool CreateNoWindow { get; set; }
    public bool ErrorDialog { get; set; }
    public IntPtr ErrorDialogParentHandle { get; set; }

    public ShellProcessStartInfo(string fileName, string arguments)
    {
        FileName = fileName;
        Arguments = arguments;
    }
}
