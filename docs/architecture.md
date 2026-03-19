# アーキテクチャ概要

## 技術スタック

- .NET 10 / C#
- Windows Forms (WinForms)
- Win32 API (P/Invoke)
- GitHub Releases API（自動更新機能）

## モジュール構成

<!-- textlint-disable -->

```text
src/Launcher/
├── Core/           ドメインモデル・ビジネスロジック
├── Infrastructure/ 基盤ユーティリティ
├── UI/             WinFormsフォーム群
├── Win32/          Win32 API連携
└── Updater/        自動更新機能
```

<!-- textlint-enable -->

### Core

アプリケーションの中心となるデータ構造とロジック。

- **Command** — コマンド（名前、実行パス、引数、優先度等）のモデル。実行やディレクトリ展開の処理も持つ
- **CommandList** — コマンドの一覧。XMLシリアライズで永続化
- **CommandMatcher** — 入力文字列とコマンド名の前方一致・部分一致マッチング
- **Config** — アプリケーション設定。ホットキー、ウィンドウ設定、ファイラ設定、ボタンランチャー起動方法等
- **ButtonLauncherData** — ボタン型ランチャーのデータ。タブ・ボタン配置・グリッドサイズ等を管理
- **ButtonTab** / **ButtonEntry** — ボタンランチャーのタブとボタンのモデル
- **MainFormPresenter** — MainFormのUIロジック。コマンド検索・選択・実行の制御
- **ButtonLauncherPresenter** — ButtonLauncherFormのUIロジック。ボタン操作・D&D・タブ管理の制御

### Infrastructure

アプリケーション基盤となるユーティリティ群。

- **ConfigStore** — XMLシリアライズの基底クラス。Config、CommandList、Dataが継承
- **PathHelper** — パス正規化、ファイルコピー（BackupAPI利用）、強制削除等
- **AppBase** — アプリケーションの初期化・終了・再起動処理とグローバル例外ハンドリング
- **AppVersion** — アプリケーションバージョン情報とタイトル文字列の提供
- **SingleInstance** — 多重起動防止
- **ErrorReporter** — 未処理例外のダイアログ表示と再起動/終了/続行の選択

### UI

WinFormsのフォーム群。

- **DummyForm** — 常駐用の不可視フォーム。ホットキーフック、トレイアイコン、設定管理、ボタンランチャー管理を担当
- **MainForm** — コマンド型ランチャー。テキストボックスによるコマンド検索、リストビューによるコマンド一覧表示、コマンド実行
- **ButtonLauncherForm** — ボタン型ランチャー。タブ付きグリッドにコマンドをボタンとして配置。D&D対応
- **HookManager** — グローバルキーボード・マウスフックの管理。ホットキーやマウス操作のイベント通知
- **CommandManagementForm** — コマンド管理画面。コマンド一覧の表示・編集・削除
- **ConfigForm** — 設定ダイアログ
- **EditCommandForm** — コマンド編集ダイアログ

### Win32

Windows APIのP/Invoke連携。

- **Hook** — グローバルキーボードフック・マウスフック（`SetWindowsHookEx`）
- **AsyncIconLoader** — 実行ファイルからアイコンを非同期読み込み
- **ProcessLauncher** — `ShellExecuteEx`によるプロセス起動（管理者権限での昇格に対応）
- **ShellLink** — ショートカット(.lnk)ファイルの読み書き
- **FormsHelper** — ウィンドウの強制アクティブ化、閉じるボタンの無効化等

### Updater

GitHub Releases APIを使った自動更新機能。

- **GitHubUpdateClient** — GitHub APIからの最新リリース取得、更新チェック判定
- **UpdatePerformer** — ZIPダウンロード、展開、バッチスクリプト生成による自動更新実行
- **UpdateForm** — 更新通知ダイアログ
- **UpdateConfig** — 更新設定（リポジトリ情報、チェック間隔）
- **UpdateRecord** — 更新チェック記録（最終チェック日時、スキップバージョン等）

## アプリケーションフロー

```text
Program.Main()
  ├── .oldファイルのクリーンアップ（前回更新の残り）
  ├── AppBase.Initialize()（グローバル例外ハンドリング登録）
  ├── SingleInstance チェック
  ├── コマンドライン引数の処理
  │   ├── /close  → 常駐プロセスへWM_CLOSE送信
  │   ├── /restart → 常駐プロセスへ再起動メッセージ送信
  │   └── ファイルパス → コマンド登録ダイアログ表示
  └── Application.Run(DummyForm)
        ├── 設定・コマンド一覧・ボタンランチャーデータの読み込み
        ├── MainForm の生成・事前初期化・表示
        ├── ButtonLauncherForm の生成（設定で有効時）
        ├── キーボード/マウスフックの登録
        └── メッセージループ
              ├── ホットキー → MainFormの表示/非表示切り替え
              ├── MainFormでのコマンド実行
              │     ├── テキスト入力 → CommandMatcherで絞り込み
              │     ├── Enter → Command.Execute()（別スレッド）
              │     └── Shift+Enter → Command.OpenDirectory()
              └── ボタンランチャーでのコマンド実行
                    └── ボタンクリック → Command.Execute()（別スレッド）
```

## 設定ファイル

すべてXMLシリアライズで、アプリケーションと同じディレクトリに保存される。
基底クラス`ConfigStore`がシリアライズ/デシリアライズを提供。

| ファイル | 内容 |
|---------|------|
| `らんちゃ.cfg` | アプリケーション設定（Config） |
| `らんちゃ.cmd.cfg` | コマンド一覧（CommandList） |
| `らんちゃ.btns.cfg` | ボタン型ランチャーのデータ（ButtonLauncherData） |
| `らんちゃ.dat` | ウィンドウハンドル・更新チェック記録等（Data） |
