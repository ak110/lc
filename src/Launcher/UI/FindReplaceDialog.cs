using System.Drawing;
using System.Text.RegularExpressions;

namespace Launcher.UI;

/// <summary>
/// 検索・置換用のモードレスダイアログ。
/// 呼び出し元(MemoForm)は現在のタブへ検索・置換を委譲するコールバックを渡す。
/// 1つのインスタンスを検索モードと置換モードで共用し、閉じる操作では非表示にするだけとして
/// 検索文字列・置換後文字列・オプションを次回表示時まで保持する。
/// </summary>
public sealed class FindReplaceDialog : Form
{
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

    bool replaceMode;

    public event EventHandler<FindEventArgs>? FindNext;
    public event EventHandler<FindEventArgs>? FindPrev;
    public event EventHandler<ReplaceEventArgs>? Replace;
    public event EventHandler<ReplaceEventArgs>? ReplaceAll;

    public string FindText => findTextBox.Text;
    public string ReplaceText => replaceTextBox.Text;
    public bool MatchCase => matchCaseCheckBox.Checked;
    public bool UseRegex => regexCheckBox.Checked;

    public FindReplaceDialog()
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        // TabIndexはラベル→入力欄→オプション→ボタンの順とし、置換モードでは検索文字列の次を置換後にする
        findLabel = new Label { Text = "検索文字列(&N):", AutoSize = true, TabIndex = 0 };
        findTextBox = new TextBox { TabIndex = 1 };
        replaceLabel = new Label { Text = "置換後(&P):", AutoSize = true, TabIndex = 2 };
        replaceTextBox = new TextBox { TabIndex = 3 };
        matchCaseCheckBox = new CheckBox { Text = "大文字と小文字を区別(&C)", AutoSize = true, TabIndex = 4 };
        regexCheckBox = new CheckBox { Text = "正規表現(&E)", AutoSize = true, TabIndex = 5 };
        findNextButton = new Button { Text = "次を検索(&F)", TabIndex = 6 };
        findPrevButton = new Button { Text = "前を検索(&V)", TabIndex = 7 };
        replaceButton = new Button { Text = "置換(&R)", TabIndex = 8 };
        replaceAllButton = new Button { Text = "すべて置換(&A)", TabIndex = 9 };
        closeButton = new Button { Text = "閉じる", TabIndex = 10 };
        statusLabel = new Label { ForeColor = Color.DarkRed, AutoEllipsis = true };

