---
paths:
  - "src/**/*.cs"
---

# ログ運用ルール

## 出力API

運用ログは`Launcher.Infrastructure.DiagnosticLog`のレベル別APIを用いる。
`Debug`は原因調査用の詳細トレースを記録する。通常運用では用いない。
`Info`はユーザー操作・重要な状態遷移を1行で記録する。
`Warn`は想定外だが継続可能な事象を記録する。
`Error`は例外・失敗を記録する。例外オブジェクトを持つ場合は`Error(category, Exception)`を用いる。

`System.Diagnostics.Debug.WriteLine`はReleaseビルドで無効化されるため運用ログには使わない。
開発時の到達不能ケース検査（`Debug.Assert`・`Debug.Fail`）は本節の対象外とし、そのまま使用する。

## パス・個人情報の出力禁止

ログメッセージへファイルパス・ユーザー名・ホスト名を意図的に埋め込まない。
`C:\Users\<username>\...`が含まれるパスは、拡張子だけを`Path.GetExtension`で抽出するなど
必要最小限の情報へ絞るか、出力自体を取りやめる。
例外オブジェクトの`Message`にパスが含まれる場合の許容範囲は生成主体で判断する。
OS由来のメッセージ（`Win32Exception`を引数なしまたは`(int errorCode)`のみで構築し、
`.NET`ランタイムが`errorCode`から自動生成するメッセージ）はそのまま出力してよい。
プロジェクト側が構築したメッセージのうち、ログ到達経路が存在するものにはパスを含めない。
対象は`throw new Exception($"failed for {path}")`のような文字列引数付きコンストラクタ全般とする。
`Win32Exception`の`(int errorCode, string message)`コンストラクタ第2引数も対象に含める。
ログ到達経路は、当該例外がキャッチされて`DiagnosticLog`へ`ex`もしくは`ex.Message`が渡る経路、
および未捕捉のまま`Program.cs`の`UnhandledException`ハンドラへ届く経路を指す。
既存の`Message`にパスが含まれ、かつログ到達経路がある例外の是正手段は次の2択とする。

- 例外構築側の第2引数からパスを除去する
- キャッチ側で`Message`を除外し例外型名（`e.GetType().Name`）のみ記録する
新規追加の例外は前者（構築側でパスを含めない）を既定とする。

## カテゴリー命名

カテゴリーは`<Layer>.<Action>`形式で命名する。
`Layer`はサブシステム識別（`Button`・`Tab`・`Command`・`Scheduler`・`Notification`・`Hook`・`Popup`など）。
`Action`は動作名（`Execute`・`Edit`・`Delete`・`Show`・`Save`など）。

## 実装仕様

`DiagnosticLog`の実装仕様は以下の通り。

- ログファイル配置は`Application.ExecutablePath`親ディレクトリ配下の`logs/`とする
- 書き込みは`FileOptions.WriteThrough`＋`Flush(flushToDisk: true)`で即時ディスク反映する
- 1分あたりの書き込み行数を制限しログ肥大化を防ぐ
- 保持期間は日付ローテーションで7日
