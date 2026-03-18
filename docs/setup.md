# 開発環境セットアップガイド

## 前提条件

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio Code](https://code.visualstudio.com/)

## 推奨 VSCode 拡張機能

プロジェクトを開くと `.vscode/extensions.json` により自動で推奨されます。

- **C#** (`ms-dotnettools.csharp`) — IntelliSense、デバッグ等
- **C# Dev Kit** (`ms-dotnettools.csdevkit`) — ソリューションエクスプローラー等

## ビルド

```bash
# ソリューション全体をビルド
dotnet build らんちゃ.sln

# Release ビルド
dotnet build らんちゃ.sln -c Release
```

VSCode では `Ctrl+Shift+B` でデフォルトのビルドタスクが実行されます。

## デバッグ

1. VSCode で `F5` を押すとデバッグ起動します
2. `.vscode/launch.json` に起動設定が定義されています

## 実行

```bash
dotnet run --project らんちゃ/らんちゃ.csproj
```

## プロジェクト構成

```
らんちゃ.sln          ソリューションファイル
├── らんちゃ/          メインプロジェクト (WinForms アプリケーション)
│   └── らんちゃ.csproj
├── Toolkit/           共通ライブラリ
│   └── Toolkit.csproj
├── .vscode/           VSCode 設定
└── docs/              ドキュメント
```
