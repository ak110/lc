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

普段使用するのは以下の2つである。

| コマンド          | 内容                                                        |
| ----------------- | ----------------------------------------------------------- |
| `mise run format` | フォーマット + 軽量lint（開発時の手動実行用。自動修正あり） |
| `mise run test`   | 全チェック実行（これを通過すればコミット可能）              |

`git commit`時にはpre-commitフックが`mise run test`を自動実行する。

その他のタスクは以下のとおりである。

| コマンド          | 説明                             |
| ----------------- | -------------------------------- |
| `mise run setup`  | 開発環境のセットアップ           |
| `mise run build`  | リリースビルド                   |
| `mise run clean`  | ビルド成果物の削除               |
| `mise run update` | 依存パッケージの更新             |
| `mise run docs`   | ドキュメントのローカルプレビュー |

VSCodeでは`Ctrl+Shift+B`でデフォルトのビルドタスク (build) を実行できる。

## デバッグ

1. VSCodeで`F5`を押すとデバッグ起動する
2. `.vscode/launch.json`に起動設定を定義している

## Analyzerルールの導入

新しいAnalyzerルールを導入する際は、まず`.editorconfig`で`none`に抑制し、
修正完了後に`warning`へ昇格するアプローチが安全である。
`TreatWarningsAsErrors=true`環境では`suggestion`もビルドに表れないため、`dotnet format --diagnostics`で対象箇所を列挙する。

## .NET SDKの更新

```cmd
mise upgrade dotnet
```

## READMEとdocsの役割分担

本プロジェクトのドキュメントは以下の構成で配置している。

- README.md: 概要・特徴・インストール手順・ドキュメントへのリンクを網羅する「玄関」。
  README.mdだけを読めばプロジェクトの目的と使い始めるための入口が把握できる状態を保つ
- docs/guide/: 利用者向けの詳細情報（コマンド型ランチャー・ボタン型ランチャー・スケジューラーの使い方）
- docs/development/: 開発者向けの情報（セットアップ・デバッグ・リリース手順など）

README.mdとdocs側で概要・特徴・インストール手順が部分的に重複する場合があるが、
README.mdはGitHubトップとして、docs側は公開ドキュメントの入口として
それぞれ自己完結する必要があるため、この重複は許容する。

変更頻度が低いため二重管理のコストより一貫性・可読性のメリットが上回ると判断した。
変更時は、docs側で同じ情報を再掲している箇所があれば同じコミット内で合わせて更新する。

## ドキュメントサイト

ドキュメントは [VitePress](https://vitepress.dev/) で構築し、GitHub Pagesでホストしている。

- URL: <https://ak110.github.io/lc/>
- ローカルプレビュー: `mise run docs`
- 自動デプロイ: masterブランチへのpush時に`Docs`ワークフローが自動実行される（`docs/`以下または`package.json`の変更時のみ）

## コミットメッセージ（Conventional Commits）

Conventional Commits形式に従う。ただし記述の方向性があまり変わらないような軽微な修正は`chore`などにしてよい。

## リリース手順

GitHub Actionsの`Release`ワークフローを手動実行してリリースする。

```cmd
rem リリース実行 (いずれか1つ)
gh workflow run release.yaml --field "bump=PATCH"
gh workflow run release.yaml --field "bump=MINOR"
gh workflow run release.yaml --field "bump=MAJOR"

rem ワークフロー完了を待ち、バージョンバンプコミットを取り込む
for /f "usebackq" %i in (`gh run list --workflow=release.yaml -L1 --json databaseId -q ".[0].databaseId"`) do gh run watch %i && git pull
```

結果の確認: <https://github.com/ak110/lc/actions>
