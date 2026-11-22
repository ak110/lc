# アーキテクチャ

## プロジェクト構成

```text
src/
├── Launcher.Core/  WinFormsを参照しないクラスライブラリ（net10.0）
├── Launcher/       Coreライブラリを参照するWindowsアプリ
└── Launcher.Tests/ アプリとCoreの自動テスト
```

`Launcher/`配下のソースの一部（`Core/`と`Infrastructure/`の一部）は`Launcher.Core.csproj`がコンパイルする。
Coreへ置いたソースがWinForms・Win32・UIを参照するとビルドが失敗し、P/Invoke宣言の所属は`CoreArchitectureTests`が検出する。
発行対象は`Launcher.csproj`であり、Coreの参照アセンブリも単一exeへ含める。

各機能の構成と設計はソースコードとテストを正とする。
コードから読めない実装上の不変条件は、`AGENTS.md`が示す規約ファイルとスキルに置く。
