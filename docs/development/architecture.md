# アーキテクチャ

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

- Core — UIフレームワーク（WinForms）に依存しない純粋なドメインモデルとロジックを置く。
  テスト容易性と関心の分離を目的とする
- Infrastructure — アプリケーション基盤。シリアライズ・パス操作・ファイル操作など、
  ドメインやUIに属さない横断的関心事を集約する。PathHelperはパス文字列操作のみ、
  FileHelperはファイル・ディレクトリ操作と役割を分離している
- UI — WinFormsに依存するフォーム群。ロジックはPresenterに委譲し、フォーム自体は表示と入力の橋渡しに徹する
- Win32 — P/Invoke呼び出しを隔離するモジュール。
  Win32 APIの複雑さ（マーシャリング、リソース管理）をアプリケーション本体から遮断する
- Updater — 自動更新機能。GitHub Releases APIとの通信、ZIPの展開、バッチスクリプトによる自己置換など、
  更新特有の処理を分離する

## 主要な設計パターン

### Presenterパターン

MainFormPresenter / ButtonLauncherPresenter / SchedulerPresenterがUIロジックを担当する。
WinFormsのフォームクラスはイベントハンドラとコントロール操作のみを持ち、判断ロジックはPresenterに委譲する。
これにより、UIロジックをWinFormsから分離してテスト可能にしている。

### ConfigStore継承による永続化

Config、CommandList、ButtonLauncherData（Data）はすべてConfigStoreを継承し、XMLシリアライズで永続化される。
ConfigStoreは原子的なファイル保存（一時ファイルに書き込み後File.Moveで置換）を提供し、
保存中のクラッシュによるデータ破損を防止する。

制約: XMLシリアライズ対象プロパティのコレクション初期化子は変更禁止。XmlSerializerはデシリアライズ時に既存インスタンスへAddするため、初期化子で値を入れるとデシリアライズ結果と重複する。

### DummyFormによるIPCハブ

DummyFormは不可視の常駐フォームで、アプリケーション全体のハブとして機能する。
WM_APPMSGによるプロセス間通信（/close、/restart等のコマンドライン引数の処理）を受け付ける。
加えて、子フォーム（MainForm、ButtonLauncherForm）のライフサイクルを管理する。
WinFormsのメッセージループを維持するために常駐フォームが必要であり、
メインウィンドウ（MainForm）は表示/非表示を繰り返すため、この役割を分離している。
また、スケジューラーのタイマー（30秒間隔）を管理し、スケジュール条件に合致したタスクの自動実行も制御する。

### スケジューラータスクの種類

スケジューラーはファイル実行に加え、メッセージ表示タスクをサポートする。

| 種類       | 説明                                                        |
| ---------- | ----------------------------------------------------------- |
| Execute    | ShellExecuteExでプログラムを起動する                        |
| BalloonTip | タスクトレイのバルーン通知でメッセージを表示する (自動消去) |
| MessageBox | `NotificationForm`をモーダル表示する (OKボタンで手動消去)   |

Core層 (SchedulerPresenter) はUI依存を持たない。
BalloonTip/MessageBoxの表示はデリゲート経由でUI層 (DummyForm) に委譲する。
MessageBoxは`Invoke`（同期呼び出し）でダイアログが閉じるまで後続タスクをブロックする。
BalloonTipはBeginInvoke（非同期）で実行する。

### 非同期通知ダイアログの追跡

`DummyForm` は現在表示中の非同期通知 (`NotificationForm`) を `activeNotifications` リストで追跡する。
スケジューラーのMessageBoxタスクは `Invoke` 経由で `NotificationForm.ShowDialog` を呼び出し、
スケジューラー STAスレッドは `ShowDialog` が戻るまで自然にブロックされる。
`ShowDialog` のネストメッセージループ内でも `DummyForm.WndProc` は動作するため、
表示中でも `WM_APPMSG_SHOWHIDE` を受信しホットキー操作を処理できる。

ホットキー押下で呼び出される `DummyForm.ShowHide()` は、通知が追跡中のときはMainFormを
非表示化せず、通知Formを `Activate()` + `BringToFront()` で最前面化する。
またMainFormの `WindowHideNoActive` によるauto-hideも同じ条件でスキップする。
具体的には `MainForm_Deactivate` / `MainForm_Leave` で `DummyForm.HasActiveNotifications` が真のとき、処理を中止する。
通知をActivateした直後の `MainForm_Deactivate` でMainFormが再び隠れる事故を防ぐためである。

