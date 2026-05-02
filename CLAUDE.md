# CLAUDE.md: lc

本プロジェクトは人間による開発作業がほぼ発生しない前提で運用するため、
AI向けの開発知識（コーディング規約・設計判断・実装上の注意点）はCLAUDE.mdおよび`.claude/rules/`配下に集約する。
利用者向けの情報は`docs/guide/`配下、外部開発者向けのコンセプト・環境・手順・
アーキテクチャ概要は`docs/development/`配下を参照する。

## 開発コマンド

`dotnet`・`node`・`pnpm`などはすべてmise経由で実行する（システムにインストールされたものは使わない）。
普段使うのは`mise run format`（フォーマット+軽量lint・自動修正あり）と`mise run test`（全チェック）の2つ。
`git commit`時のpre-commitフックが`mise run test`を自動実行する。
miseタスク一覧・初回セットアップ・デバッグ・Analyzerルール導入・.NET SDK更新・リリース手順は
[docs/development/development.md](docs/development/development.md)を参照する。

コミット前検証には`uvx pyfltr run-for-agent`を使う。
ドキュメントのみの変更ならpre-commitに任せて省略してよい。
修正後の再実行は対象を絞ってよい（最終検証はCIに委ねる前提）。
例: `uvx pyfltr run-for-agent --commands=dotnet-build,dotnet-test path/to/file`

## アーキテクチャの参照先

モジュール構成・主要な設計パターン・スレッディングモデル・フック管理・環境変数の自動リロードなどの
アーキテクチャ概要は[docs/development/architecture.md](docs/development/architecture.md)を参照する。

## 実装上の不変条件・コーディング規約

トピック別に`.claude/rules/`配下に配置している。実装・レビュー前に該当ファイルを参照する。

- [.claude/rules/threading.md](.claude/rules/threading.md): STAスレッド制約・アイコンローダー並行度
- [.claude/rules/win32-interop.md](.claude/rules/win32-interop.md): Win32フックコールバック・モーダルダイアログのTopMost伝播
- [.claude/rules/persistence.md](.claude/rules/persistence.md): 設定永続化（cfg/dat分離・XMLシリアライザ・ReplaceEnvList）
- [.claude/rules/markdown.md](.claude/rules/markdown.md): Markdown記述上の注意
