using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;

namespace Toolkit.Updater {
	/// <summary>
	/// ダウンロード開始イベントの引数
	/// </summary>
	public class DownloadUpdatesEventArgs : EventArgs {
		Info.File[] files;
		public DownloadUpdatesEventArgs(Info.File[] f) {
			files = f;
		}
		/// <summary>
		/// ファイル情報のリスト
		/// </summary>
		public Info.File[] Files {
			get { return files; }
		}
		/// <summary>
		/// ファイルサイズの合計を算出するヘルパメソッド
		/// </summary>
		public int TotalFileSize {
			get {
				int n = 0;
				foreach (Info.File file in files) {
					n += file.FileSize;
				}
				return n;
			}
		}
	}

	/// <summary>
	/// ダウンロード開始時と終了時の引数
	/// </summary>
	public class DownloadEventArgs : EventArgs {
		string url;
		int size;
		public DownloadEventArgs(string u, int n) {
			url = u;
			size = n;
		}

		/// <summary>
		/// ファイルのURL
		/// </summary>
		public string RelativeUrl {
			get { return url; }
		}

		/// <summary>
		/// ファイルサイズ。取得に失敗してたら-1
		/// </summary>
		public int ContentLength {
			get { return size; }
		}
	}
	/// <summary>
	/// ダウンロード進捗状況の引数
	/// </summary>
	public class DownloadProgressEventArgs : EventArgs {
		string url;
		int received, total;
		public DownloadProgressEventArgs(string f, int r, int t) {
			url = f;
			received = r;
			total = t;
		}

		/// <summary>
		/// ファイルのURL
		/// </summary>
		public string RelativeUrl {
			get { return url; }
		}

		/// <summary>
		/// 受信済みバイト数
		/// </summary>
		public int BytesReceived {
			get { return received; }
		}

		/// <summary>
		/// 全バイト数
		/// </summary>
		public int TotalBytesToReceive {
			get { return total; }
		}
	}

	/// <summary>
	/// 通信を行うオブジェクト。
	/// </summary>
	public class UpdateClient {

		Data.UpdateConfig config;
		Data.UpdateRecord record;
		Info.UpdateInfo info = null;
		string dirURL; // url が http://example.com/hoge.cfg なら http://example.com/

		List<Info.File> downloadFiles = null;
		string tempDir;
		string batchFileName;

		public UpdateClient(Data.UpdateConfig config, Data.UpdateRecord record) {
			this.config = config;
			this.record = record;
		}

		/// <summary>
		/// ダウンロード開始なイベント。
		/// </summary>
		/// <remarks>
		/// このイベントはGetUpdateInfo()でも呼ばれる。
		/// </remarks>
		public event EventHandler<DownloadEventArgs> BeginDownload;
		/// <summary>
		/// ダウンロードの進捗状況なイベント。
		/// </summary>
		/// <remarks>
		/// このイベントはGetUpdateInfo()でも呼ばれる。
		/// </remarks>
		public event EventHandler<DownloadProgressEventArgs> DownloadProgress;
		/// <summary>
		/// ダウンロード完了なイベント。
		/// </summary>
		/// <remarks>
		/// このイベントはGetUpdateInfo()でも呼ばれる。
		/// </remarks>
		public event EventHandler<DownloadEventArgs> CompleteDownload;

		/// <summary>
		/// アップデートが存在しなかった
		/// </summary>
		public event EventHandler NoUpdateExists;

		/// <summary>
		/// ダウンロード開始なイベント。
		/// </summary>
		/// <remarks>
		/// このイベントはGetUpdateInfo()では呼ばれない。
		/// </remarks>
		public event EventHandler<DownloadUpdatesEventArgs> BeginDownloadUpdates;

		/// <summary>
		/// ダウンロード完了なイベント。
		/// </summary>
		/// <remarks>
		/// このイベントはGetUpdateInfo()では呼ばれない。
		/// </remarks>
		public event EventHandler<DownloadUpdatesEventArgs> CompleteDownloadUpdates;

		/// <summary>
		/// 更新情報の取得
		/// </summary>
		/// <param name="url">UpdateInfoなファイルの置いてあるURL</param>
		public Info.UpdateInfo GetUpdateInfo(string url) {
			if (info != null) {
				return info;
			}
			// ダウンロード。
			int urlSep = url.LastIndexOf('/') + 1;
			byte[] data = InnerDownloadInfo(url, urlSep);
			info = Info.UpdateInfo.DeserializeFromString(Encoding.UTF8.GetString(data));
			dirURL = url.Substring(0, urlSep);

			ListupDownloadFiles();

			return info;
		}

