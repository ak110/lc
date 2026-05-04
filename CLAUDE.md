# CLAUDE.md: lc

Windows用アプリケーションランチャー（C#/.NET WinForms）。コマンド型・ボタン型・スケジューラーを統合する単一ユーザー向けGUIアプリ。
AI向けの開発知識はCLAUDE.mdおよびClaude Codeが参照する規約ファイル群に集約し、利用者・外部開発者向け情報は `docs/` 配下を参照する。

## 開発手順

`dotnet`・`node`・`pnpm` などはすべてmise経由で実行する（システムにインストールされたものは使わない）。
普段使うのは `mise run format`（フォーマット+軽量lint・自動修正あり）と `mise run test`（全チェック）の2つ。
`git commit` 時のpre-commitフックが `mise run test` を自動実行する。
初回セットアップは `mise install && mise run setup`。
デバッグ実行はVSCodeで `F5`（`.vscode/launch.json` に起動設定を定義している）。
Analyzerルールを新規導入する際は `.editorconfig` で `none` に抑制してから修正し、`warning` へ昇格する。
.NET SDKの更新は `mise upgrade dotnet`。
リリースは `gh workflow run release.yaml --field="bump=PATCH"` で実行する（PATCH/MINOR/MAJOR）。

- コミット前の検証方法: `uvx pyfltr run-for-agent`
  - ドキュメントなどのみの変更の場合は省略可（pre-commitで実行されるため）
  - 修正後の再実行時は、対象ファイルや対象ツールを必要に応じて限定して実行する（最終検証はCIに委ねる前提）
    - 例: `uvx pyfltr run-for-agent --commands=dotnet-build,dotnet-test path/to/file`

## アーキテクチャの参照先

モジュール構成・主要な設計パターン・フック管理・環境変数の自動リロードなどの
アーキテクチャ概要は[docs/development/architecture.md](docs/development/architecture.md)を参照する。

## 実装上の不変条件・コーディング規約

トピック別に規約ファイルとして整理している。実装時に参照されるトピックは以下の通り。

- スレッディング（`.claude/rules/threading.md`）:
  STAスレッド制約、スレッドモデル一覧、アイコンローダー並行度
- Win32相互運用（`.claude/rules/win32-interop.md`）:
  フックコールバック制約、モーダルダイアログのTopMost伝播
- 永続化（`.claude/rules/persistence.md`）:
  cfg/dat分離、XMLシリアライザ制約、ReplaceEnvList非対称性
- 通知ダイアログ（`.claude/rules/notification-dialog.md`）:
  非同期通知の追跡パターン、owner選定、フォーカス復元

## サブエージェント・スキル連携

`.claude/agents/winforms-sta-reviewer.md` に、STAスレッド・Shell API・Win32フック・
ConfigStore派生クラスの変更をレビューする専用エージェントがある。
該当箇所を変更した場合は呼び出しを検討する。

## 注意点

Linux環境での検証は限定的。
`mise.toml` の `dotnet-root` テンプレートはWindowsの `LOCALAPPDATA` を参照するため、
Linuxではpre-commitフックがテンプレート展開エラーで失敗する。
ドキュメントのみ変更時は例外として `git commit --no-verify` が許容される
（CIのWindows runnerでlintを担保）。
