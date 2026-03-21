# 開発ガイド

## 前提条件

- Windows 10/11
- [mise](https://mise.jdx.dev/)（.NET SDK・Node.js・pnpmのバージョン管理）
- [Visual Studio Code](https://code.visualstudio.com/)

## セットアップ

```cmd
mise install && mise run setup
```

これだけで .NET SDK、Node.js、pnpmのインストールと依存パッケージのセットアップが完了する。
バージョンは `mise.toml` で一元管理される。

## miseタスク

普段使うのはこの2つだけ。

| コマンド | 内容 |
| ------- | ---- |
| `mise run format` | format + build + lint |
| `mise run test` | format + build + lint + test |

その他。

| コマンド | 説明 |
| ------- | ---- |
| `mise run run` | アプリケーション実行 |
| `mise run watch` | テスト自動実行（ファイル変更監視） |
| `mise run coverage` | テストカバレッジ計測 |
| `mise run publish` | Release ビルド + 成果物出力 |
| `mise run update` | 依存パッケージの最新化 |
| `mise run setup` | 初期セットアップ |
| `mise run clean` | ビルド成果物のクリーン |

VSCodeでは`Ctrl+Shift+B`でデフォルトのビルドタスク（build）を実行する。

## デバッグ

1. VSCodeで`F5`を押すとデバッグ起動
2. `.vscode/launch.json`に起動設定を定義済み

## .NET SDKの更新

```cmd
mise upgrade dotnet
```

## リリース手順

GitHub Actionsの`Release`ワークフローを手動実行してリリースする。

### GitHub CLIから実行

```cmd
rem 1. リリース実行（いずれか1つ）
gh workflow run release.yaml --field "bump=バグフィックス"
gh workflow run release.yaml --field "bump=マイナーバージョンアップ"
gh workflow run release.yaml --field "bump=メジャーバージョンアップ"

rem 2. ワークフロー完了を待ち、バージョンバンプコミットを取り込む
for /f "usebackq" %i in (`gh run list --workflow=release.yaml -L1 --json databaseId -q ".[0].databaseId"`) do gh run watch %i && git pull
```

<!-- textlint-disable -->

結果の確認: <https://github.com/ak110/lc/actions>

<!-- textlint-enable -->
