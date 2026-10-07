# アーキテクチャ

## モジュール構成

```text
src/
├── Launcher.Core/  WinFormsを参照しないクラスライブラリのプロジェクト
├── Launcher/       Coreライブラリを参照するWindowsアプリのプロジェクト
│   ├── Core/           ドメインモデル・判断処理（Coreライブラリへリンク）
│   ├── Infrastructure/ 保存・ログ等の共通部品とアプリの基盤
│   ├── UI/             フォーム・UI配送・例外表示・起動の調整
│   ├── Win32/          P/Invoke・Shell・Windows固有のファイル操作
│   └── Updater/        自動更新機能
└── Launcher.Tests/ アプリとCoreの自動テスト
```

### モジュール分割の設計意図

- Coreはドメインモデルと判断処理を置く。`Launcher.Core.csproj`は`net10.0`を対象とし、
  WinFormsやアプリのプロジェクトを参照しないため、UI・Win32への参照を加えるとビルドが失敗する。
  コマンドの修飾キーは中立な`InputModifiers`で受け取る
- Infrastructureのうち`ConfigFile`・`ConfigStore`・`LegacyConfigReader`・`IoFailureHandler`・
  `PathHelper`・`AppVersion`・`DiagnosticLog`はCoreライブラリでコンパイルする。
  STAの起動、単一起動の管理、ファビコンとHTTPの共有接続はアプリ側でコンパイルする。
  ソースの配置を維持しつつ、各プロジェクトのコンパイル対象を分けている
- UIはフォーム、`UiThreadDispatcher`による配送、`ErrorReporter`による例外表示、
  `AppBase`による起動と終了の処理を持つ。判断はCoreの型へ委譲する
- Win32 — P/Invoke呼び出しを隔離するモジュール。
  Win32 APIの複雑さ（マーシャリング、リソース管理）をアプリケーション本体から遮断する。
  Shell API呼び出し（プロセス起動・アイコン取得・ショートカット操作・Shellコンテキストメニュー表示等）をここへ集約する
- Windows固有のパス解決は`FileHelper`、自然順のフォルダー列挙は`FolderEntryEnumerator`、
  ショートカットからのコマンド生成は`CommandFactory`が担う。
  P/Invoke宣言の所属は`CoreArchitectureTests`で確かめ、複数箇所から使う宣言は`NativeMethods`へ集める
- Updater: 自動更新機能。GitHub Pagesの`version.json`取得、ZIPの展開、バッチスクリプトによる自己置換など、
  更新特有の処理を分離する

`SharedHttpClient`はファビコンと更新処理の接続を共有し、更新要求のUser-Agentを1か所で設定する。
各要求と応答は呼び出し側で破棄し、共有クライアントはアプリの生存中保持する。
発行対象は`Launcher.csproj`を維持し、Coreの参照アセンブリも単一exeへ含める。

## 主要な設計パターン

### Presenterパターン

`CommandLauncherPresenter`・`ButtonLauncherPresenter`・`MemoPresenter`は表示と操作の判断を担う。
メモの検索と置換は`MemoSearch`、インデントは`TextIndentation`、フック入力の状態遷移は`HookInputState`へ分ける。
予定の走査と日付・時刻の判定は`SchedulerScheduleEvaluator`へ集約し、
UI側の`SchedulerTaskRunner`が専用STA上でタスク列を実行する。
ドラッグと長押しの状態は`DragDropState`・`LongPressOperationState`がそれぞれ独立したファイルで管理する。

判断処理は公開型から直接テストし、画面の動作は通常の公開操作からテストする。
lc自身の非公開メンバーの反射呼び出しや、テストのための可視性変更は使わない。
`ConfigTests`は省略時の値と複製、`ConfigStoreTests`は往復、`SerializationTests`は旧形式の互換をテストする。

### ConfigStore継承による永続化

ConfigStoreを継承するクラスはXMLシリアライズで永続化される。
対象はConfig・CommandList・ButtonLauncherData・SchedulerData・Data・MemoDataである。
ConfigStoreは原子的なファイル保存（一時ファイルに書き込み後File.Moveで置換）を提供し、
保存中のクラッシュによるデータ破損を防止する。
6種類とも`ConfigFile<T>`で読み書きし、読込結果を成功・初回の不在・失敗に分ける。
cfgは失敗したファイルへの保存を止め、直前の正常な内容を`.bak`へ1世代残し、失敗の通知から復元できるようにする。
datは保存停止とバックアップの対象から外す。
保存は成否を返し、保存停止と書込失敗をファイル・種類ごとに成功まで1回通知する。
読込状態は各窓口が所有し、本体・バックアップ・復元の書き込みには同じ原子保存を使う。
保存先は`ApplicationHostForm`の`baseName`引数で受け取り、省略時は実行ファイルと同じ場所を使う。
不変条件は`.claude/skills/persistence/`に記載している。

### ApplicationHostFormによるIPCハブ

