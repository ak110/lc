using System.Runtime.InteropServices;
using System.Text;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// 書式を持たないプレーンテキスト運用のRichTextBox派生。
/// 右端で折り返し、行番号は持たない。
/// 貼り付けは書式と画像を除去してテキストのみ挿入する。
/// コピーは選択文字列をプレーンテキストとしてクリップボードへ格納する。
/// 取り消し上限を大きく設定し、実用上は無制限に取り消せる。
/// 複数行選択時のTab/Shift+Tabは行単位のインデント/アンインデントとして扱う。
/// フォーマット矩形へ余白を設ける。
/// </summary>
public sealed class PlainRichTextBox : RichTextBox
{
    // 取り消し上限。十分大きい値を設定し、実用上は無制限とする。
    const int UndoLimit = 1000000;

    // インデント幅 (半角スペース数)
    internal const int IndentWidth = 2;

    readonly Func<bool> containsClipboardText;
    readonly Func<string> getClipboardText;
    readonly Action<string> setClipboardText;

    public PlainRichTextBox() : this(
        () => Clipboard.ContainsText(),
        () => Clipboard.GetText(),
        text => Clipboard.SetText(text))
    {
    }

    PlainRichTextBox(
        Func<bool> containsClipboardText,
        Func<string> getClipboardText,
        Action<string> setClipboardText)
    {
        this.containsClipboardText = containsClipboardText;
        this.getClipboardText = getClipboardText;
        this.setClipboardText = setClipboardText;
        Multiline = true;
        WordWrap = true;
        AcceptsTab = true;
        DetectUrls = false;
        ScrollBars = RichTextBoxScrollBars.Vertical;
        BorderStyle = BorderStyle.None;
        Dock = DockStyle.Fill;
    }

    /// <summary>
    /// フォントを全体へ一括適用する。
    /// RichTextBoxのFont設定は既存テキストを含む全体へ反映される。
    /// </summary>
    public void ApplyFont(Font font)
    {
        Font = font;
        ApplyPadding();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // ハンドル生成後に取り消し上限を設定する (ハンドル再生成時にも再設定される)
        new WindowHelper(Handle).SendMessage(WM.EM_SETUNDOLIMIT, (IntPtr)UndoLimit, IntPtr.Zero);
        ApplyPadding();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyPadding();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyPadding();
    }

