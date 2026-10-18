using System.ComponentModel;

namespace Launcher.UI;

/// <summary>
/// エラー表示フォーム。
/// </summary>
public partial class ErrorReporterForm : Form
{
    Exception exception;

    public ErrorReporterForm(Exception e)
    {
        InitializeComponent();

        this.exception = e;
        label1.Text = e.Message;
        textBox2.Text = ErrorReporter.GetDetailMessage(exception);
        AddGuidance();

        // 閉じる。
        button4_Click(this, EventArgs.Empty);
    }

    /// <summary>
    /// 続行してよいかと報告先の案内を詳細ボタンの下へ置き、詳細欄とボタンをその分だけ下げる。
    /// </summary>
    void AddGuidance()
    {
        var guidance = new Label
        {
            Name = "guidanceLabel",
            AutoSize = true,
            MaximumSize = new Size(ClientSize.Width - label1.Left * 2, 0),
            Location = new Point(label1.Left, button4.Bottom + 8),
            Text = ErrorReporter.ContinueGuidance + "\r\n" + ErrorReporter.ReportGuidance,
            UseMnemonic = false,
        };
        Controls.Add(guidance);
        int shift = guidance.Top + guidance.PreferredSize.Height + 8 - label2.Top;
        label2.Top += shift;
        textBox2.Top += shift;
        // 下端に固定したボタンは、クライアント領域の拡大に合わせて下がる。
        ClientSize += new Size(0, shift);
    }

    /// <summary>
    /// 続行ボタンを使うかどうか
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseContinue
    {
        get { return button5.Visible; }
        set { button5.Visible = value; }
    }

    /// <summary>
    /// 詳細ボタン
    /// </summary>
    private void button4_Click(object? sender, EventArgs e)
    {
        if (button4.Text == ">> 詳細(&D)")
        {
            button4.Text = "<< 詳細(&D)";
            // 閉じる
            label2.Visible = false;
            textBox2.Visible = false;
            Size = Size - new Size(0, GetFoldingSize());
        }
        else
        {
            button4.Text = ">> 詳細(&D)";
            // 開く
            label2.Visible = true;
            textBox2.Visible = true;
            Size = Size + new Size(0, GetFoldingSize());
        }
    }

    /// <summary>
    /// 折り畳み時のサイズを算出する。
    /// </summary>
    private int GetFoldingSize()
    {
        int gridSize = 8;
        int gap = button1.Left - button2.Right;
        System.Diagnostics.Debug.Assert(gap <= gridSize * 1);
        gap = Math.Min(gap, gridSize * 1);
        return textBox2.Bottom - label2.Top + gridSize * 3 - gap;
    }
}