ApplicationHostFormは不可視の常駐フォームで、アプリケーション全体のハブとして機能する。
WM_APPMSGによるプロセス間通信（/close、/restart等のコマンドライン引数の処理）を受け付ける。
メッセージの値と送信処理は`ResidentMessages`を送受信側で共有する。
加えて、子フォーム（CommandLauncherForm、ButtonLauncherForm、MemoForm）のライフサイクルを管理する。
WinFormsのメッセージループを維持するために常駐フォームが必要であり、
CommandLauncherFormは表示/非表示を繰り返すため、この役割を分離している。
また、スケジューラーのタイマー（30秒間隔）を管理し、スケジュール条件に合致したタスクの自動実行も制御する。
予定実行の開始・保留・完了はアイテムの識別子（`SchedulerItem.Id`）ごとに`SchedulerRunCoordinator`で管理する。
`SchedulerTaskRunner.ExecuteItemTasks`はタスク列（タスク間の待機を含む）の完了時に完了通知を呼び、
ApplicationHostFormは`UiThreadDispatcher.SafeBeginInvoke`でUIスレッドへ配送して実行状態を解放する。
実行中に到来した同じアイテムの予定は1件の保留にまとめ、完了後に最新の確定設定で1回実行する。

共有データの変更はUIスレッドで行う。
RELOADはcmd.cfgだけを読み、一覧実体を維持してランチャーと管理画面を更新する。
同じ定義のCommand参照は保持し、定義変更や削除で一覧から外れた編集参照の保存は止める。
ボタンと予定の共有実体は再読込で差し替えない。
通常終了と更新終了は共通の`PrepareShutdown`でタイマーとフックを止める。
続けてメモと設定の遅延保存を確定し、datのWindowHandleを消去する。

### スケジューラータスクの種類

スケジューラーはファイル実行に加え、メッセージ表示タスクをサポートする。

| 種類       | 説明                                                         |
| ---------- | ------------------------------------------------------------ |
| Execute    | `LaunchRequestBuilder`の要求を`ShellLaunchService`で起動する |
| BalloonTip | タスクトレイのバルーン通知でメッセージを表示する (自動消去)  |
| MessageBox | `NotificationForm`をモーダル表示する (OKボタンで手動消去)    |

`SchedulerTaskExecutor`はファイルタスクの起動要求を返す。
`SchedulerTaskRunner`は専用STAでタスク列を実行し、ファイル起動を`ShellLaunchService.ExecuteOnSta`へ渡す。
BalloonTip/MessageBoxの表示はデリゲート経由でUI層 (ApplicationHostForm) に委譲する。
MessageBoxは`Invoke`（同期呼び出し）でダイアログが閉じるまで後続タスクをブロックする。
BalloonTipは`UiThreadDispatcher.SafeBeginInvoke`で非同期に実行する。

### 通知ダイアログの追跡とowner選定

実装上の不変条件は`.claude/skills/notification-dialog/`に記載している。

想定外の例外は`ErrorReporter`でログへ記録し、表示中のフォームを所有者とする`ErrorReporterForm`で通知する。
所有者は`ApplicationHostForm.GetVisibleOwner`で表示時に選ぶ。
フックからの通知はログ出力も含めてUIへ非同期に配送し、コールバック内で待機しない。

### UIへの配送とフォーム間の共通処理

UIへの非同期配送は`UiThreadDispatcher.SafeBeginInvoke`へ集約する。
配送先の破棄・ハンドル未作成・配送後のハンドル破棄では、処理の代わりに解放用のコールバックを1回呼ぶ。
ボタン・コマンド・管理一覧・フォルダーメニューは`IconReceiver`でアイコンを受け取り、採用時も破棄時も元の`Icon`を解放する。
アクティブ化の再入は`WindowHelper.ActivateForce`の内部で防ぐ。

保存位置の復元とカーソル中心の配置は`FormsHelper`で作業領域内へ補正する。
タブ名の入力は`InputDialog`、タブの当たり判定とホイール切替は`TabControlHelper`を使う。
コマンド編集と予定タスク編集は`LaunchOptionsControl`で表示形式・優先度を選び、`CommandFileBrowser`で参照ボタンの処理を共有する。
最前面の親から子ダイアログへの伝播は`FormsHelper.ShowDialogOver`が担う。

## フック管理

`HookManager`はWindowsのフックイベントを`HookInputState`への入力へ変換する。
`HookInputState`は左右の物理修飾キー、ホットキーのリピート抑止、マウスボタンの押下順序を追跡する。
自プロセスが注入したキー解放は物理状態を変えず、実際のキー解放まで修飾キーを保持する。

コールバック内で守るべき実装上の不変条件（即時return・UP抑制フラグ更新など）は
`.claude/rules/win32-interop.md`に記載している。

## 環境変数の自動リロード

`EnvironmentRefresher`（`Win32/`）がレジストリから環境変数を再読込し、現プロセスの環境ブロックを差分更新する。
`ApplicationHostForm.WndProc`が`WM_SETTINGCHANGE`（`lParam == "Environment"`）を受信する。
500msのデバウンスを経て`EnvironmentRefresher.Refresh()`を呼び、
その後UIスレッドでコマンドと予定のパス文字列を取得し、背景スレッドでは置換結果だけを計算する。
UIへ戻り、取得時と所属・値が一致する対象へ結果を適用する。
計算中に追加・変更された対象は次の処理で取り込む。
ReplaceEnvListに関する挙動上の注意は`.claude/skills/persistence/`に記載している。

マージ規則はExplorer互換（HKLM+HKCU統合、Path系のみ`;`連結、それ以外はユーザー変数優先）とする。

子プロセスへの伝搬は追加実装不要である。
全画面の起動要求は`LaunchRequestBuilder`で環境変数を展開する。
`ShellLaunchService`が専用STAから呼ぶ`ShellExecuteEx`は呼び出し元プロセスの環境ブロックを継承するため、
`Environment.SetEnvironmentVariable`で更新すれば以降の起動プロセスへ新値が反映される。