BalloonTipはOSのトレイ通知であり自動消去されるため、この追跡対象には含めない。

#### owner 選定と前景ウィンドウの復元

`DummyForm.GetVisibleOwner()` は owner 候補として MainForm / ButtonLauncherForm / `Form.ActiveForm` の順で返す。
`Form.ActiveForm` フォールバックは、MainForm が非表示で ConfigForm 等のモーダルダイアログが開いている最中に
スケジューラーが発火したケースをカバーする。この場合、NotificationForm はモーダルダイアログを owner として
`ShowDialog` するため、閉じたときは WinForms の標準挙動でモーダルダイアログへフォーカスが戻る。

一方、launcher 内に表示フォームが一切無い状態 (`HideFirst=true` での起動直後や MainForm hide 後) で
発火したときは owner が `null` になる。`Form.ShowDialog(null)` は閉じるときのフォーカス復元先を持たないため、
Windows が z-order 上の任意ウィンドウを前面化してしまう。これを避けるため、MessageBox ハンドラでは
`NotificationForm` を表示する直前に `WindowHelper.GetForegroundWindowHandle()` で前景 HWND を記録し、
`ShowDialog` 終了後に `GetVisibleOwner()` が依然として `null` のときだけ
`WindowHelper.RestoreForegroundWindow()` で記録した HWND に前景を戻す。

`RestoreForegroundWindow` は自プロセス所有の HWND には何もしない。launcher 内へのフォーカス復帰は
WinForms の標準挙動に任せる責務分担にしている。

## 設定ファイル

すべてXMLシリアライズで、アプリケーションと同じディレクトリに保存される。
基底クラス`ConfigStore`がシリアライズ/デシリアライズを提供。

`*.cfg`ファイルはユーザーが設定変更したときのみ書き換わる静的な設定ファイル。
`*.dat`ファイルはアプリケーション動作中に頻繁に更新されるランタイムデータ
（ウィンドウハンドル、スケジューラーの最終チェック時刻など）。
この分離により、大容量になりうる設定ファイルの頻繁な書き込みを避けている。

| ファイル            | 内容                                            |
| ------------------- | ----------------------------------------------- |
| `らんちゃ.cfg`      | アプリケーション設定 (Config)                   |
| `らんちゃ.cmd.cfg`  | コマンド一覧 (CommandList)                      |
| `らんちゃ.btns.cfg` | ボタン型ランチャーのデータ (ButtonLauncherData) |
| `らんちゃ.sch.cfg`  | スケジューラー設定 (SchedulerData)              |
| `らんちゃ.dat`      | ランタイムデータ (Data)                         |

## スレッディングモデル

| スレッド                         | 用途                                      | 備考                                    |
| -------------------------------- | ----------------------------------------- | --------------------------------------- |
| UIスレッド (STA)                 | WinFormsメッセージループ、全UI操作        | `Application.Run(DummyForm)`            |
| コマンド実行スレッド (STA)       | `Command.Execute()`の実行                 | `MainForm.ExecuteCommand`で生成         |
| ディレクトリ展開スレッド (STA)   | `Command.OpenDirectory()`の実行           | `MainForm.OpenDirectory`で生成          |
| アイコン読込スレッド (STAx8)     | `AsyncIconLoader`による非同期アイコン取得 | 固定8本STAワーカー + リトライ(最大2回)  |
| 環境変数置換スレッド             | `ReplaceEnvList`のコマンド名置換          | `MainForm.ApplyConfig`で生成            |
| スケジューラー実行スレッド (STA) | `SchedulerPresenter.ExecuteItemTasks`     | タイマーTick時に生成、アイテムごとに1本 |
| フックコールバック               | キーボード/マウスフックのイベント通知     | `BeginInvoke`でUIスレッドへディスパッチ |

### STA制約

コマンド実行・ディレクトリ展開・アイコン読込はすべてSTAスレッドで行う。
ShellExecuteExやSHGetFileInfo等のShell APIはCOMのSTA（Single-Threaded Apartment）を前提としている。
そのため、`Task.Run`（ThreadPool/MTA）では正常に動作しない。専用のSTAスレッドを生成して実行する必要がある。

### アイコンローダーの並行度制限

