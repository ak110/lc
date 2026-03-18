using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Toolkit.Updater {
	/// <summary>
	/// ネットワーク更新なフォーム
	/// </summary>
	public partial class UpdateForm : Form {

		Thread thread;
		object lockObject = new object();

		int state = 0; // キャンセル時-1, 完了時1。

		string url;
		Data.UpdateConfig config;
		Data.UpdateRecord record;
		UpdateClient client;

		public UpdateForm(string url, Data.UpdateConfig config, Data.UpdateRecord record) {
			InitializeComponent();

			this.url = url;
			this.config = config;
			this.record = record;

			SetNoticeText(record.OldNotices);

			client = new UpdateClient(this.config, this.record);
			client.BeginDownload += new EventHandler<DownloadEventArgs>(client_BeginDownload);
			client.CompleteDownload += new EventHandler<DownloadEventArgs>(client_CompleteDownload);
			client.DownloadProgress += new EventHandler<DownloadProgressEventArgs>(client_DownloadProgress);
			client.NoUpdateExists += new EventHandler(client_NoUpdateExists);
			client.BeginDownloadUpdates += new EventHandler<DownloadUpdatesEventArgs>(client_BeginDownloadUpdates);
			client.CompleteDownloadUpdates += new EventHandler<DownloadUpdatesEventArgs>(client_CompleteDownloadUpdates);

			thread = new Thread(new ThreadStart(GetInfoThread));
			thread.Start();
		}

		/// <summary>
		/// 更新情報
		/// </summary>
		public Data.UpdateRecord UpdateRecord {
			get { return record; }
		}

		/// <summary>
		/// 更新がキャンセルされたらtrue。
		/// 外から呼び出す必要無いかもしれんけど一応public。
		/// </summary>
		public bool Canceled {
			get { return state == -1; }
		}

		/// <summary>
		/// 更新が完了してたらtrue。
		/// このときは速やかに再起動するべし。
		/// </summary>
		public bool Finished {
			get { return state == 1; }
		}

		#region ダウンロードの進捗状況イベントなど。

		int receivedTotalBytes = 0;
		int totalSize = 0; // ←これがゼロならInfoのダウンロードと見なす

		void client_BeginDownload(object sender, DownloadEventArgs e) {
			if (Canceled) {
				throw new UpdateCancelException();
			}
			if (totalSize == 0) {
				Invoke(new MethodInvoker(delegate() {
					progressBar1.Minimum = 0;
					progressBar1.Value = 0;
					progressBar1.Maximum = e.ContentLength;
					progressBar2.Minimum = 0;
					progressBar2.Value = 0;
					progressBar2.Maximum = e.ContentLength;
				}));
			} else {
				Invoke(new MethodInvoker(delegate() {
					progressBar1.Minimum = 0;
					progressBar1.Value = 0;
					progressBar1.Maximum = e.ContentLength;
				}));
			}
		}

		void client_DownloadProgress(object sender, DownloadProgressEventArgs e) {
			if (Canceled) {
				throw new UpdateCancelException();
			}
			Invoke(new MethodInvoker(delegate() {
				label1.Text = string.Format("{0} ({1}/{2})", e.RelativeUrl, e.BytesReceived, e.TotalBytesToReceive);
				progressBar1.Value = e.BytesReceived;
				progressBar2.Value = e.BytesReceived + receivedTotalBytes;
			}));
		}

		void client_CompleteDownload(object sender, DownloadEventArgs e) {
			if (Canceled) {
				throw new UpdateCancelException();
			}
			if (totalSize != 0) {
				receivedTotalBytes += e.ContentLength;
			}
		}

		void client_BeginDownloadUpdates(object sender, DownloadUpdatesEventArgs e) {
			if (Canceled) {
				throw new UpdateCancelException();
			}

			receivedTotalBytes = 0;
			totalSize = e.TotalFileSize;

			Invoke(new MethodInvoker(delegate() {
				progressBar1.Minimum = 0;
				progressBar1.Value = 0;
				progressBar2.Minimum = 0;
				progressBar2.Value = 0;
				progressBar2.Maximum = totalSize;
			}));
		}

		void client_CompleteDownloadUpdates(object sender, DownloadUpdatesEventArgs e) {
			if (Canceled) {
				throw new UpdateCancelException();
			}
			totalSize = 0;
		}


		#endregion
			
		/// <summary>
		/// UpdateInfo取得処理
		/// </summary>
		void GetInfoThread() {
			try {
				Info.UpdateInfo info = client.GetUpdateInfo(url);

				if (Canceled) throw new UpdateCancelException();
				button1.Invoke(new MethodInvoker(delegate() {
					if (Canceled) return;
					SetNoticeText(info.Notices);
					button1.Enabled = true;
				}));
			} catch (UpdateCancelException) {
				// キャンセルされた。
			} catch (System.Net.WebException e) {
				if (Canceled) return;
				Invoke(new MethodInvoker(delegate() {
					state = -1; // canceled
					MessageBox.Show(this, e.Message, "通信エラー");
					Close();
				}));
			}
		}

		/// <summary>
		/// Noticeな文字列をTextBoxへ。
		/// </summary>
		private void SetNoticeText(IList<Info.Notice> notices) {
			textBox1.Text = BuildNoticeText(notices);
			textBox1.Select(0, 0);
		}

		/// <summary>
		/// Noticeな文字列を作成
		/// </summary>
		private static string BuildNoticeText(IList<Info.Notice> notices) {
			StringBuilder builder = new StringBuilder();
			//foreach (Info.Notice n in notices) {
			for (int i = 0; i < notices.Count; i++) {
				Info.Notice notice = notices[notices.Count - i - 1]; // 逆順に表示する(新しいのが上側)
				builder.AppendLine(notice.Text.TrimEnd()
					.Replace(Environment.NewLine, "\n")
					.Replace("\n", Environment.NewLine));
				builder.AppendLine();
			}
			if (builder.Length <= 0) {
				return "情報の取得中...";
			}
			return builder.ToString();
		}

		/// <summary>
		/// 更新が存在しなかった
		/// </summary>
		void client_NoUpdateExists(object sender, EventArgs e) {
			Invoke(new MethodInvoker(delegate() {
				state = -1; // canceled
				MessageBox.Show(this, "現在利用できる更新はありませんでした。", "ネットワーク更新");
				Close();
			}));
		}

		/// <summary>
		/// 更新ボタン
		/// </summary>
		private void button1_Click(object sender, EventArgs e) {
			thread.Join();
			button1.Enabled = false;
			thread = new Thread(new ThreadStart(DoUpdateThread));
			thread.Start();
		}

		/// <summary>
		/// 更新処理なスレッド
		/// </summary>
		void DoUpdateThread() {
			try {
				client.DownloadUpdates();

				if (Canceled) throw new UpdateCancelException();
				Invoke(new MethodInvoker(delegate() {
					if (Canceled) return;
					if (MessageBox.Show(this, "更新処理を行います", "確認",
						MessageBoxButtons.OKCancel) == DialogResult.OK) {
						buttonCancel.Enabled = false;

						state = 1; // finished
						client.DoUpdate();
						Close();
					} else {
						// キャンセル時は閉じてしまう
						Close();
					}
				}));
			} catch (UpdateCancelException) {
				// キャンセルされた。
			} catch (System.Net.WebException e) {
				if (Canceled) return;
				Invoke(new MethodInvoker(delegate() {
					state = -1; // canceled
					MessageBox.Show(this, e.Message, "通信エラー");
					Close();
				}));
			}
		}

		/// <summary>
		/// 閉じるときの処理。
		/// </summary>
		private void UpdateForm_FormClosing(object sender, FormClosingEventArgs e) {
			if (state != 1) {
				state = -1; // canceled
				//workerThread.Join();
			} else {
				System.Diagnostics.Debug.Assert(state == 1);
				DialogResult = DialogResult.OK;
			}
		}

		[Serializable]
		class UpdateCancelException : ApplicationException { }
	}
}