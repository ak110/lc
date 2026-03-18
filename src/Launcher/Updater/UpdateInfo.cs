#nullable disable
using System.Text;
using Launcher.Infrastructure;

namespace Launcher.Updater;

/// <summary>
/// 更新情報。これをシリアライズしたファイルをサーバー上に置く。
/// </summary>
public class UpdateInfo : ConfigStore, ICloneable {
	/// <summary>
	/// ダウンロードすべきファイルのリスト
	/// </summary>
	public List<UpdateFile> Files = new List<UpdateFile>();
	/// <summary>
	/// 書庫内のファイルとかを表すエントリのリスト
	/// </summary>
	public List<UpdateEntry> Entries = new List<UpdateEntry>();
	/// <summary>
	/// 告知メッセージのリスト
	/// </summary>
	/// <remarks>
	/// 古いのが先で新しいのが後。
	/// </remarks>
	public List<Notice> Notices = new List<Notice>();

	/// <summary>
	/// 複製の作成
	/// </summary>
	public UpdateInfo Clone() {
		UpdateInfo copy = (UpdateInfo)MemberwiseClone();
		// string以外の参照型なメンバがあればここでコピー
		copy.Files = Files.ConvertAll(f => (UpdateFile)f.Clone());
		copy.Entries = Entries.ConvertAll(e => (UpdateEntry)e.Clone());
		copy.Notices = Notices.ConvertAll(n => (Notice)n.Clone());
		return copy;
	}

	#region ICloneable メンバ

	object ICloneable.Clone() {
		return Clone();
	}

	#endregion

	/// <summary>
	/// 文字列から読み込み
	/// </summary>
	public static UpdateInfo DeserializeFromString(string data) {
		return ConfigStore.DeserializeFromString<UpdateInfo>(data);
	}
}

/// <summary>
/// ファイル１個分の情報
/// </summary>
[Serializable]
public class UpdateFile : ICloneable {
	/// <summary>
	/// ファイルへの相対パス
	/// </summary>
	public string RelativeUrl;
	/// <summary>
	/// ファイルのハッシュ
	/// </summary>
	public string Hash;
	/// <summary>
	/// ファイルサイズ
	/// </summary>
	public int FileSize;

	/// <summary>
	/// 複製の作成
	/// </summary>
	public UpdateFile Clone() {
		UpdateFile copy = (UpdateFile)MemberwiseClone();
		// string以外の参照型なメンバがあればここでコピー
		return copy;
	}

	#region ICloneable メンバ

	object ICloneable.Clone() {
		return Clone();
	}

	#endregion

	public override string ToString() {
		return RelativeUrl;
	}
}

/// <summary>
/// 解凍後のファイル１つ分の情報
/// </summary>
[Serializable]
public class UpdateEntry : ICloneable {
	/// <summary>
	/// 書庫内ファイル名 or コピー元ファイル名
	/// </summary>
	public string Source;
	/// <summary>
	/// コピー先フォルダ or コピー先ファイル名 (実行中のexeからの相対パス)
	/// </summary>
	public string Destination = ".";
	/// <summary>
	/// バックアップすべきファイルならtrue
	/// </summary>
	public bool Backup = true;
	/// <summary>
	/// 要らなくなったファイルならtrue
	/// </summary>
	public bool Obsoleted = false;

	/// <summary>
	/// 複製の作成
	/// </summary>
	public UpdateEntry Clone() {
		UpdateEntry copy = (UpdateEntry)MemberwiseClone();
		// string以外の参照型なメンバがあればここでコピー
		return copy;
	}

	#region ICloneable メンバ

	object ICloneable.Clone() {
		return Clone();
	}

	#endregion

	public override string ToString() {
		return Source;
	}
}

/// <summary>
/// 告知。
/// </summary>
[Serializable]
public class Notice : ICloneable {
	/// <summary>
	/// 告知なメッセージ。
	/// </summary>
	public string Text;

	public Notice() {
		// サンプルメッセージを設定
		StringBuilder builder = new StringBuilder();
		builder.AppendLine(DateTime.Now.ToString("[yyyy/MM/dd]"));
		builder.AppendLine("　build-XXX");
		builder.AppendLine("・変更箇所など");
		Text = builder.ToString();
	}

	/// <summary>
	/// 複製の作成
	/// </summary>
	public Notice Clone() {
		Notice copy = (Notice)MemberwiseClone();
		// string以外の参照型なメンバがあればここでコピー
		return copy;
	}

	#region ICloneable メンバ

	object ICloneable.Clone() {
		return Clone();
	}

	#endregion

	public override string ToString() {
		return Text;
	}
}
