using System.Runtime.InteropServices;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// 書式を持たないプレーンテキスト運用のRichTextBox派生。
/// 右端で折り返し、行番号は持たない。
/// 貼り付けは書式と画像を捨ててテキストのみ挿入する。
/// 取り消し上限を大きく設定し、実用上は無制限に取り消せる。
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

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM.WM_PASTE)
        {
            PastePlainText();
            return;
        }
        base.WndProc(ref m);
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
}
