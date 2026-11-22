using Launcher.Infrastructure;

namespace Launcher.Core;

public static class AdminElevationApplier
{
    public static void Apply(
        ShellProcessStartInfo info,
        AdminElevation elevation,
        string runAsCommandLine,
        string vECmdPath)
    {
        switch (elevation)
        {
            default:
            case AdminElevation.RunAs:
                info.Verb = "runas";
                break;

            case AdminElevation.RunAsCommand:
                info.Arguments = $"{runAsCommandLine} \"\\\"{info.FileName}\\\" {info.Arguments!.Replace("\"", "\\\"")}\"";
                info.FileName = "runas";
                break;

            case AdminElevation.VistaElevator:
                char delim = '/';
                foreach (char c in ",;*?<>|")
                {
                    if (info.FileName!.IndexOf(delim) < 0 &&
                        info.Arguments!.IndexOf(delim) < 0) break;
                    delim = c;
                }
                info.Arguments = $"0{delim}{info.FileName}{delim}{info.Arguments}{delim}{delim}";
                info.FileName = PathHelper.PathNormalize(vECmdPath);
                break;
        }
    }
}
