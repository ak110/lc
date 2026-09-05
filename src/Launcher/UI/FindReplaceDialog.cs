using System.Drawing;
using System.Text.RegularExpressions;

namespace Launcher.UI;

/// <summary>
/// 検索・置換用のモードレスダイアログ。
/// 呼び出し元(MemoForm)は現在のタブへ検索・置換を委譲するコールバックを渡す。
/// </summary>
public sealed class FindReplaceDialog : Form
{
    readonly bool replaceMode;
    readonly Label findLabel;
    readonly TextBox findTextBox;
    readonly Label replaceLabel;
    readonly TextBox replaceTextBox;
    readonly CheckBox matchCaseCheckBox;
    readonly CheckBox regexCheckBox;
    readonly Button findNextButton;
    readonly Button findPrevButton;
    readonly Button replaceButton;
    readonly Button replaceAllButton;
    readonly Button closeButton;
    readonly Label statusLabel;

    public event EventHandler<FindEventArgs>? FindNext;
    public event EventHandler<FindEventArgs>? FindPrev;
    public event EventHandler<ReplaceEventArgs>? Replace;
    public event EventHandler<ReplaceEventArgs>? ReplaceAll;

    public string FindText => findTextBox.Text;
    public string ReplaceText => replaceTextBox.Text;
    public bool MatchCase => matchCaseCheckBox.Checked;
    public bool UseRegex => regexCheckBox.Checked;

    public FindReplaceDialog(bool replaceMode)
    {
        this.replaceMode = replaceMode;
        Text = replaceMode ? "置換" : "検索";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        int labelWidth = 72;
        int inputLeft = 88;
        int inputWidth = 240;
        int buttonLeft = 336;
        int buttonWidth = 96;
        int rowHeight = 28;
        int top = 12;

        findLabel = new Label { Text = "検索文字列(&N):", Left = 8, Top = top + 4, Width = labelWidth, TextAlign = ContentAlignment.MiddleLeft };
        findTextBox = new TextBox { Left = inputLeft, Top = top, Width = inputWidth };
        findNextButton = new Button { Text = "次を検索(&F)", Left = buttonLeft, Top = top - 2, Width = buttonWidth };
        findNextButton.Click += (s, e) => RaiseFindNext();

        top += rowHeight;

        replaceLabel = new Label { Text = "置換後(&P):", Left = 8, Top = top + 4, Width = labelWidth, TextAlign = ContentAlignment.MiddleLeft, Visible = replaceMode };
        replaceTextBox = new TextBox { Left = inputLeft, Top = top, Width = inputWidth, Visible = replaceMode };
        findPrevButton = new Button { Text = "前を検索(&V)", Left = buttonLeft, Top = top - 2, Width = buttonWidth };
        findPrevButton.Click += (s, e) => RaiseFindPrev();

        top += rowHeight;

        replaceButton = new Button { Text = "置換(&R)", Left = buttonLeft, Top = top - 2, Width = buttonWidth, Visible = replaceMode };
        replaceButton.Click += (s, e) => RaiseReplace();

        top += rowHeight;

        replaceAllButton = new Button { Text = "すべて置換(&A)", Left = buttonLeft, Top = top - 2, Width = buttonWidth, Visible = replaceMode };
        replaceAllButton.Click += (s, e) => RaiseReplaceAll();

        top += rowHeight;

        matchCaseCheckBox = new CheckBox { Text = "大文字と小文字を区別(&C)", Left = 8, Top = top, Width = 220 };
        regexCheckBox = new CheckBox { Text = "正規表現(&E)", Left = 232, Top = top, Width = 128 };
        closeButton = new Button { Text = "閉じる", Left = buttonLeft, Top = top - 2, Width = buttonWidth };
        closeButton.Click += (s, e) => Close();

        top += rowHeight;

        statusLabel = new Label { Left = 8, Top = top, Width = 420, Height = 20, ForeColor = Color.DarkRed };

        ClientSize = new Size(buttonLeft + buttonWidth + 12, top + 28);

        Controls.AddRange(new Control[]
        {
            findLabel, findTextBox, findNextButton, findPrevButton,
            replaceLabel, replaceTextBox, replaceButton, replaceAllButton,
            matchCaseCheckBox, regexCheckBox, closeButton, statusLabel,
        });

        AcceptButton = findNextButton;
        CancelButton = closeButton;

        // Escで閉じる（CancelButtonで動作するが、モードレスのKeyPreviewも念のため）
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// ダイアログを表示するか、既に表示中であればアクティブ化する。検索文字列に初期値を設定する。
    /// </summary>
    public void ShowOrActivate(IWin32Window owner, string? initialText)
    {
        if (!string.IsNullOrEmpty(initialText))
        {
            findTextBox.Text = initialText;
        }
        if (Visible)
        {
            Activate();
        }
        else
        {
            Show(owner);
        }
        findTextBox.Focus();
        findTextBox.SelectAll();
        statusLabel.Text = string.Empty;
    }

    /// <summary>
    /// 結果メッセージを表示する。
    /// </summary>
    public void SetStatus(string message) => statusLabel.Text = message;

    /// <summary>
    /// 正規表現の妥当性を検査する。無効な場合は状態欄へメッセージを表示してfalseを返す。
    /// </summary>
    bool ValidateRegex()
    {
        if (!UseRegex) return true;
        try
        {
            _ = new Regex(FindText);
            return true;
        }
        catch (ArgumentException ex)
        {
            statusLabel.Text = $"正規表現エラー: {ex.Message}";
            return false;
        }
    }

    void RaiseFindNext()
    {
        if (string.IsNullOrEmpty(FindText) || !ValidateRegex()) return;
        statusLabel.Text = string.Empty;
        FindNext?.Invoke(this, new FindEventArgs(FindText, MatchCase, UseRegex));
    }

    void RaiseFindPrev()
    {
        if (string.IsNullOrEmpty(FindText) || !ValidateRegex()) return;
        statusLabel.Text = string.Empty;
        FindPrev?.Invoke(this, new FindEventArgs(FindText, MatchCase, UseRegex));
    }

    void RaiseReplace()
    {
        if (string.IsNullOrEmpty(FindText) || !ValidateRegex()) return;
        statusLabel.Text = string.Empty;
        Replace?.Invoke(this, new ReplaceEventArgs(FindText, ReplaceText, MatchCase, UseRegex));
    }

    void RaiseReplaceAll()
    {
        if (string.IsNullOrEmpty(FindText) || !ValidateRegex()) return;
        statusLabel.Text = string.Empty;
        ReplaceAll?.Invoke(this, new ReplaceEventArgs(FindText, ReplaceText, MatchCase, UseRegex));
    }
}

public class FindEventArgs(string pattern, bool matchCase, bool useRegex) : EventArgs
{
    public string Pattern { get; } = pattern;
    public bool MatchCase { get; } = matchCase;
    public bool UseRegex { get; } = useRegex;
}

public sealed class ReplaceEventArgs(string pattern, string replacement, bool matchCase, bool useRegex)
    : FindEventArgs(pattern, matchCase, useRegex)
{
    public string Replacement { get; } = replacement;
}