		/// <summary>
		/// 更新処理
		/// </summary>
		/// <exception cref="WebException">ダウンロード失敗時の例外</exception>
		public void DownloadUpdates() {
			System.Diagnostics.Debug.Assert(info != null);
			System.Diagnostics.Debug.Assert(downloadFiles != null);
			System.Diagnostics.Debug.Assert(0 < downloadFiles.Count);

			bool toBeContinue = false;
			tempDir = CreateTempDiectory();
			try {
				InnerDownloadUpdates();
				toBeContinue = true;
			} finally {
				if (!toBeContinue) {
					Directory.Delete(tempDir, true);
					tempDir = null; // 念のため
				}
			}
		}

		/// <summary>
		/// バックアップのrotateやバッチファイルの起動などを行う。
		/// これを呼び出したあとは、速やかに終了すべし。
		/// </summary>
		public void DoUpdate() {
			System.Diagnostics.Debug.Assert(tempDir != null);
			bool toBeContinue = false;
			try {
				// バックアップのrotate
				DoBackup();
				// recordの更新
				UpdateRecord();
				// バッチファイルの起動
				System.Diagnostics.Process.Start(batchFileName);
				toBeContinue = true;
			} finally {
				if (!toBeContinue) {
					System.Diagnostics.Debug.Fail("なんか変なとこに来た(´ω`)");
					// こんなとこ来られても困るのだが…。
					Directory.Delete(tempDir, true);
				}
			}

		}

		#region 更新処理関連

		/// <summary>
		/// 更新情報ファイルのダウンロード処理
		/// </summary>
		private byte[] InnerDownloadInfo(string url, int urlSep) {
			return DownloadData(url, url.Substring(urlSep));
		}

