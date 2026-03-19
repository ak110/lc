# 開発ガイド

## 前提条件

- Windows 10/11
- [mise](https://mise.jdx.dev/)（.NET SDK・Node.jsのバージョン管理）
- [Visual Studio Code](https://code.visualstudio.com/)
- [pnpm](https://pnpm.io/)（Node.jsパッケージマネージャ）

`mise install`で.NET SDKとNode.jsをまとめてインストール可能。
バージョンは`mise.toml`で管理される。

## 推奨VSCode拡張機能

プロジェクトを開くと`.vscode/extensions.json`により自動で推奨される。

- **C#** (`ms-dotnettools.csharp`) — IntelliSense、デバッグ等
- **C# Dev Kit** (`ms-dotnettools.csdevkit`) — ソリューションエクスプローラー等
- **markdownlint** (`DavidAnson.vscode-markdownlint`) — Markdownリンター
- **Prettier** (`esbenp.prettier-vscode`) — YAML/JSONフォーマッター

## セットアップ

```cmd
rem ツールチェインと依存パッケージのインストール
mise install && mise run setup

rem ビルド確認
mise run build
```

## .NET SDKの更新

```cmd
mise upgrade dotnet
```

## ビルド

```cmd
mise run build
```

VSCodeでは`Ctrl+Shift+B`でデフォルトのビルドタスクを実行する。

## テスト

```cmd
mise run test
```

## デバッグ

1. VSCodeで`F5`を押すとデバッグ起動
2. `.vscode/launch.json`に起動設定を定義済み

## 実行

```cmd
dotnet run --project src/Launcher/Launcher.csproj
```

## Lint

```cmd
rem チェックのみ
mise run lint

rem 全自動修正
mise run format
```

## リリース手順

GitHub Actionsの`Release`ワークフローを手動実行してリリースする。

### GitHub CLIから実行

```cmd
rem 1. リリース実行（いずれか1つ）
gh workflow run release.yml --field "bump=バグフィックス"
gh workflow run release.yml --field "bump=マイナーバージョンアップ"
gh workflow run release.yml --field "bump=メジャーバージョンアップ"

rem 2. ワークフロー完了を待ち、バージョンバンプコミットを取り込む
for /f "usebackq" %i in (`gh run list --workflow=release.yml -L1 --json databaseId -q ".[0].databaseId"`) do gh run watch %i && git pull
```

<!-- textlint-disable -->

結果の確認: <https://github.com/ak110/lc/actions>

<!-- textlint-enable -->

## プロジェクト構成

<!-- textlint-disable -->

```text
Launcher.sln                ソリューションファイル
├── src/
│   ├── Launcher/           メインプロジェクト (WinForms)
│   │   ├── Core/           コアロジック (Command, Config等)
│   │   ├── Win32/          Win32 API連携
│   │   ├── UI/             フォーム群
│   │   ├── Infrastructure/ 基盤 (シリアライズ、設定読込等)
│   │   ├── Updater/        更新機能 (GitHub Releases連携)
│   │   └── Launcher.csproj
│   └── Launcher.Tests/     テストプロジェクト (xUnit)
├── .github/workflows/      CI設定
├── .vscode/                 VSCode設定
├── docs/                    ドキュメント
├── Directory.Build.props    共通ビルド設定
└── package.json             Node.js依存 (ドキュメントlint用)
```

<!-- textlint-enable -->
