using Launcher.Win32;

namespace Launcher.UI;

/// <summary>コマンドのPATH解決とファイル参照ダイアログへの反映をまとめる。</summary>
public static class CommandFileBrowser
{
    public static void Browse(IWin32Window owner, TextBox input, OpenFileDialog dialog)
    {
        string resolved = FileHelper.ResolveCommandPath(input.Text);
        if (File.Exists(resolved))
        {
            dialog.FileName = resolved;
            string? directory = Path.GetDirectoryName(resolved);
            if (!string.IsNullOrEmpty(directory)) dialog.InitialDirectory = directory;
        }
        if (dialog.ShowDialog(owner) == DialogResult.OK) input.Text = dialog.FileName;
    }
}