		/// <summary>
		/// ダウンロードすべきファイルをリストアップ
		/// </summary>
		void ListupDownloadFiles() {
			System.Diagnostics.Debug.Assert(info != null);

			// リストアップ
			downloadFiles = new List<Info.File>();
			foreach (Info.File f in info.Files) {
				Data.FileRecord r = FindFileRecord(f);
				if (r == null || f.Hash != r.Hash) {
					// エントリが存在しないか、ハッシュが異なってるならダウンロードすべき。
					downloadFiles.Add(f);
				}
			}

			// ダウンロードすべきファイルが無いならコールバック
			if (downloadFiles.Count <= 0) {
				EventHandler NoUpdateExists = this.NoUpdateExists;
				if (NoUpdateExists != null) {
					NoUpdateExists(this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// テンポラリディレクトリの作成
		/// </summary>
		private static string CreateTempDiectory() {
			string tempDir = null;
			for (int i = 0; i < 5; i++) {
				tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetRandomFileName()) + ".tmp";
				try {
					Directory.CreateDirectory(tempDir);
					break;
				} catch (IOException) {
					// ディレクトリが存在したとかのはずなのだが、
					// ヘタに永久ループされても困るので適当実装
					if (i == 4) throw;
				}
			}
			return tempDir;
		}

		/// <summary>
		/// ファイルのダウンロードとバッチファイルの生成を行う
		/// </summary>
		/// <param name="tempDir">テンポラリディレクトリ</param>
		private void InnerDownloadUpdates() {
			// ダウンロード開始イベント
			DownloadUpdatesEventArgs e = new DownloadUpdatesEventArgs(downloadFiles.ToArray());

			EventHandler<DownloadUpdatesEventArgs> BeginDownloadUpdates = this.BeginDownloadUpdates;
			if (BeginDownloadUpdates != null) {
				BeginDownloadUpdates(this, e);
			}

			// ダウンロード
			foreach (Info.File file in downloadFiles) {
				byte[] data = DownloadData(dirURL + file.RelativeUrl, file.RelativeUrl, file.FileSize);
				if (Hash.MD5(data) != file.Hash) {
					throw new WebException(file.RelativeUrl + " のダウンロードに失敗しました");
				}

				string extractPath = Path.Combine(tempDir,
					file.RelativeUrl.Replace('/', Path.DirectorySeparatorChar));
				// zipなら解凍、でなくばそのまま書き出し。
				if (Path.GetExtension(extractPath).ToLower() == ".zip") {
					Directory.CreateDirectory(extractPath);
					using (IO.ZipReader zip = new IO.ZipReader(data)) {
						foreach (IO.ZipEntry entry in zip.Entries) {
							string path = Path.Combine(extractPath,
								entry.Name.Replace('/', Path.DirectorySeparatorChar));
							if (entry.IsDirectory) {
								Directory.CreateDirectory(path);
							} else if (entry.IsFile) {
								zip.Extract(path, entry);
							}
						}
					}
				} else {
					File.WriteAllBytes(extractPath, data);
				}
			}

			// ダウンロード完了イベント
			if (CompleteDownloadUpdates != null) {
				CompleteDownloadUpdates(this, e);
			}

			// バッチファイル名を生成
			batchFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
				Path.GetRandomFileName() + ".$update$.bat");
			// 更新処理なバッチファイルの生成
			int labelCount = 0;
			StringBuilder batch = new StringBuilder();
			batch.AppendLine("setlocal ENABLEEXTENSIONS");
			foreach (Info.Entry entry in info.Entries) {
				if (entry.Obsoleted) {
					string dst = GetDestinationFileName(entry);
					batch.AppendFormat("if not exist \"{0}\" goto label{1}skip", dst, labelCount);
					batch.AppendLine();
					batch.AppendFormat(":label{0}", labelCount);
					batch.AppendLine();
					batch.AppendFormat("del /F /S /Q \"{0}\"", dst);
					batch.AppendLine();
					batch.AppendFormat("if exist \"{0}\" goto label{1}", dst, labelCount);
					batch.AppendLine();
					batch.AppendFormat(":label{0}skip", labelCount);
					batch.AppendLine();
				} else {
					string src = Path.Combine(tempDir, entry.Source.Replace('/', Path.DirectorySeparatorChar));
					string dst = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, entry.Destination);
					System.Diagnostics.Debug.Assert(File.Exists(src));
					System.Diagnostics.Debug.Assert(Directory.Exists(dst));
					batch.AppendFormat("if not exist \"{0}\" mkdir \"{0}\"", dst);
					batch.AppendLine();
					batch.AppendFormat(":label{0}", labelCount);
					batch.AppendLine();
					batch.AppendFormat("move /Y \"{0}\" \"{1}\"", src, dst);
					batch.AppendLine();
					batch.AppendFormat("if exist \"{0}\" goto label{1}", src, labelCount);
					batch.AppendLine();
				}
				labelCount++;
			}
			batch.AppendFormat("cd /D \"{0}\"", AppDomain.CurrentDomain.BaseDirectory);
			batch.AppendLine();
			batch.AppendFormat("start \"\" \"{0}\"",
				System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
			batch.AppendLine();
			//batch.AppendFormat(":label{0}", labelCount);
			//batch.AppendLine();
			batch.AppendFormat("rmdir /S /Q \"{0}\"", tempDir);
			batch.AppendLine();
			//batch.AppendFormat("if exist \"{0}\" goto label{1}", tempDir, labelCount);
			//batch.AppendLine();
			//labelCount++;
			batch.AppendFormat("del /F /S /Q \"{0}\"", batchFileName);
			batch.AppendLine();

			File.WriteAllText(batchFileName, batch.ToString(), Encoding.Default);
		}

		/// <summary>
		/// Info.Entryから対象ファイル名を作成
		/// </summary>
		private static string GetDestinationFileName(Info.Entry entry) {
			return GetDestinationFileName(AppDomain.CurrentDomain.BaseDirectory, entry);
		}

		/// <summary>
		/// Info.Entryから対象ファイル名を作成
		/// </summary>
		private static string GetDestinationFileName(string dir, Info.Entry entry) {
			return Path.Combine(Path.Combine(
				dir,
				entry.Destination),
				Path.GetFileName(entry.Source));
		}

		/// <summary>
		/// Info.Fileに対応するData.FileRecordを検索。無ければnull
		/// </summary>
		private Data.FileRecord FindFileRecord(Info.File f) {
			return record.FileRecords.Find(delegate(Data.FileRecord fr) {
				return f.RelativeUrl == fr.RelativeUrl;
			});
		}

		/// <summary>
		/// recordの更新
		/// </summary>
		private void UpdateRecord() {
			List<Data.FileRecord> fileRecords = new List<Data.FileRecord>();
			foreach (Info.File f in info.Files) {
				Data.FileRecord r = new Data.FileRecord();
				r.RelativeUrl = f.RelativeUrl;
				r.Hash = f.Hash;
				fileRecords.Add(r);
			}
			record.OldNotices = Utility.Clone(info.Notices);
			record.FileRecords = fileRecords;
		}

		/// <summary>
		/// バックアップ処理
		/// </summary>
		private void DoBackup() {
			if (!config.Backup) return;
			int backupCount = config.BackupCount;
			for (int i = backupCount - 1; ; i++) { // BackupCount以上のを全部削除
				string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
					string.Format("$backup-{0:d2}$", i));
				if (Directory.Exists(dir)) {
					Directory.Delete(dir, true);
				} else {
					break;
				}
			}
			for (int i = backupCount - 2; 0 <= i; i--) { // 0 ～ BackupCount-2までをリネーム
				string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
					string.Format("$backup-{0:d2}$", i));
				if (Directory.Exists(dir)) {
					Directory.Move(dir,
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
						string.Format("$backup-{0:d2}$", i + 1)));
				}
			}
			foreach (Info.Entry entry in info.Entries) {
				string src = GetDestinationFileName(entry);
				if (File.Exists(src)) {
					string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "$backup-00$");
					string dst = GetDestinationFileName(dir, entry);
					Directory.CreateDirectory(Path.GetDirectoryName(dst));
					File.Copy(src, dst);
				}
			}
		}

		#endregion
			
		#region 通信関連

		/// <summary>
		/// ファイルのダウンロード
		/// </summary>
		private byte[] DownloadData(string url, string relUrl) {
			return DownloadData(url, relUrl, -1);
		}

		/// <summary>
		/// ファイルのダウンロード
		/// </summary>
		/// <param name="fileSize">ファイルサイズ。分からなければ負</param>
		private byte[] DownloadData(string url, string relUrl, int fileSize) {
			using (MemoryStream memory = new MemoryStream()) {
				WebRequest request = CreateWebRequest(url);
				using (WebResponse responce = request.GetResponse())
				using (Stream stream = responce.GetResponseStream()) {
					// ファイルサイズの取得
					int fileSizeByServer = -1;
					try {
						fileSizeByServer = (int)responce.ContentLength;
					} catch (NotSupportedException) {
						// ファイルサイズ取得失敗？
					}
					if (0 <= fileSizeByServer) {
						System.Diagnostics.Debug.Assert(fileSize < 0 || fileSize == fileSizeByServer);
						fileSize = fileSizeByServer;
					}
					// イベントの準備
					EventHandler<DownloadEventArgs> BeginDownload = this.BeginDownload;
					EventHandler<DownloadProgressEventArgs> DownloadProgress = this.DownloadProgress;
					EventHandler<DownloadEventArgs> CompleteDownload = this.CompleteDownload;
					DownloadEventArgs e = new DownloadEventArgs(relUrl, fileSize);
					// ダウンロード前コールバック
					if (BeginDownload != null) {
						BeginDownload(this, e);
					}
					// ダウンロード処理
					//stream.ReadTimeout = 15 * 1000; // 15秒も待っててダメなら諦める
					byte[] buffer = new byte[0x2000];
					while (true) {
						int n = stream.Read(buffer, 0, buffer.Length);
						if (n == 0) break;
						memory.Write(buffer, 0, n);
						// ダウンロード進捗状況の通知
						if (DownloadProgress != null) {
							DownloadProgress(this, new DownloadProgressEventArgs(relUrl,
								(int)memory.Position, 0 <= fileSize ? fileSize :
								(n == buffer.Length ? 0 : (int)memory.Position))); // ←適当
						}
					}
					// ダウンロード完了時コールバック
					if (CompleteDownload != null) {
						CompleteDownload(this, e);
					}
				}
				return memory.ToArray();
			}
		}

		/// <summary>
		/// HTTP通信の準備
		/// </summary>
		private WebRequest CreateWebRequest(string url) {
			WebRequest client = WebRequest.Create(url);
			// プロクシを設定
			switch (config.ProxyType) {
			case Toolkit.Updater.Data.ProxyType.None:
				break;
			case Toolkit.Updater.Data.ProxyType.System:
				client.Proxy = WebRequest.GetSystemWebProxy();
				break;
			case Toolkit.Updater.Data.ProxyType.Custom:
				string[] proxy = config.ProxyServer.Split(':');
				if (proxy.Length == 2) {
					client.Proxy = new WebProxy(proxy[0], int.Parse(proxy[1]));
				} else {
					client.Proxy = new WebProxy(config.ProxyServer);
				}
				break;
			}
			// キャッシュポリシーを指定
			//client.CachePolicy = new System.Net.Cache.RequestCachePolicy(
			//	System.Net.Cache.RequestCacheLevel.BypassCache);
			client.CachePolicy = new System.Net.Cache.RequestCachePolicy(
				System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
			return client;
		}

		#endregion
	}
}
