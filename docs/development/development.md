# 開発ガイド

本プロジェクトはWindows専用アプリケーションである。
`dotnet-format`・`dotnet-build`・`dotnet-test`はWindowsターゲットのため、Linux環境では実行不可。

## 開発環境の構築手順

### 必要環境

- Windows 10/11
- [mise](https://mise.jdx.dev/)（タスクランナー・ツールバージョン管理）
- [Visual Studio Code](https://code.visualstudio.com/)

### 初回セットアップ

```cmd
mise install && mise run setup
```

`dotnet` や `node`・`pnpm` などのコマンドはシステムにインストールされたものではなく、
必ずmise経由で実行する。
具体的には `mise run` タスク経由、またはmiseが管理するPATH上のバイナリを使用する。

## 開発コマンド

普段使うのは `mise run format`（フォーマット+軽量lint・自動修正あり）と `mise run test`（全チェック）の2つ。
`git commit` 時にはpre-commitフックが `mise run test` を自動実行する。
全タスクは以下のとおりである。

| コマンド          | 説明                                                         |
| ----------------- | ------------------------------------------------------------ |
| `mise run setup`  | 開発環境のセットアップ（dotnet tool restore / pnpm install） |
| `mise run format` | フォーマット + 軽量lint（開発時の手動実行用。自動修正あり）  |
| `mise run test`   | 全チェック実行（これを通過すればコミット可能）               |
| `mise run build`  | リリースビルド                                               |
| `mise run clean`  | ビルド成果物の削除                                           |
| `mise run update` | 依存パッケージの更新                                         |
| `mise run docs`   | ドキュメントのローカルプレビュー（VitePress dev server）     |

VSCodeでは `Ctrl+Shift+B` でデフォルトのビルドタスク（`mise run build`）を実行できる。
デバッグ実行はVSCodeで `F5` を押す（`.vscode/launch.json` に起動設定を定義している）。

Linux環境ではドキュメントのlintのみ実行可能（`uvx pyfltr run-for-agent docs/ README.md CLAUDE.md`）。
全チェック（`mise run test`）はWindowsのみで実行する。

## サプライチェーン攻撃対策

GitHub Actionsのワークフローは`pinact`でハッシュピン留めして実行している
（`mise run update`でハッシュピン更新が可能）。

NuGetパッケージ・GitHub Actions・npmパッケージはdependabot（`.github/dependabot.yaml`）で
週次自動更新している（cooldown: 1日で公開直後のバージョンを除外）。

NuGetパッケージの脆弱性は`dotnet list package --vulnerable`で手動確認できる。

## Analyzerルールの導入

新しいAnalyzerルールを導入する際は、まず `.editorconfig` で `none` に抑制し、
修正完了後に `warning` へ昇格するアプローチが安全である。
`TreatWarningsAsErrors=true` 環境では `suggestion` もビルドに表れないため、
`dotnet format --diagnostics` で対象箇所を列挙する。

## .NET SDKの更新

```cmd
mise upgrade dotnet
```

## ドキュメントサイト運用

ドキュメントは [VitePress](https://vitepress.dev/) で構築し、GitHub Pagesでホストしている。

- ローカルプレビュー: `mise run docs`
- 自動デプロイ: masterブランチへのpush時に `Docs` ワークフローが自動実行される（`docs/` 以下または `package.json` の変更時のみ）

## リリース手順

GitHub Actionsの `Release` ワークフローを手動実行してリリースする。

```cmd
rem リリース実行 (いずれか1つ)
gh workflow run release.yaml --field="bump=PATCH"
gh workflow run release.yaml --field="bump=MINOR"
gh workflow run release.yaml --field="bump=MAJOR"

rem ワークフロー完了を待ち、バージョンバンプコミットを取り込む
for /f "usebackq" %i in (`gh run list --workflow=release.yaml -L1 --json=databaseId -q ".[0].databaseId"`) do gh run watch %i && git pull
```

## 環境制限

- dotnet-format・dotnet-build・dotnet-testはWindowsターゲットのためLinuxでは実行不可
- `mise.toml` の `dotnet-root` テンプレートはWindowsの `LOCALAPPDATA` を参照するため、
  Linux環境では `mise` が呼ばれるpre-commitフックがテンプレート展開エラーで失敗する。
  ドキュメントのみ変更時は例外として `git commit --no-verify` が許容される（CIのWindows runnerでlintを担保）
- WinForms Designer.csのマルチバイト文字を含むテーブル等ではmarkdownlint MD060が発生する場合がある
