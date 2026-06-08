using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.Core;

/// <summary>
/// 管理者権限への昇格方法に応じた <see cref="ShellProcessStartInfo"/> の加工ロジック。
/// 副作用ある <see cref="Command.Execute"/> から、テスト容易性のために分離した。
/// </summary>
public static class AdminElevationApplier
{
    /// <summary>
    /// 管理者権限での実行方法に応じて <see cref="ShellProcessStartInfo"/> を加工する。
    /// </summary>
    /// <remarks>
    /// 「現在のユーザーが管理者ではないが管理者権限が必要」な状況であることの事前確認は呼び出し元が担う。
    /// </remarks>
    /// <param name="info">加工対象の起動情報。<see cref="ShellProcessStartInfo.FileName"/> および
    /// <see cref="ShellProcessStartInfo.Arguments"/> は呼び出し前に設定済みであること。</param>
    /// <param name="elevation">管理者権限への昇格方法。</param>
    /// <param name="runAsCommandLine"><see cref="AdminElevation.RunAsCommand"/> 時に runas へ渡す引数雛形。</param>
    /// <param name="vECmdPath"><see cref="AdminElevation.VistaElevator"/> 時に呼び出すエレベーター実行ファイルのパス。</param>
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

            case AdminElevation.RunAsCommand: // runasコマンド
                info.Arguments = $"{runAsCommandLine} \"\\\"{info.FileName}\\\" {info.Arguments!.Replace("\"", "\\\"")}\"";
                info.FileName = "runas";
                break;

            case AdminElevation.VistaElevator: // Vistaのエレベーター
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
