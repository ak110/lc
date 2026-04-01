# 開発ガイド

## 必要環境

- Windows 10/11
- [mise](https://mise.jdx.dev/)（タスクランナー・ツールバージョン管理）
- [Visual Studio Code](https://code.visualstudio.com/)

## 初回セットアップ

```cmd
mise install && mise run setup
```

`dotnet`や`node`、`pnpm`などのコマンドはシステムにインストールされたものではなく、必ずmise経由で実行すること。
具体的には`mise run`タスク経由、またはmiseが管理するPATH上のバイナリを使用する。

## miseタスク

普段使うのはこの2つ。

| コマンド          | 内容                                          |
| ----------------- | --------------------------------------------- |
| `mise run format` | コード整形 + lint (testの軽量版)              |
| `mise run test`   | 全チェック (format確認 + build + test + lint) |

`git commit`時にはpre-commitフックが`mise run test`を自動実行する。

その他のタスク。

| コマンド          | 説明                             |
| ----------------- | -------------------------------- |
| `mise run setup`  | 開発環境のセットアップ           |
| `mise run build`  | リリースビルド                   |
| `mise run clean`  | ビルド成果物の削除               |
| `mise run update` | 依存パッケージの更新             |
| `mise run docs`   | ドキュメントのローカルプレビュー |

VSCodeでは`Ctrl+Shift+B`でデフォルトのビルドタスク（build）を実行できる。

## デバッグ

1. VSCodeで`F5`を押すとデバッグ起動
2. `.vscode/launch.json`に起動設定を定義済み

## Analyzerルールの導入

新しいAnalyzerルールを導入する際は、まず`.editorconfig`で`none`に抑制し、修正完了後に`warning`へ昇格するアプローチが安全。
`TreatWarningsAsErrors=true`環境では`suggestion`もビルドに表れないため、`dotnet format --diagnostics`で対象箇所を列挙する。

## .NET SDKの更新

```cmd
mise upgrade dotnet
```

## ドキュメントサイト

ドキュメントは [VitePress](https://vitepress.dev/) で構築し、GitHub Pagesでホストしている。

- URL: <https://ak110.github.io/lc/>
- ローカルプレビュー: `mise run docs`
- 自動デプロイ: masterブランチへのpush時に`Docs`ワークフローが自動実行される（`docs/`以下または`package.json`の変更時のみ）

## リリース手順

GitHub Actionsの`Release`ワークフローを手動実行してリリースする。

```cmd
rem リリース実行（いずれか1つ）
gh workflow run release.yaml --field "bump=バグフィックス"
gh workflow run release.yaml --field "bump=マイナーバージョンアップ"
gh workflow run release.yaml --field "bump=メジャーバージョンアップ"

rem ワークフロー完了を待ち、バージョンバンプコミットを取り込む
for /f "usebackq" %i in (`gh run list --workflow=release.yaml -L1 --json databaseId -q ".[0].databaseId"`) do gh run watch %i && git pull
```

結果の確認: <https://github.com/ak110/lc/actions>
