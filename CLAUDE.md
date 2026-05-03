# CLAUDE.md: lc

Windows用アプリケーションランチャー（C#/.NET WinForms）。コマンド型・ボタン型・スケジューラーを統合する単一ユーザー向けGUIアプリ。
本プロジェクトは人間による開発作業がほぼ発生しない前提で運用するため、
AI向けの開発知識はCLAUDE.mdおよび自動ロード対象の規約ファイル群に集約し、利用者・外部開発者向け情報は`docs/`配下を参照する。

## 開発手順

`dotnet`・`node`・`pnpm`などはすべてmise経由で実行する（システムにインストールされたものは使わない）。
普段使うのは`mise run format`（フォーマット+軽量lint・自動修正あり）と`mise run test`（全チェック）の2つ。
`git commit`時のpre-commitフックが`mise run test`を自動実行する。
miseタスク一覧・初回セットアップ・デバッグ・Analyzerルール導入・.NET SDK更新・リリース手順は
[docs/development/development.md](docs/development/development.md)を参照する。

- コミット前の検証方法: `uvx pyfltr run-for-agent`
  - ドキュメントなどのみの変更の場合は省略可（pre-commitで実行されるため）
  - 修正後の再実行時は、対象ファイルや対象ツールを必要に応じて絞って実行する（最終検証はCIに委ねる前提）
    - 例: `uvx pyfltr run-for-agent --commands=dotnet-build,dotnet-test path/to/file`

## アーキテクチャの参照先

モジュール構成・主要な設計パターン・スレッディングモデル・フック管理・環境変数の自動リロードなどの
アーキテクチャ概要は[docs/development/architecture.md](docs/development/architecture.md)を参照する。

## 実装上の不変条件・コーディング規約

トピック別に自動ロード対象の規約ファイルとして整理している。
実装時に参照されるトピックは以下の通り。

- スレッディング: STAスレッド制約・アイコンローダー並行度
- Win32相互運用: フックコールバック・モーダルダイアログのTopMost伝播
- 永続化: cfg/dat分離・XMLシリアライザ・ReplaceEnvList

## 注意点

- Windows用プロジェクトのため、Linux環境での検証は限定的（textlint / markdownlint / prettierなど一部のlint系のみ）。
  dotnet-format / dotnet-build / dotnet-testはWindowsターゲットのためLinuxでは実行不可
- `mise.toml`の`dotnet-root`テンプレートはWindowsの`LOCALAPPDATA`を参照するため、
  Linux環境では`mise`が呼ばれるpre-commitフックがテンプレート展開エラーで失敗する。
  ドキュメントのみ変更時は例外として`git commit --no-verify`が許容される（CIのWindows runnerでlintを担保）
- WinForms Designer.csのマルチバイト文字を含むテーブル等ではmarkdownlint MD060が発生する場合がある
