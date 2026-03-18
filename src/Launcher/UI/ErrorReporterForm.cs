#nullable disable
using Launcher.Infrastructure;

namespace Launcher.UI {
	/// <summary>
	/// エラー表示フォーム。
	/// </summary>
	public partial class ErrorReporterForm : Form {
		Exception exception;

		public ErrorReporterForm(Exception e) {
			InitializeComponent();

			this.exception = e;
			label1.Text = e.Message;
			textBox2.Text = ErrorReporter.Instance.GetDetailMessage(exception);

			// 閉じる。
			button4_Click(this, null);
		}

		/// <summary>
		/// 続行ボタンを使うかどうか
		/// </summary>
		public bool UseContinue {
			get { return button5.Visible; }
			set { button5.Visible = value; }
		}

		/// <summary>
		/// 送信ボタン
		/// </summary>
		private void button3_Click(object sender, EventArgs e) {
			if (MessageBox.Show(this, "エラーレポートを開発元に送信します。", "確認",
				MessageBoxButtons.OKCancel) == DialogResult.OK) {
				try {
					ErrorReporter.Instance.SendReport(exception);
				} catch (Exception ee) {
					MessageBox.Show(this, ee.Message, "エラー");
					return;
				}
				button3.Enabled = false;
				MessageBox.Show(this, "エラーレポートは正常に送信されました。" + Environment.NewLine +
					"ご協力ありがとうございました。" + Environment.NewLine +
					 Environment.NewLine +
					 "間もなく修正できるかもしれません。" + Environment.NewLine +
					"ネットワーク更新してみてください。",
					"送信完了");
			}
		}

		/// <summary>
		/// 詳細ボタン
		/// </summary>
		private void button4_Click(object sender, EventArgs e) {
			if (button4.Text == ">> 詳細(&D)") {
				button4.Text = "<< 詳細(&D)";
				// 閉じる
				label2.Visible = false;
				textBox2.Visible = false;
				Size = Size - new Size(0, GetFoldingSize());
			} else {
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
		private int GetFoldingSize() {
			int gridSize = 8;
			int gap = button1.Left - button2.Right;
			System.Diagnostics.Debug.Assert(gap <= gridSize * 1);
			gap = Math.Min(gap, gridSize * 1);
			return textBox2.Bottom - label2.Top + gridSize * 3 - gap;
		}
	}
}
