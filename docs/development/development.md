# 開発ガイド

## 開発環境の構築手順

### 必要環境

- Windows 10/11
- mise
- Visual Studio Code

### 初回セットアップ

```cmd
mise install && mise run setup
```

## 開発コマンド

| コマンド          | 説明                                                        |
| ----------------- | ----------------------------------------------------------- |
| `mise run setup`  | 開発環境のセットアップ                                      |
| `mise run format` | フォーマット + 軽量lint（開発時の手動実行用。自動修正あり） |
| `mise run test`   | 全チェック実行（これを通過すればコミット可能）              |
| `mise run build`  | リリースビルド                                              |
| `mise run clean`  | ビルド成果物の削除                                          |
| `mise run update` | 依存パッケージの更新                                        |
| `mise run docs`   | ドキュメントのローカルプレビュー                            |

Linux環境ではドキュメントのlintのみ実行できる（`uvx pyfltr run-for-agent docs/ README.md CLAUDE.md`）。
全チェック（`mise run test`）はWindowsのみで実行する。

## サプライチェーン攻撃対策

GitHub Actionsのワークフローは`pinact`でハッシュピン留めしている（`mise run update`で更新可能）。
NuGet・GitHub Actions・npmはdependabot（`.github/dependabot.yaml`）で週次自動更新する。

## Analyzerルールの導入

新しいAnalyzerルールを導入する際は、まず`.editorconfig`で`none`に抑制し、
修正完了後に`warning`へ昇格する。
`TreatWarningsAsErrors=true`環境では`suggestion`もビルドに表れないため、
`dotnet format --diagnostics`で対象箇所を列挙する。

## ドキュメントサイト運用

ドキュメントはGitHub Pagesでホストしている。

- ローカルプレビュー: `mise run docs`
- 自動デプロイ: masterブランチへのpush時に`Docs`ワークフローが自動実行される（`docs/`配下または`package.json`の変更時のみ）

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

- `dotnet-format`・`dotnet-build`・`dotnet-test`はWindowsターゲットのためLinuxでは実行不可
- `mise.toml`の`dotnet-root`テンプレートはWindowsの`LOCALAPPDATA`を参照するため、
  Linux環境では`mise`が呼ばれるpre-commitフックがテンプレート展開エラーで失敗する。
  ドキュメントのみ変更時は例外として`git commit --no-verify`が許容される（CIのWindows runnerでlintを担保）
- WinForms Designer.csのマルチバイト文字を含むテーブル等ではmarkdownlint MD060が発生する場合がある
