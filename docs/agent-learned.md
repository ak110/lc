# Lessons Learned

- ビルド警告は放置すると蓄積する。`TreatWarningsAsErrors=true` で機械的に防止し、AGENTS.mdのルールで人的にも徹底する。
- Writeツールで新規ファイルを作成すると行末がLFになることがある。コミット前に必ず `mise run format` を実行して行末マーカー(CRLF)を含むフォーマットを統一する。
- 新しいAnalyzerルールを導入する際は `.editorconfig` で `none` に抑制し、修正完了後に `warning` へ昇格するアプローチが安全。`TreatWarningsAsErrors=true` 環境では `suggestion` もビルドに表れないため、`dotnet format --diagnostics` で対象箇所を列挙する。
- PathHelperはパス文字列操作のみ、FileHelperはファイル・ディレクトリ操作。用途に応じて使い分ける。
- アイコン読み込み完了時のInvalidateは `btn.Parent?.Invalidate(true)` で親パネル全体を対象にすること。`btn.Invalidate()` では非選択タブのボタンが再描画されない。
- コードを書いた後は **`mise run format`**（format + build + lint）を実行する
  - コミット前は **`mise run test`**（format + build + lint + test）を実行する
  - Lintエラー・テスト失敗・コンパイル時警告などは、その時実施中の作業に無関係のものであっても一緒に対応する
  - **ビルド警告ゼロを維持する**
  - 特にファイル新規作成時は行末マーカー(CRLF)が崩れやすいため、formatで統一される
