namespace Launcher.Core;

/// <summary>Shellで開く対象と起動条件。OS操作は含まない。</summary>
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

    public ShellProcessStartInfo() { }
    public ShellProcessStartInfo(string fileName) => FileName = fileName;
    public ShellProcessStartInfo(string fileName, string arguments)
    {
        FileName = fileName;
        Arguments = arguments;
    }
}
