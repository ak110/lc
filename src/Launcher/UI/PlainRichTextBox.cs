using System.Runtime.InteropServices;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// 書式を持たないプレーンテキスト運用のRichTextBox派生。
/// 右端で折り返し、行番号は持たない。
/// 貼り付けは書式と画像を除去してテキストのみ挿入する。
/// コピーは選択文字列をプレーンテキストとしてクリップボードへ格納する。
/// 取り消し上限を大きく設定し、実用上は無制限に取り消せる。
/// </summary>
public sealed class PlainRichTextBox : RichTextBox
{
    // 取り消し上限。十分大きい値を設定し、実用上は無制限とする。
    const int UndoLimit = 1000000;

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
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // ハンドル生成後に取り消し上限を設定する (ハンドル再生成時にも再設定される)
        new WindowHelper(Handle).SendMessage(WM.EM_SETUNDOLIMIT, (IntPtr)UndoLimit, IntPtr.Zero);
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
}
