#nullable disable

namespace Launcher.Updater;

/// <summary>
/// 更新記録を保持する為のオブジェクト。
/// これは各アプリケーションが保存し、更新の必要があるかどうかの判別などに用いる。
/// </summary>
[Serializable]
public class UpdateRecord : ICloneable {
	/// <summary>
	/// 古い告知。新しいのを取得するまで表示が無いのも寂しいので。
	/// </summary>
	public List<Notice> OldNotices = new List<Notice>();
	/// <summary>
	/// 各ファイルの情報を記録
	/// </summary>
	public List<FileRecord> FileRecords = new List<FileRecord>();

	/// <summary>
	/// 複製の作成
	/// </summary>
	public UpdateRecord Clone() {
		UpdateRecord copy = (UpdateRecord)MemberwiseClone();
		// string以外の参照型なメンバがあればここでコピー
		return copy;
	}

	#region ICloneable メンバ

	object ICloneable.Clone() {
		return Clone();
	}

	#endregion
}

/// <summary>
/// Info.File のデータを記録するためのオブジェクト。
/// </summary>
[Serializable]
public class FileRecord : ICloneable {
	/// <summary>
	/// 実行中のexeからの相対パス。
	/// </summary>
	public string RelativeUrl;

	/// <summary>
	/// 最終更新日時。
	/// </summary>
	public string Hash;

	/// <summary>
	/// 複製の作成
	/// </summary>
	public FileRecord Clone() {
		FileRecord copy = (FileRecord)MemberwiseClone();
		// string以外の参照型なメンバがあればここでコピー
		return copy;
	}

	#region ICloneable メンバ

	object ICloneable.Clone() {
		return Clone();
	}

	#endregion
}
