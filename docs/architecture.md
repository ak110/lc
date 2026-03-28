# アーキテクチャ概要

## 技術スタック

- .NET 10 / C#
- Windows Forms (WinForms)
- Win32 API (P/Invoke)
- GitHub Releases API（自動更新機能）

## モジュール構成

```text
src/Launcher/
├── Core/           ドメインモデル・ビジネスロジック
├── Infrastructure/ 基盤ユーティリティ
├── UI/             WinFormsフォーム群
├── Win32/          Win32 API連携
└── Updater/        自動更新機能
```

### モジュール分割の設計意図

- **Core** — UIフレームワーク（WinForms）に依存しない純粋なドメインモデルとロジック。テスト容易性と関心の分離が目的
- **Infrastructure** — アプリケーション基盤。シリアライズ・パス操作・ファイル操作など、ドメインやUIに属さない横断的関心事を集約。PathHelperはパス文字列操作のみ、FileHelperはファイル・ディレクトリ操作と役割を分離している
- **UI** — WinFormsに依存するフォーム群。ロジックはPresenterに委譲し、フォーム自体は表示と入力の橋渡しに徹する
- **Win32** — P/Invoke呼び出しを隔離するモジュール。Win32 APIの複雑さ（マーシャリング、リソース管理）をアプリケーション本体から遮断する
- **Updater** — 自動更新機能。GitHub Releases APIとの通信、ZIPの展開、バッチスクリプトによる自己置換など、更新特有の処理を分離

## 主要な設計パターン

### Presenterパターン

MainFormPresenter / ButtonLauncherPresenter / SchedulerPresenterがUIロジックを担当する。WinFormsのフォームクラスはイベントハンドラとコントロール操作のみを持ち、判断ロジックはPresenterに委譲する。これにより、UIロジックをWinFormsから分離してテスト可能にしている。

### ConfigStore継承による永続化

Config、CommandList、ButtonLauncherData（Data）はすべてConfigStoreを継承し、XMLシリアライズで永続化される。ConfigStoreは原子的なファイル保存（一時ファイルに書き込み後File.Moveで置換）を提供し、保存中のクラッシュによるデータ破損を防止する。

**制約**: XMLシリアライズ対象プロパティのコレクション初期化子は変更禁止。XmlSerializerはデシリアライズ時に既存インスタンスへAddするため、初期化子で値を入れるとデシリアライズ結果と重複する。

### DummyFormによるIPCハブ

DummyFormは不可視の常駐フォームで、アプリケーション全体のハブとして機能する。WM_APPMSGによるプロセス間通信（/close、/restart等のコマンドライン引数の処理）を受け付け、子フォーム（MainForm、ButtonLauncherForm）のライフサイクルを管理する。WinFormsのメッセージループを維持するために常駐フォームが必要であり、メインウィンドウ（MainForm）は表示/非表示を繰り返すため、この役割を分離している。また、スケジューラのタイマー（30秒間隔）を管理し、スケジュール条件に合致したタスクの自動実行を制御する。

## 設定ファイル

すべてXMLシリアライズで、アプリケーションと同じディレクトリに保存される。
基底クラス`ConfigStore`がシリアライズ/デシリアライズを提供。

`*.cfg`ファイルはユーザーが設定変更したときのみ書き換わる静的な設定ファイル。`*.dat`ファイルはアプリケーション動作中に頻繁に更新されるランタイムデータ（ウィンドウハンドル、スケジューラの最終チェック時刻など）。この分離により、大容量になりうる設定ファイルの頻繁な書き込みを避けている。

| ファイル            | 内容                                             |
|---------------------|--------------------------------------------------|
| `らんちゃ.cfg`      | アプリケーション設定（Config）                   |
| `らんちゃ.cmd.cfg`  | コマンド一覧（CommandList）                      |
| `らんちゃ.btns.cfg` | ボタン型ランチャーのデータ（ButtonLauncherData） |
| `らんちゃ.sch.cfg`  | スケジューラ設定（SchedulerData）                |
| `らんちゃ.dat`      | ランタイムデータ（Data）                         |

## スレッディングモデル

| スレッド                       | 用途                                      | 備考                                    |
|--------------------------------|-------------------------------------------|-----------------------------------------|
| UIスレッド (STA)               | WinFormsメッセージループ、全UI操作        | `Application.Run(DummyForm)`            |
| コマンド実行スレッド (STA)     | `Command.Execute()`の実行                 | `MainForm.ExecuteCommand`で生成         |
| ディレクトリ展開スレッド (STA) | `Command.OpenDirectory()`の実行           | `MainForm.OpenDirectory`で生成          |
| アイコン読込スレッド (STA×8)   | `AsyncIconLoader`による非同期アイコン取得 | 固定8本STAワーカー + リトライ(最大2回)  |
| 環境変数置換スレッド           | `ReplaceEnvList`のコマンド名置換          | `MainForm.ApplyConfig`で生成            |
| スケジューラ実行スレッド (STA) | `SchedulerPresenter.ExecuteItemTasks`     | タイマーTick時に生成、アイテムごとに1本 |
| フックコールバック             | キーボード/マウスフックのイベント通知     | `BeginInvoke`でUIスレッドへディスパッチ |

### STA制約

コマンド実行・ディレクトリ展開・アイコン読込はすべてSTAスレッドで行う。ShellExecuteExやSHGetFileInfo等のShell APIはCOMのSTA（Single-Threaded Apartment）を前提としており、`Task.Run`（ThreadPool/MTA）では正常に動作しない。そのため専用のSTAスレッドを生成して実行する。

### アイコンローダーの並行度制限

AsyncIconLoaderのワーカー数は8本固定。SHGetFileInfo（Shell API）は高並行度で不安定になるため、ProcessorCount等の動的な値は使用せず固定値とする。ButtonLauncherFormのHandle作成は、アイコン非同期読み込み（BuildTabs→iconLoader.Load）より前に行うこと。Handle未作成時にIconLoadedイベントが到着すると、BeginInvokeの失敗によりアイコンは破棄される。アイコン読み込み完了時のInvalidateは`btn.Parent?.Invalidate(true)`で親パネル全体を対象にすること。`btn.Invalidate()`では非選択タブのボタンが再描画されない。

## フック管理

`HookManager`がグローバルキーボード/マウスフックの状態管理を一元的に担当する。

- **ホットキー検知**: `SetWindowsHookEx`で登録したキーボードフックのコールバックで、KEYDOWN時に仮想キーコードと修飾キーを照合。一致したらKEYUPを抑制しつつ、`BeginInvoke`でUIスレッドへShowHideメッセージを送信
- **ボタンランチャー起動**: マウスフックでボタン押下状態（`lbuttonDown`/`rbuttonDown`）を追跡し、設定されたトリガー（左右同時押し等）を検知。トリガー発動時はUPイベントを抑制して誤操作を防止
- **UP抑制フラグ**: `suppressNextLButtonUp`/`suppressNextRButtonUp`/`suppressKeyUpVK`で、トリガー発動後の不要なUPイベントをフック内で消費する。フックコールバック内でUPを消費しないと、トリガー操作の直後にボタンクリックやキー入力として誤検知される
