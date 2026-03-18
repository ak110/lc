using System;
using System.Collections.Generic;
using System.Text;

namespace Toolkit.Updater.Data {
	public enum ProxyType : int {
		None,
		System,
		Custom
	}
	/// <summary>
	/// 通信設定など。
	/// </summary>
	[Serializable]
	public class UpdateConfig : ICloneable {
		/// <summary>
		/// プロクシサーバー
		/// </summary>
		public ProxyType ProxyType = ProxyType.System;
		/// <summary>
		/// プロクシサーバー
		/// </summary>
		public string ProxyServer = "proxy.example.com:80";

		/// <summary>
		/// バックアップを行うのかどうか
		/// </summary>
		public bool Backup = true;
		/// <summary>
		/// バックアップ世代数
		/// </summary>
		public int BackupCount = 4;

		/// <summary>
		/// 複製の作成
		/// </summary>
		public UpdateConfig Clone() {
			UpdateConfig copy = (UpdateConfig)MemberwiseClone();
			// string以外の参照型なメンバがあればここでコピー
			return copy;
		}

		#region ICloneable メンバ

		object ICloneable.Clone() {
			return Clone();
		}

		#endregion
	}
}
