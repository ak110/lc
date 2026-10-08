---
name: persistence
description: >
  設定永続化・XMLシリアライズの不変条件。
  ConfigStore・設定ファイル（cfg/dat）・XMLシリアライザ・ReplaceEnvListを実装・修正するとき、
  または「XMLシリアライズ」「設定永続化」「ConfigStore」「cfg」「dat」「ReplaceEnvList」
  等のキーワードを含む実装を扱うときに呼び出す。
---

# 設定永続化のルール

## 設定ファイル一覧

すべてXMLシリアライズで、保存先を指定しない場合はアプリケーションと同じディレクトリに保存される。
`ApplicationHostForm`の`baseName`引数で保存先のベース名を指定でき、読込・保存・再読込・終了保存に共通して使う。
基底クラス`ConfigStore`がXML変換と原子的な置換を提供し、6種類とも`ConfigFile<T>`で読み書きする。

| ファイル            | 内容                                            |
| ------------------- | ----------------------------------------------- |
| `らんちゃ.cfg`      | アプリケーション設定 (Config)                   |
| `らんちゃ.cmd.cfg`  | コマンド一覧 (CommandList)                      |
| `らんちゃ.btns.cfg` | ボタン型ランチャーのデータ (ButtonLauncherData) |
| `らんちゃ.sch.cfg`  | スケジューラー設定 (SchedulerData)              |
| `らんちゃ.memo.cfg` | メモパッドのデータ (MemoData)                   |
| `らんちゃ.dat`      | ランタイムデータ (Data)                         |

## 設定ファイル分離（cfg/dat）

`*.cfg`（設定）と`*.dat`（ランタイムデータ）の分離を徹底する。
頻繁に更新されるデータは`*.dat`へ置き、両者を混在させない。
ConfigStoreは原子的なファイル保存（一時ファイルに書き込み後`File.Move`で置換）を提供する。

## 読込失敗時の保存停止とバックアップ

各型の`Load()`（内部は`ConfigFile<T>.Load`）で読み、結果を`ConfigLoadStatus`の成功・初回の不在・失敗に分ける。
読込例外を空の新規データへ変換して返す実装を置かない。
空データを保存すると、読めなかった原本を利用者のデータごと上書きするためである。

- 本体とバックアップがどちらも無い場合だけ初回として扱い、保存を許す
- cfgの本体を読めない場合と、本体が無くバックアップだけある場合は失敗とし、窓口が所有する`ConfigFileState`が保存を止める
- 保存は各型の`Save`から`ConfigFile<T>.Save`を呼び、成否を返すとともに、保存停止と書込失敗を区別して通知する
- 失敗通知はファイル・種類ごとに保存成功まで1回とし、呼び出し元は窓口から渡された通知だけを表示する
- 読込の案内と復元判断は`ConfigLoadInteraction.Accept`を使い、表示はUI側へ委ねる
- バックアップは`<ファイル名>.bak`の1世代とし、正常な読込の直後と、保存で本体を置換する直前に更新する。
  直前の本体が最後の正常な読込・保存から外部で変わっていて読めない場合は、`.bak`を更新せず`<ファイル名>.broken-<日時>`へ保全する。
  `.bak`の作成と保全に失敗したら本体を置換しない
- 復元（`ConfigFile<T>.RestoreFromBackup`）は`.bak`を読めることを確かめ、本体を`.broken-<日時>`へ移してから置換し、保存を再開する
- 旧形式（`キー = 値`の行）として受理するのは、XMLとして読めずXMLの開始で始まらない内容に限る。破損したXMLを空の旧形式として成功扱いしない
- `らんちゃ.dat`も共通の窓口を使うが、`ProtectOriginal`を無効にして保存停止とバックアップの対象から外す

「送る」からの登録は`CommandList.AddAndSave`が読込から保存までを排他し、成功した場合だけProgramがRELOADを送る。
`SchedulerRunCoordinator.EnsureIds`は保存の失敗後に予定実行を止め、保存の再試行に成功してから再開する。
通常終了と更新の強制終了は`ApplicationHostForm.PrepareShutdown`を呼ぶ。
タイマー停止とフック解除の後、メモと設定の遅延保存を確定し、datの`WindowHandle`を消去する。

## XMLシリアライザの初期化子禁止

XMLシリアライズ対象プロパティのコレクションに値付き初期化子（`= new List<...> { 要素 }`）を付けない。
`XmlSerializer`はデシリアライズ時に既存インスタンスへAddするため、
初期化子の値とデシリアライズ結果が重複する。
空初期化子（`= new List<...>()`）は禁止対象ではなく、むしろ付与する。
`XmlSerializer`は既存インスタンスへAddする方式のため、nullだとデシリアライズ時にAddが失敗する。
`ButtonLauncherData`・`MemoData`はこの空初期化子で正しく往復する。

## 共有データの所有と環境変数の置換

`ApplicationHostForm`が共有一覧をUIスレッドで所有する。
再読込はcmd.cfgだけを対象とし、`CommandList.ReplaceContents`で一覧実体を保持して表示を更新する。
同じ定義のCommand参照を保ち、再読込で失われた編集参照は`SaveEditedCommand`で検出する。
ボタンと予定の共有実体は再読込で差し替えない。

一括置換は`EnvironmentReplacementBatch.Capture`で文字列をUI側で取得し、`ReplaceEnvList.StartBackgroundReplace`で背景スレッドへ渡す。
背景スレッド内では`ReplaceEnvList`のインスタンスの`Calculate`が置換結果を計算する。
完了通知を`UiThreadDispatcher.SafeBeginInvoke`でUIへ戻し、`EnvironmentReplacementBatch.Apply`で所属と元の値が一致する対象だけを更新する。
計算中の追加・変更は次の計算へ送る。
背景スレッドへ共有一覧を渡して書き換えない。
共有前の単一Commandは同期の`Replace`で置換してよい。

## ReplaceEnvListの片方向圧縮

`ReplaceEnvList`は値→`%VAR%`形式への片方向圧縮で元の生文字列を保持しないため、以下の非対称性がある。

- 値変更（`JAVA_HOME`のパス差し替え等）: 表示は変わらないが、`Environment.ExpandEnvironmentVariables`が新値を使うため、
  子プロセスは新値で起動する
- 変数追加: 新規に置換可能となったコマンドは`%VAR%`形式に圧縮される
- 変数削除: 一度`%VAR%`形式で保存されたコマンドは復元不能（再起動しても同じ）
