using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Net;

namespace Toolkit.App {
	/// <summary>
	/// �G���[�񍐏���
	/// </summary>
	public class ErrorReporter {
		/// <summary>
		/// Singleton�ȃC���X�^���X�̎擾
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
			// ���f�t�H���g�œo�^���Ă��܂��H
			//Register();
		}

		/// <summary>
		/// ���M��URL
		/// </summary>
		public string URL {
			get { return url; }
			set { url = value; }
		}

		/// <summary>
		/// �I�[�i�[�E�B���h�E
		/// </summary>
		public Control Owner {
			get { return owner; }
			set { owner = value; }
		}

		/// <summary>
		/// form��Owner�ɓo�^�B
		/// </summary>
		public void SetOwner(Control form) {
			owner = form;
			form.Disposed += new EventHandler(form_Disposed);
		}

		/// <summary>
		/// �A�v���P�[�V�����ŗL�̒ǉ����
		/// </summary>
		public Dictionary<string, string> AdditionalInfo {
			get { return additionalInfo; }
		}

		/// <summary>
		/// �A�v���P�[�V�����̍ċN�����s���B
		/// �K���������ׂ��B
		/// </summary>
		public event EventHandler RestartApplication;

		/// <summary>
		/// �A�v���P�[�V�����̏I�����s���B
		/// �K���������ׂ��B
		/// </summary>
		/// <remarks>
		/// Application.Exit()���邾���ł����C������c�B
		/// </remarks>
		public event EventHandler ExitApplication;

		/// <summary>
		/// �n���h����o�^����B
		/// </summary>
		public void Register() {
			//*
			Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
			/*/
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			//*/
		}

		/// <summary>
		/// �n���h����o�^��������B
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
		/// �g���b�v����Ȃ�������O������ƌĂяo�����C�x���g�B
		/// </summary>
		void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
			OnException(e.Exception);
		}

		/// <summary>
		/// ��O�ɑ΂��鏈��
		/// </summary>
		/// <param name="e">��O�I�u�W�F�N�g</param>
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
				case DialogResult.Abort: // �I��
					ExitApplication(this, EventArgs.Empty);
					break;
				case DialogResult.Retry: // �ċN��
					RestartApplication(this, EventArgs.Empty);
					break;
				case DialogResult.None:
				case DialogResult.Ignore: // ���s
					break;
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.Fail(ex.ToString());
				// ����ɍċA����Ɩʓ|�Ȃ̂ŁB�B
			} finally {
				localLock = false;
			}
		}

		/// <summary>
		/// ErrorReporterForm��\��
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
		/// �G���[���|�[�g�̑��M����
		/// </summary>
		/// <param name="e">��O�I�u�W�F�N�g</param>
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
				client.Proxy = WebRequest.GetSystemWebProxy(); // �߂�ǂ�����R���Ɍ��ߑł��B
				client.UploadValues(url, c);
			}
		}

		/// <summary>
		/// ��O�̏ڍ׃��b�Z�[�W��g�ݗ��ĂĕԂ�
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
				builder.AppendLine("�X�^�b�N�g���[�X:");
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
			if (be != null && be != ie && !object.Equals(be, e)) { // ���H(�L��`)
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
}
