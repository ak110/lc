using System.Runtime.InteropServices;
using Launcher.Core;
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

    public PlainRichTextBox()
    {
        Multiline = true;
        WordWrap = true;
        AcceptsTab = true;
        DetectUrls = false;
        ScrollBars = RichTextBoxScrollBars.Vertical;
        BorderStyle = BorderStyle.None;
        Dock = DockStyle.Fill;
        // 検索ダイアログへフォーカスが移っても一致箇所の選択を表示し続ける
        HideSelection = false;
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
        // ハンドル生成後に設定する (ハンドル再生成時にも再設定される)
        var window = new WindowHelper(Handle);
        window.SendMessage(WM.EM_SETUNDOLIMIT, (IntPtr)UndoLimit, IntPtr.Zero);
        DisableAutomaticFontBinding(window);
        ApplyPadding();
    }

    /// <summary>
    /// 文字ごとにフォントを割り当てるRichEditの既定動作を止める。
    /// IMF_DUALFONTはASCIIへ英文フォント、アジア文字へアジアフォントを割り当て、
    /// IMF_AUTOFONTはキーボードレイアウトの切り替えに応じてフォントを変える。
    /// いずれも既定で有効なため、書式を持たない運用では入力経路と文字種で文字書式が分かれる。
    /// EM_SETLANGOPTIONSは言語オプションの全ビットを設定するため、
    /// 現在値を取得し、対象の3ビットだけを無効化した値を書き戻す。
    /// </summary>
    static void DisableAutomaticFontBinding(WindowHelper window)
    {
        int options = window.SendMessage(WM.EM_GETLANGOPTIONS, IntPtr.Zero, IntPtr.Zero);
        int updated = options & ~(IMF_AUTOFONT | IMF_AUTOFONTSIZEADJUST | IMF_DUALFONT);
        window.SendMessage(WM.EM_SETLANGOPTIONS, IntPtr.Zero, (IntPtr)updated);
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
        var rect = new NativeMethods.RECT
        {
            Left = pad,
            Top = pad,
            Right = Math.Max(pad + 1, ClientSize.Width - pad),
            Bottom = Math.Max(pad + 1, ClientSize.Height - pad),
        };
        NativeMethods.SendMessageRect(Handle, EM_SETRECT, IntPtr.Zero, ref rect);
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
            if (Clipboard.ContainsText())
            {
                SelectedText = Clipboard.GetText();
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
            Clipboard.SetText(SelectedText);
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

        string replaced = TextIndentation.ComputeIndentReplacement(
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

    #region RichEditメッセージ

    const int EM_SETRECT = 0x00B3;

    // 言語オプションのビット定義 (Windows SDK 10.0.19041.0 の richedit.h)
    const int IMF_AUTOFONT = 0x0002;
    const int IMF_AUTOFONTSIZEADJUST = 0x0010;
    const int IMF_DUALFONT = 0x0080;

    #endregion
}