    /// <summary>
    /// EM_SETRECTでフォーマット矩形へ内側余白 (約0.5em) を設定する。
    /// </summary>
    void ApplyPadding()
    {
        if (!IsHandleCreated) return;
        int pad = Math.Max(1, Font.Height / 2);
        var rect = new RECT
        {
            Left = pad,
            Top = pad,
            Right = Math.Max(pad + 1, ClientSize.Width - pad),
            Bottom = Math.Max(pad + 1, ClientSize.Height - pad),
        };
        SendMessageRect(Handle, EM_SETRECT, IntPtr.Zero, ref rect);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.V:
            case Keys.Shift | Keys.Insert:
                PastePlainText();
                return true;
            case Keys.Control | Keys.C:
            case Keys.Control | Keys.Insert:
                CopyPlainText();
                return true;
            case Keys.Tab:
                if (TryHandleMultiLineIndent(dedent: false)) return true;
                return base.ProcessCmdKey(ref msg, keyData);
            case Keys.Shift | Keys.Tab:
                if (TryHandleMultiLineIndent(dedent: true)) return true;
                return base.ProcessCmdKey(ref msg, keyData);
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM.WM_PASTE:
                PastePlainText();
                return;
            case WM.WM_COPY:
                CopyPlainText();
                return;
            default:
                base.WndProc(ref m);
                return;
        }
    }

    /// <summary>
    /// クリップボードのテキストのみを書式無しで挿入する。書式と画像は破棄する。
    /// </summary>
    void PastePlainText()
    {
        try
        {
            if (containsClipboardText())
            {
                SelectedText = getClipboardText();
            }
        }
        catch (ExternalException)
        {
            // クリップボードが他プロセスにロックされている場合は貼り付けを行わない
        }
    }

    /// <summary>
    /// 選択文字列のみをプレーンテキストとしてクリップボードへ格納する。
    /// </summary>
    void CopyPlainText()
    {
        if (SelectionLength == 0) return;

        try
        {
            setClipboardText(SelectedText);
        }
        catch (ExternalException)
        {
            // クリップボードが他プロセスにロックされている場合はコピーを行わない
        }
    }

    /// <summary>
    /// 複数行にまたがる選択に対してTab/Shift+Tabで行単位のインデント調整を行う。
    /// 単一行選択では基底処理へ委ねる (単純なTab挿入)。
    /// </summary>
    bool TryHandleMultiLineIndent(bool dedent)
    {
        int selStart = SelectionStart;
        int selLength = SelectionLength;
        if (selLength <= 0) return false;

        string text = Text;
        if (!ContainsLineBreak(text, selStart, selLength)) return false;

        string replaced = ComputeIndentReplacement(
            text, selStart, selLength, dedent,
            out int regionStart, out int regionLength, out int newSelLength);

        SelectionStart = regionStart;
        SelectionLength = regionLength;
        SelectedText = replaced;

        SelectionStart = regionStart;
        SelectionLength = newSelLength;
        return true;
    }

    static bool ContainsLineBreak(string text, int start, int length)
    {
        int end = start + length;
        for (int i = start; i < end && i < text.Length; i++)
        {
            if (text[i] == '\n') return true;
        }
        return false;
    }

    /// <summary>
    /// 選択範囲を行単位へ拡張し、インデントを加減した置換文字列と新しい選択範囲を算出する。
    /// </summary>
    internal static string ComputeIndentReplacement(
        string text, int selStart, int selLength, bool dedent,
        out int regionStart, out int regionLength, out int newSelLength)
    {
        int selEnd = selStart + selLength;

        // 行頭まで拡張
        int lineStart = selStart;
        while (lineStart > 0 && text[lineStart - 1] != '\n') lineStart--;

        // 行末まで拡張。ただし選択が改行直後 (次行冒頭) で終わる場合はその行を含めない。
        int regionEnd;
        if (selEnd > selStart && selEnd <= text.Length && text[selEnd - 1] == '\n')
        {
            regionEnd = selEnd;
        }
        else
        {
            regionEnd = selEnd;
            while (regionEnd < text.Length && text[regionEnd] != '\n') regionEnd++;
        }

        regionStart = lineStart;
        regionLength = regionEnd - lineStart;

        string segment = text.Substring(regionStart, regionLength);
        // \nで分割し、末尾が\nなら末尾の空要素は変換対象外 (次行冒頭)
        string[] lines = segment.Split('\n');
        int transformCount = segment.EndsWith('\n') ? lines.Length - 1 : lines.Length;

        string indent = new(' ', IndentWidth);
        var sb = new StringBuilder(segment.Length + transformCount * IndentWidth);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (i < transformCount)
            {
                if (dedent)
                {
                    int remove = 0;
                    while (remove < IndentWidth && remove < line.Length && line[remove] == ' ')
                    {
                        remove++;
                    }
                    if (remove > 0) line = line[remove..];
                }
                else
                {
                    // \rを除いた実質が空行なら追加しない
                    string trimmed = line.TrimEnd('\r');
                    if (trimmed.Length > 0)
                    {
                        line = indent + line;
                    }
                }
            }
            sb.Append(line);
            if (i < lines.Length - 1) sb.Append('\n');
        }

        string replaced = sb.ToString();
        newSelLength = replaced.Length;
        return replaced;
    }

    #region P/Invoke

    const int EM_SETRECT = 0x00B3;

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref RECT lParam);

    static int SendMessageRect(IntPtr hWnd, int Msg, IntPtr wParam, ref RECT lParam)
        => SendMessage(hWnd, Msg, wParam, ref lParam);

    #endregion
}