        findNextButton.Click += (s, e) => RaiseFindNext();
        findPrevButton.Click += (s, e) => RaiseFindPrev();
        replaceButton.Click += (s, e) => RaiseReplace();
        replaceAllButton.Click += (s, e) => RaiseReplaceAll();
        closeButton.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            findLabel, findTextBox, replaceLabel, replaceTextBox,
            matchCaseCheckBox, regexCheckBox,
            findNextButton, findPrevButton, replaceButton, replaceAllButton, closeButton,
            statusLabel,
        });

        AcceptButton = findNextButton;
        CancelButton = closeButton;

        ApplyMode(replaceMode: false);
    }

    /// <summary>
    /// 指定モードでダイアログを表示するか、既に表示中であればモードを切り替えてアクティブ化する。
    /// 初期値が指定された場合は検索文字列を置き換え、未指定なら前回の検索文字列を保持する。
    /// 新たに表示する場合はオーナーの中央へ配置する。
    /// </summary>
    public void ShowOrActivate(Form owner, bool replaceMode, string? initialText)
    {
        ApplyMode(replaceMode);
        if (!string.IsNullOrEmpty(initialText))
        {
            findTextBox.Text = initialText;
        }
        statusLabel.Text = string.Empty;

        if (Visible)
        {
            Activate();
        }
        else
        {
            // モードレス表示ではCenterParentが適用されないため、自前で中央へ配置する
            var bounds = owner.Bounds;
            var pos = new Point(
                bounds.Left + (bounds.Width - Width) / 2,
                bounds.Top + (bounds.Height - Height) / 2);
            FormsHelper.SetLocationWithClip(this, pos);
            Show(owner);
        }
        findTextBox.Focus();
        findTextBox.SelectAll();
    }

    /// <summary>
    /// 結果メッセージを表示する。
    /// </summary>
    public void SetStatus(string message) => statusLabel.Text = message;

    /// <summary>
    /// 現在の入力から検索条件を生成する。検索文字列が空か正規表現が無効な場合はfalseを返し、
    /// 状態欄へ理由を表示する。
    /// </summary>
    public bool TryGetFindCondition(out FindEventArgs args)
    {
        args = new FindEventArgs(FindText, MatchCase, UseRegex);
        if (string.IsNullOrEmpty(FindText))
        {
            statusLabel.Text = "検索文字列を入力してください。";
            return false;
        }
        if (UseRegex)
        {
            try
            {
                _ = new Regex(FindText);
            }
            catch (ArgumentException ex)
            {
                statusLabel.Text = $"正規表現エラー: {ex.Message}";
                return false;
            }
        }
        statusLabel.Text = string.Empty;
        return true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 利用者の閉じる操作は非表示に留め、入力内容とオプションを保持する
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyLayout();
    }

    /// <summary>
    /// モードに応じてタイトルと置換関連コントロールの表示を切り替え、再配置する。
    /// </summary>
    void ApplyMode(bool replaceMode)
    {
        this.replaceMode = replaceMode;
        Text = replaceMode ? "置換" : "検索";
        replaceLabel.Visible = replaceMode;
        replaceTextBox.Visible = replaceMode;
        replaceButton.Visible = replaceMode;
        replaceAllButton.Visible = replaceMode;
        ApplyLayout();
    }

    /// <summary>
    /// フォントの実測寸法から各コントロールを配置する。
    /// 左列に入力欄とオプション、右列にボタンを縦に並べ、最下段に状態欄を置く。
    /// 固定ピクセルを使わないため、DPIとフォントの違いによる重なりと欠けが生じない。
    /// </summary>
    void ApplyLayout()
    {
        int unit = Font.Height;
        int margin = unit;
        int gap = unit / 2;
        int rowHeight = findTextBox.PreferredHeight + gap;
        int buttonHeight = findTextBox.PreferredHeight + gap / 2;

        int labelWidth = Math.Max(findLabel.PreferredSize.Width, replaceLabel.PreferredSize.Width);
        int inputLeft = margin + labelWidth + gap;
        // 左列の幅はチェックボックスが収まる幅を下限とする
        int optionWidth = Math.Max(matchCaseCheckBox.PreferredSize.Width, regexCheckBox.PreferredSize.Width);
        int inputWidth = Math.Max(unit * 16, optionWidth - labelWidth - gap);
        int buttonLeft = inputLeft + inputWidth + margin;
        int buttonWidth = new[] { findNextButton, findPrevButton, replaceButton, replaceAllButton, closeButton }
            .Max(b => b.PreferredSize.Width);

        // 左列
        int top = margin;
        PlaceRow(findLabel, findTextBox, top, margin, inputLeft, inputWidth);
        top += rowHeight;
        if (replaceMode)
        {
            PlaceRow(replaceLabel, replaceTextBox, top, margin, inputLeft, inputWidth);
            top += rowHeight;
        }
        matchCaseCheckBox.Location = new Point(margin, top + (rowHeight - matchCaseCheckBox.Height) / 2);
        top += rowHeight;
        regexCheckBox.Location = new Point(margin, top + (rowHeight - regexCheckBox.Height) / 2);
        top += rowHeight;
        int leftBottom = top;

        // 右列 (Visibleはフォーム非表示中に常にfalseとなるため、モードから配置対象を決める)
        top = margin;
        var buttons = replaceMode
            ? new[] { findNextButton, findPrevButton, replaceButton, replaceAllButton, closeButton }
            : new[] { findNextButton, findPrevButton, closeButton };
        foreach (var button in buttons)
        {
            button.SetBounds(buttonLeft, top, buttonWidth, buttonHeight);
            top += rowHeight;
        }
        int rightBottom = top;

        // 状態欄
        int statusTop = Math.Max(leftBottom, rightBottom);
        int width = buttonLeft + buttonWidth + margin;
        statusLabel.SetBounds(margin, statusTop, width - margin * 2, unit + gap / 2);

        ClientSize = new Size(width, statusTop + statusLabel.Height + margin);
    }

    static void PlaceRow(Label label, TextBox textBox, int top, int labelLeft, int inputLeft, int inputWidth)
    {
        textBox.SetBounds(inputLeft, top, inputWidth, textBox.PreferredHeight);
        label.Location = new Point(labelLeft, top + (textBox.Height - label.Height) / 2);
    }

    void RaiseFindNext()
    {
        if (!TryGetFindCondition(out var args)) return;
        FindNext?.Invoke(this, args);
    }

    void RaiseFindPrev()
    {
        if (!TryGetFindCondition(out var args)) return;
        FindPrev?.Invoke(this, args);
    }

    void RaiseReplace()
    {
        if (!TryGetFindCondition(out var args)) return;
        Replace?.Invoke(this, new ReplaceEventArgs(args.Pattern, ReplaceText, args.MatchCase, args.UseRegex));
    }

    void RaiseReplaceAll()
    {
        if (!TryGetFindCondition(out var args)) return;
        ReplaceAll?.Invoke(this, new ReplaceEventArgs(args.Pattern, ReplaceText, args.MatchCase, args.UseRegex));
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
