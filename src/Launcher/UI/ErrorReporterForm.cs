using System.ComponentModel;
using Launcher.Infrastructure;

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

        // 閉じる。
        button4_Click(this, EventArgs.Empty);
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
    /// 畳むサイズを適当に算出
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
