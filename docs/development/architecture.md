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

CommandLauncherPresenter / ButtonLauncherPresenter / SchedulerPresenterがUIロジックを担当する。
WinFormsのフォームクラスはイベントハンドラとコントロール操作のみを持ち、判断ロジックはPresenterに委譲する。
これにより、UIロジックをWinFormsから分離してテスト可能にしている。

### ConfigStore継承による永続化

Config、CommandList、ButtonLauncherData（Data）はすべてConfigStoreを継承し、XMLシリアライズで永続化される。
ConfigStoreは原子的なファイル保存（一時ファイルに書き込み後File.Moveで置換）を提供し、
保存中のクラッシュによるデータ破損を防止する。

### ApplicationHostFormによるIPCハブ

ApplicationHostFormは不可視の常駐フォームで、アプリケーション全体のハブとして機能する。
WM_APPMSGによるプロセス間通信（/close、/restart等のコマンドライン引数の処理）を受け付ける。
加えて、子フォーム（CommandLauncherForm、ButtonLauncherForm）のライフサイクルを管理する。
WinFormsのメッセージループを維持するために常駐フォームが必要であり、
CommandLauncherFormは表示/非表示を繰り返すため、この役割を分離している。
また、スケジューラーのタイマー（30秒間隔）を管理し、スケジュール条件に合致したタスクの自動実行も制御する。

### スケジューラータスクの種類

スケジューラーはファイル実行に加え、メッセージ表示タスクをサポートする。

| 種類       | 説明                                                        |
| ---------- | ----------------------------------------------------------- |
| Execute    | ShellExecuteExでプログラムを起動する                        |
| BalloonTip | タスクトレイのバルーン通知でメッセージを表示する (自動消去) |
| MessageBox | `NotificationForm`をモーダル表示する (OKボタンで手動消去)   |

Core層 (SchedulerPresenter) はUI依存を持たない。
BalloonTip/MessageBoxの表示はデリゲート経由でUI層 (ApplicationHostForm) に委譲する。
MessageBoxは`Invoke`（同期呼び出し）でダイアログが閉じるまで後続タスクをブロックする。
BalloonTipはBeginInvoke（非同期）で実行する。

### 通知ダイアログの追跡とowner選定

実装上の不変条件は `.claude/rules/notification-dialog.md` にまとめている。

## フック管理

`HookManager`がグローバルキーボード/マウスフックの状態管理を一元的に担当する。
キーボードフックは`SetWindowsHookEx`で登録し、KEYDOWN時に仮想キーコードと修飾キーを照合してホットキーを検知する。
マウスフックはボタン押下状態（`lbuttonDown`/`rbuttonDown`）を追跡し、設定されたトリガー（左右同時押し等）を検知する。
検知時は`BeginInvoke`でUIスレッドへShowHideメッセージを送信する。

コールバック内で守るべき実装上の不変条件（即時return・UP抑制フラグ更新など）は
リポジトリ内の`.claude/rules/win32-interop.md`にまとめている。

## 環境変数の自動リロード

`EnvironmentRefresher`（`Win32/`）がレジストリから環境変数を再読込し、現プロセスの環境ブロックを差分更新する。
`ApplicationHostForm.WndProc`が`WM_SETTINGCHANGE`（`lParam == "Environment"`）を受信する。
500msのデバウンスを経て`EnvironmentRefresher.Refresh()`を呼び、
その後`ReplaceEnvList`を`CommandList`と`SchedulerData`に背景スレッドで再適用する。
ReplaceEnvListに関する挙動上の注意はリポジトリ内の`.claude/rules/persistence.md`にまとめている。

マージ規則はExplorer互換である。
`HKLM\Session Manager\Environment`と`HKCU\Environment`を統合し、
`Path`／`PATHEXT`／`LIBPATH`／`OS2LIBPATH`のみシステム + ユーザーを`;`で連結する。
それ以外はユーザー変数がシステム変数を上書きする。
`REG_EXPAND_SZ`は現プロセス環境ベースで展開する。

子プロセスへの伝搬は追加実装不要である。
`ShellExecuteEx`は呼び出し元プロセスの環境ブロックを継承するため、
`Environment.SetEnvironmentVariable`で更新すれば以降の起動プロセスへ新値が反映される。
