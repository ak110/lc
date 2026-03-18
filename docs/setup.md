# 開発環境セットアップガイド

## 前提条件

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Node.js 22](https://nodejs.org/)（ドキュメントlint用）
- [pnpm](https://pnpm.io/)（Node.jsパッケージマネージャ）

[mise](https://mise.jdx.dev/)を使えば`mise install`で.NET SDKとNode.jsをまとめてインストール可能。

## 推奨VSCode拡張機能

プロジェクトを開くと`.vscode/extensions.json`により自動で推奨される。

- **C#** (`ms-dotnettools.csharp`) — IntelliSense、デバッグ等
- **C# Dev Kit** (`ms-dotnettools.csdevkit`) — ソリューションエクスプローラー等
- **markdownlint** (`DavidAnson.vscode-markdownlint`) — Markdownリンター
- **Prettier** (`esbenp.prettier-vscode`) — YAML/JSONフォーマッター

## セットアップ

```bash
# Node.js依存のインストール（ドキュメントlint用）
pnpm install

# ビルド確認
dotnet build Launcher.sln
```

## ビルド

```bash
# ソリューション全体をビルド
dotnet build Launcher.sln

# Releaseビルド
dotnet build Launcher.sln -c Release
```

VSCodeでは`Ctrl+Shift+B`でデフォルトのビルドタスクを実行する。

## テスト

```bash
dotnet test Launcher.sln
```

## デバッグ

1. VSCodeで`F5`を押すとデバッグ起動
2. `.vscode/launch.json`に起動設定を定義済み

## 実行

```bash
dotnet run --project src/Launcher/Launcher.csproj
```

## Lint

```bash
# C#フォーマットチェック
dotnet format Launcher.sln --verify-no-changes

# ドキュメントlint
pnpm run lint

# 全自動修正
dotnet format Launcher.sln
pnpm run lint:fix
```

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
