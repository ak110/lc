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

すべてXMLシリアライズで、アプリケーションと同じディレクトリに保存される。
基底クラス `ConfigStore` がシリアライズ/デシリアライズを提供する。

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

`*.cfg`の5種は各型の`Load()`（内部は`ConfigFile<T>.Load`）で読み、結果を`ConfigLoadStatus`の成功・初回の不在・失敗に分ける。
読込例外を空の新規データへ変換して返す実装を置かない。
空データを保存すると、読めなかった原本を利用者のデータごと上書きするためである。

- 本体とバックアップがどちらも無い場合だけ初回として扱い、保存を許す
- 本体を読めない場合と、本体が無くバックアップだけある場合は失敗とし、`ConfigFileState`がそのファイルへの保存を止める。
  保存は`ConfigSaveBlockedException`（`IOException`の派生）で失敗する
- 保存停止の判定は`ConfigStore.SerializeToFile`の中で行う。呼出元ごとに判定を書かない
- バックアップは`<ファイル名>.bak`の1世代とし、正常な読込の直後と、保存で本体を置換する直前に更新する。
  直前の本体が最後の正常な読込・保存から外部で変わっていて読めない場合は、`.bak`を更新せず`<ファイル名>.broken-<日時>`へ保全する。
  `.bak`の作成と保全に失敗したら本体を置換しない
- 復元（`ConfigFile<T>.RestoreFromBackup`）は`.bak`を読めることを確かめ、本体を`.broken-<日時>`へ移してから置換し、保存を再開する
- 旧形式（`キー = 値`の行）として受理するのは、XMLとして読めずXMLの開始で始まらない内容に限る。破損したXMLを空の旧形式として成功扱いしない
- `らんちゃ.dat`は`ConfigFile<T>`の対象外とし、従来どおり保存停止もバックアップも行わない

## XMLシリアライザの初期化子禁止

XMLシリアライズ対象プロパティのコレクションに値付き初期化子（`= new List<...> { 要素 }`）を付けない。
`XmlSerializer`はデシリアライズ時に既存インスタンスへAddするため、
初期化子の値とデシリアライズ結果が重複する。
空初期化子（`= new List<...>()`）は禁止対象ではなく、むしろ付与する。
`XmlSerializer`は既存インスタンスへAddする方式のため、nullだとデシリアライズ時にAddが失敗する。
`ButtonLauncherData`・`MemoData`はこの空初期化子で正しく往復する。

## ReplaceEnvListの排他は静的

`ReplaceEnvList`は呼び出しごとに新規インスタンスが作成されるため、ロックは`static`で保持する。
このロックがあるため、`CommandLauncherForm.ApplyConfig`の背景スレッドと環境変数変更の背景スレッドは、
同じ`Command`や`SchedulerTask`を同時には書き換えない。

## ReplaceEnvListの片方向圧縮

`ReplaceEnvList`は値→`%VAR%`形式への片方向圧縮で元の生文字列を保持しないため、以下の非対称性がある。

- 値変更（`JAVA_HOME`のパス差し替え等）: 表示は変わらないが、`Environment.ExpandEnvironmentVariables`が新値を使うため、
  子プロセスは新値で起動する
- 変数追加: 新規に置換可能となったコマンドは`%VAR%`形式に圧縮される
- 変数削除: 一度`%VAR%`形式で保存されたコマンドは復元不能（再起動しても同じ）
