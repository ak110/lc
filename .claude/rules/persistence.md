# 設定永続化のルール

## 設定ファイル分離（cfg/dat）

`*.cfg`（設定）と`*.dat`（ランタイムデータ）の分離を徹底する。
頻繁に更新されるデータは`*.dat`へ置き、両者を混在させない。
ConfigStoreは原子的なファイル保存（一時ファイルに書き込み後`File.Move`で置換）を提供する。

## XMLシリアライザの初期化子禁止

XMLシリアライズ対象プロパティのコレクションに初期化子（`= new List<...> { ... }`）を付けない。
`XmlSerializer`はデシリアライズ時に既存インスタンスへAddするため、
初期化子の値とデシリアライズ結果が重複する。

## ReplaceEnvListの排他は静的

`ReplaceEnvList`は呼び出しごとに新規インスタンスが作られるため、ロックは`static`で保持する。
これにより`CommandLauncherForm.ApplyConfig`の背景スレッドと環境変数変更の背景スレッドが、
同じ`Command`や`SchedulerTask`を同時に書き換える事故を防いでいる。

## ReplaceEnvListの片方向圧縮

`ReplaceEnvList`は値→`%VAR%`形式への片方向圧縮で元の生文字列を保持しないため、以下の非対称性がある。

- 値変更（`JAVA_HOME`のパス差し替え等）: 表示は変わらないが、`Environment.ExpandEnvironmentVariables`が新値を使うため、
  子プロセスは新値で起動する
- 変数追加: 新規に置換可能となったコマンドは`%VAR%`形式に圧縮される
- 変数削除: 一度`%VAR%`形式で保存されたコマンドは復元不能（再起動しても同じ）
