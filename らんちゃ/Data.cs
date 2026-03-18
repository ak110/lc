using System;
using System.Collections.Generic;
using System.Text;

namespace らんちゃ {
	public class Data : Toolkit.Serializable {
		public long WindowHandle = 0;

		public Toolkit.Updater.Data.UpdateRecord UpdateRecord = new Toolkit.Updater.Data.UpdateRecord();

		#region Serialize/Deserialize

		/// <summary>
		/// 書き込み
		/// </summary>
		public void Serialize() {
			Serialize(".dat");
		}

		/// <summary>
		/// 読み込み
		/// </summary>
		public static Data Deserialize() {
			try {
				return Deserialize<Data>(".dat");
			} catch {
				return new Data();
			}
		}

		#endregion
	}
}
