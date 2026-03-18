#nullable disable
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Launcher.UI;

namespace Launcher.Infrastructure;

/// <summary>
/// エラー報告処理
/// </summary>
public class ErrorReporter
{
	/// <summary>
	/// Singletonなインスタンスの取得
	/// </summary>
	public static ErrorReporter Instance {
		get {
			return Singleton<ErrorReporter>.GetInstance(delegate() {
				return new ErrorReporter();
			});
		}
	}

	string url = "http://tqzh.tk:24497/site/error/";
	Dictionary<string, string> additionalInfo = new Dictionary<string, string>();
	Control owner = null;

	object lockObject = new object();
	bool localLock = false;

	private ErrorReporter() {
		// デフォルトで登録してしまう？
		//Register();
	}

	/// <summary>
	/// 送信先URL
	/// </summary>
	public string URL {
		get { return url; }
		set { url = value; }
	}

	/// <summary>
	/// オーナーウィンドウ
	/// </summary>
	public Control Owner {
		get { return owner; }
		set { owner = value; }
	}

	/// <summary>
	/// formをOwnerに登録。
	/// </summary>
	public void SetOwner(Control form) {
		owner = form;
		form.Disposed += new EventHandler(form_Disposed);
	}

	/// <summary>
	/// アプリケーション固有の追加情報
	/// </summary>
	public Dictionary<string, string> AdditionalInfo {
		get { return additionalInfo; }
	}

	/// <summary>
	/// アプリケーションの再起動を行う。
	/// 必ず実装すべし。
	/// </summary>
	public event EventHandler RestartApplication;

	/// <summary>
	/// アプリケーションの終了を行う。
	/// 必ず実装すべし。
	/// </summary>
	/// <remarks>
	/// Application.Exit()するだけでいい気がする…。
	/// </remarks>
	public event EventHandler ExitApplication;

	/// <summary>
	/// ハンドラを登録する。
	/// </summary>
	public void Register() {
		//*
		Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
		/*/
		AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
		//*/
	}

	/// <summary>
	/// ハンドラを登録解除する。
	/// </summary>
	public void UnRegister() {
		//*
		Application.ThreadException -= new ThreadExceptionEventHandler(Application_ThreadException);
		/*/
		AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
		//*/
	}

	void form_Disposed(object sender, EventArgs e) {
		owner = null;
	}

	/// <summary>
	/// トラップされなかった例外が発生すると呼び出されるイベント。
	/// </summary>
	void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
		OnException(e.Exception);
	}

	/// <summary>
	/// 例外に対する処理
	/// </summary>
	/// <param name="e">例外オブジェクト</param>
	public void OnException(Exception e) {
		lock (lockObject) {
			if (localLock) {
				//System.Diagnostics.Process.GetCurrentProcess().Kill();
				return;
			}
			localLock = true;
		}
		try {
			switch (ShowReporterForm(e)) {
			case DialogResult.Abort: // 終了
				ExitApplication(this, EventArgs.Empty);
				break;
			case DialogResult.Retry: // 再起動
				RestartApplication(this, EventArgs.Empty);
				break;
			case DialogResult.None:
			case DialogResult.Ignore: // 続行
				break;
			}
		} catch (Exception ex) {
			System.Diagnostics.Debug.Fail(ex.ToString());
			// ここに再帰すると面倒なので。。
		} finally {
			localLock = false;
		}
	}

	/// <summary>
	/// ErrorReporterFormを表示
	/// </summary>
	private DialogResult ShowReporterForm(Exception ex) {
		using (ErrorReporterForm form = new ErrorReporterForm(ex)) {
			if (owner != null) {
				if (owner.Visible) {
					form.StartPosition = FormStartPosition.CenterParent;
				}
				return form.ShowDialog(owner);
			}
			return form.ShowDialog();
		}
	}

	/// <summary>
	/// エラーレポートの送信処理
	/// </summary>
	/// <param name="e">例外オブジェクト</param>
	public void SendReport(Exception e) {
		NameValueCollection c = new NameValueCollection();
		c["Message"] = e.Message.Trim();
		c["Details"] = GetDetailMessage(e).Trim();
		c["ExceptionData"] = Serialize(e.Data).Trim();
		c["AdditionalInfo"] = Serialize(additionalInfo).Trim();
		c["ApplicationInfo"] = System.Diagnostics.Process.GetCurrentProcess()
			.MainModule.FileVersionInfo.ToString();
		c["ExeLastUpdate"] = System.IO.File.GetLastWriteTime(
			System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName).ToString("yyyy-MM-dd HH:mm:ss");
		c["StackTrace"] = e.StackTrace;

		using (WebClient client = new WebClient()) {
			client.Proxy = WebRequest.GetSystemWebProxy(); // めちゃどうでもよく決め打ち。
			client.UploadValues(url, c);
		}
	}

	/// <summary>
	/// 例外の詳細メッセージを組み立てて返す
	/// </summary>
	public string GetDetailMessage(Exception e) {
		StringBuilder builder = new StringBuilder();
		AppendExceptionString(builder, e);
		return builder.ToString();
	}

	private static void AppendExceptionString(StringBuilder builder, Exception e) {
		builder.Append(e.GetType().ToString());
		builder.AppendLine(":");
		if (e.Message != null) {
			builder.AppendLine(e.Message.TrimEnd());
			builder.AppendLine();
		}
		if (e.StackTrace != null) {
			builder.AppendLine("スタックトレース:");
			builder.AppendLine(e.StackTrace.TrimEnd());
			builder.AppendLine();
		}
		if (e.Source != null) {
			builder.AppendLine("Source:");
			builder.AppendLine(e.Source);
			builder.AppendLine();
		}
		Exception ie = e.InnerException;
		if (ie != null) {
			builder.Append("InnerException -> ");
			AppendExceptionString(builder, ie);
			builder.AppendLine(" <- InnerException");
		}
		Exception be = e.GetBaseException();
		if (be != null && be != ie && !object.Equals(be, e)) { // ？(´ω`)
			builder.Append("BaseException -> ");
			AppendExceptionString(builder, ie);
			builder.Append(" <- BaseException");
		}
	}

	private static string Serialize(IDictionary data) {
		//return Serializer<List<string>>.SerializeToString(Convert(data));
		return string.Join(Environment.NewLine, Convert(data).ToArray());
	}
	private static List<string> Convert(IDictionary data) {
		List<string> result = new List<string>();
		foreach (DictionaryEntry p in data) {
			result.Add(string.Format("{0}: {1}", p.Key.ToString().Trim(), p.Value.ToString().Trim()));
		}
		return result;
	}

}
