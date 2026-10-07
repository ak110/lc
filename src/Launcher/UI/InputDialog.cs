namespace Launcher.UI;

/// <summary>タブ名など短い文字列を入力する共通ダイアログ。</summary>
public sealed class InputDialog : Form
{
    readonly TextBox input;

    public InputDialog(string prompt, string title, string value)
    {
        Text = title;
        ClientSize = new Size(300, 100);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        var label = new Label { Text = prompt, Left = 8, Top = 8, Width = 280 };
        input = new TextBox { Text = value, Left = 8, Top = 32, Width = 280, TabIndex = 1 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 120, Top = 64, Width = 75, TabIndex = 2 };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Left = 200, Top = 64, Width = 75, TabIndex = 3 };
        Controls.AddRange([label, input, ok, cancel]);
        AcceptButton = ok;
        CancelButton = cancel;
        ActiveControl = input;
    }

    /// <summary>OKで確定した文字列を返す。キャンセルはnullを返す。</summary>
    public static string? Prompt(IWin32Window owner, string prompt, string title, string value)
    {
        using var dialog = new InputDialog(prompt, title, value);
        return dialog.ShowDialogOver(owner) == DialogResult.OK ? dialog.input.Text : null;
    }
}