AsyncIconLoaderのワーカー数は8本固定。SHGetFileInfo（Shell API）は高並行度で不安定になるため、
ProcessorCount等の動的な値は使用せず固定値とする。
ButtonLauncherFormのHandle作成は、アイコン非同期読み込み（BuildTabs→iconLoader.Load）より前に行うこと。
Handle未作成時にIconLoadedイベントが到着すると、BeginInvokeの失敗によりアイコンは破棄される。
アイコン読み込み完了時のInvalidateは`btn.Parent?.Invalidate(true)`で親パネル全体を対象にすること。
`btn.Invalidate()`では非選択タブのボタンが再描画されない。

## フック管理

`HookManager`がグローバルキーボード/マウスフックの状態管理を一元的に担当する。

- ホットキー検知: `SetWindowsHookEx`で登録したキーボードフックのコールバックで、
  KEYDOWN時に仮想キーコードと修飾キーを照合する。一致した場合はKEYUPを抑制しつつ、
  `BeginInvoke`でUIスレッドへShowHideメッセージを送信する
- ボタンランチャー起動: マウスフックでボタン押下状態（`lbuttonDown`/`rbuttonDown`）を追跡し、
  設定されたトリガー（左右同時押し等）を検知する。トリガー発動時はUPイベントを抑制して誤操作を防止する
- UP抑制フラグ: `suppressNextLButtonUp`/`suppressNextRButtonUp`/`suppressKeyUpVK`により、
  トリガー発動後の不要なUPイベントをフック内で消費する。
  フックコールバック内でUPを消費しないと、トリガー操作の直後にボタンクリックやキー入力として誤検知される

## 環境変数の自動リロード

`EnvironmentRefresher` (`Win32/`) がレジストリから環境変数を再読込し、
現プロセスの環境ブロックを差分更新する。

`DummyForm.WndProc` が `WM_SETTINGCHANGE` (`lParam == "Environment"`) を受ける。

500msのデバウンスを経て `EnvironmentRefresher.Refresh()` を呼ぶ。
その後、`ReplaceEnvList` を `CommandList` と `SchedulerData` に背景スレッドで再適用する。

マージ規則はExplorer互換。

`HKLM\Session Manager\Environment` と `HKCU\Environment` を統合する。
`Path` / `PATHEXT` / `LIBPATH` / `OS2LIBPATH` のみシステム + ユーザーを `;` で連結する。
それ以外はユーザー変数がシステム変数を上書きする。
`REG_EXPAND_SZ` は現プロセス環境ベースで展開する。

子プロセスへの伝搬は追加実装不要。

`ShellExecuteEx` は呼び出し元プロセスの環境ブロックを継承する。
`Environment.SetEnvironmentVariable` で更新すれば、以降の起動プロセスへ新値が反映される。

### ReplaceEnvListの挙動に由来する制約

`ReplaceEnvList` は値→`%VAR%` 形式への片方向圧縮で、元の生文字列を保持しない。
そのため以下の非対称性がある。

- 値変更 (`JAVA_HOME` のパス差し替え等): 表示は変わらない。ただし `Command.Execute` 内の
  `Environment.ExpandEnvironmentVariables` が新値を使うため、子プロセスは新値で起動する
- 変数追加: 新規に置換可能となったコマンドは `%VAR%` 形式に圧縮される
- 変数削除: 一度 `%VAR%` 形式で保存されたコマンドは復元不能（再起動しても同じ）

### ReplaceEnvListの排他は静的

`ReplaceEnvList` は呼び出しごとに新規インスタンスが作られるため、ロックは
`static` で保持している。
これにより `MainForm.ApplyConfig` の背景スレッドと環境変数変更の背景スレッドが、
同じ `Command` や `SchedulerTask` を同時に書き換える事故を防いでいる。

## 設計上の制約・選択

- cfg/dat分離の徹底: `*.cfg`は設定変更時のみ書き換え、`*.dat`は頻繁に書き換わるデータとする。両者を混在させない
- STAスレッド必須: Shell API呼び出しはすべてSTAスレッドから行う。ThreadPool/MTAからの呼び出しは禁止する
- アイコンローダー8本固定: SHGetFileInfoの安定性のため動的調整しない
- ReplaceEnvListの排他はstatic: 異なるインスタンス間でも`Command`/`SchedulerTask`への書き込みが並行しないように直列化するため
- TopMost親からのダイアログ表示: 親フォームが`TopMost=true`の場合、
  子モーダルダイアログも`TopMost`に揃えないとz-order再評価時に親の裏に回る。
  すべての子Formの`ShowDialog`は`FormsHelper.ShowDialogOver`拡張メソッド経由で呼ぶこと。
  ネストしたダイアログでも親から自動で伝播する
