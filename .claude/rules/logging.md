---
paths:
  - "src/**/*.cs"
---

# ログ運用ルール

本書はlc固有の規約を定める。C#を含む共通の品質基準は`agent-basics:writing-standards`（`references/csharp.md`など）が定め、本書はそれに加えて適用する。

## 出力API

運用ログは`Launcher.Infrastructure.DiagnosticLog`のレベル別APIを用いる。
`Debug`は原因調査用の詳細トレースを記録する。通常運用では用いない。
`Info`はユーザー操作・重要な状態遷移を1行で記録する。
`Warn`は想定外だが継続可能な事象を記録する。
`Error`は例外・失敗を記録する。例外オブジェクトを持つ場合は`Error(category, Exception, nextAction)`を用いる。

`Warn`と`Error`は観測した事実の文面と別に、必須の引数`nextAction`でログの読み手が次に行う操作を受け取る。
書込処理は文面に続けて同じ行へ`次の操作: <nextAction>`を書く（例外版はスタックトレースより前の行に置く）。
次の操作には、続行してよいか、実行する操作（実在する画面操作・コマンド・確認対象）、実行できない場合の代替手段を書く。
対処が不要な場合も「対応不要（アイコンなしで表示を継続した）」のように、不要であることと継続した処理を書く。
同じ失敗を画面でも通知する場合は「画面の案内に従う」としてよい。

`System.Diagnostics.Debug.WriteLine`はReleaseビルドで無効化されるため運用ログには使わない。
開発時に到達不能ケースを確認する`Debug.Assert`・`Debug.Fail`は本節の対象外とし、そのまま使用する。

## パス・個人情報の出力禁止

ログメッセージへファイルパス・ユーザー名・ホスト名を意図的に埋め込まない。
`C:\Users\<username>\...`が含まれるパスは、拡張子だけを`Path.GetExtension`で抽出するなど
必要最小限の情報に限定するか、出力自体を取りやめる。

ログへ渡す文字列の可否は、表示名・種類名などの用途名ではなく、値の生成元と入力制約をたどって判定する。
固定値、数値、型名、コードが保持する固定の拡張子など、生成元が内容を制約する値は記録してよい。
コマンドの表示名は、実行したコマンドを識別するために記録してよい。
診断ログは利用者本人が読む前提とし、他人へ渡す場合の確認はエラー画面の案内（`ErrorReporter.ReportGuidance`）で促す。
表示名以外の利用者の自由入力（コマンドのファイル名・引数など）、外部から得た名前、ファイル名やパスから抽出した部分文字列
（`ConfigFileState.KindOf`の戻り値のように、ベース名にドットがあるとその一部を含む値）は記録しない。
操作や状態の識別が必要な場合は、生成元が内容を制約する値（固定の拡張子、カテゴリー、件数など）で代える。
例外オブジェクトの`Message`にパスが含まれる場合の許容範囲は生成主体で判断する。
OS由来のメッセージ（`Win32Exception`を引数なしまたは`(int errorCode)`のみで構築し、
`.NET`ランタイムが`errorCode`から自動生成するメッセージ）はそのまま出力してよい。
プロジェクト側が構築したメッセージのうち、ログへ届き得るものにはパスを含めない。
対象は`throw new Exception($"failed for {path}")`のような文字列引数付きコンストラクタ全般とする。
`Win32Exception`の`(int errorCode, string message)`コンストラクタ第2引数も対象に含める。
ログへ届き得るとは、例外がキャッチされて`DiagnosticLog`へ`ex`もしくは`ex.Message`が渡る場合、
および未捕捉のまま`Program.cs`の`UnhandledException`ハンドラへ届く場合を指す。
既存の`Message`にパスが含まれ、かつログへ届き得る例外の是正手段は次の2択とする。

- 例外構築側の第2引数からパスを除去する
- キャッチ側で`Message`を除外し例外型名（`e.GetType().Name`）のみ記録する
新規追加の例外は前者（構築側でパスを含めない）を既定とする。

## カテゴリー命名

カテゴリーは`<Layer>.<Action>`形式で命名する。
`Layer`はサブシステム識別（`Button`・`Tab`・`Command`・`Scheduler`・`Notification`・`Hook`・`Popup`など）。
`Action`は動作名（`Execute`・`Edit`・`Delete`・`Show`・`Save`など）。

## 実装仕様

`DiagnosticLog`の実装仕様は以下の通り。

- ログファイル配置は`Environment.ProcessPath`親ディレクトリ配下の`logs/`とする
- 書き込みは`FileOptions.WriteThrough`＋`Flush(flushToDisk: true)`で即時ディスク反映する
- 1分あたりの書き込み行数を制限しログ肥大化を防ぐ
- `DiagnosticLogWriter.GetLogFileName`が書き込みごとに現在のローカル日付からファイル名を決める
- 初回と日付が変わった最初の書き込みで古いログを削除する。`IsExpired`が最終更新時刻と現在時刻をUTCで比較し、7日より古いファイルだけを削除する
- 保存先と`TimeProvider`は`DiagnosticLogWriter`が保持し、静的APIは共通のインスタンスへ委ねる
