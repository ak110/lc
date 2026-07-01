---
paths:
  - "src/**/*.cs"
---

# スレッディング・STA関連のルール

## スレッディングモデル

| スレッド                         | 用途                                      | 備考                                       |
| -------------------------------- | ----------------------------------------- | ------------------------------------------ |
| UIスレッド (STA)                 | WinFormsメッセージループ、全UI操作        | `Application.Run(ApplicationHostForm)`     |
| コマンド実行スレッド (STA)       | `Command.Execute()`の実行                 | `CommandLauncherForm.ExecuteCommand`で生成 |
| ディレクトリ展開スレッド (STA)   | `Command.OpenDirectory()`の実行           | `CommandLauncherForm.OpenDirectory`で生成  |
| アイコン読込スレッド (STAx8)     | `AsyncIconLoader`による非同期アイコン取得 | 固定8本STAワーカー + リトライ（最大2回）   |
| 環境変数置換スレッド             | `ReplaceEnvList`のコマンド名置換          | `CommandLauncherForm.ApplyConfig`で生成    |
| スケジューラー実行スレッド (STA) | `SchedulerPresenter.ExecuteItemTasks`     | タイマーTick時に生成、アイテムごとに1本    |
| フックコールバック               | キーボード/マウスフックのイベント通知     | `BeginInvoke`でUIスレッドへディスパッチ    |

## STAスレッド制約

Shell API（`ShellExecuteEx`・`SHGetFileInfo`等）はCOMのSTA（Single-Threaded Apartment）を前提とするため、
専用STAスレッド経由で呼ぶ。
`Task.Run`（ThreadPool/MTA）からの呼び出しは禁止する。
新規`Thread`を生成する場合は、生成直後に`SetApartmentState(ApartmentState.STA)`を呼ぶ。
コマンド実行・ディレクトリ展開・アイコン読込・スケジューラータスク実行はすべて専用STAスレッドで動かす。
表示範囲が限定される用途に限り「Shell APIのUIスレッド同期呼び出し例外」節でUIスレッド同期呼び出しを許容する。

## Shell APIのUIスレッド同期呼び出し例外

前節「STAスレッド制約」の例外として、次の用途に限りUIスレッド上での同期呼び出しを許容する。
Shellコンテキストメニュー呼び出し（`ShellContextMenuInvoker`）と、
フォルダポップアップメニューのアイコン取得（`FolderPopupMenuBuilder`）を対象とする。
UIスレッドはSTAアパートメントのためShell APIを呼び出せる。
本例外の適用範囲は単一メニュー1階層分の表示処理に限定し、それ以外は専用STAスレッド経由とする。
`AsyncIconLoader`が担うグリッド全体のアイコン取得は引き続き専用STAワーカー8本を維持する。

## アイコンローダーの並行度

`AsyncIconLoader`のワーカー数は8本固定とする。
`SHGetFileInfo`は高並行度で不安定になるため、`Environment.ProcessorCount`等の動的値は使わない。
`ButtonLauncherForm.Handle`の作成は`iconLoader.Load`より前に行う。
Handle未作成時に`IconLoaded`が届くと、`BeginInvoke`が失敗してアイコンが破棄される。
完了時の再描画は`btn.Parent?.Invalidate(true)`を使う。
`btn.Invalidate()`では非選択タブのボタンが再描画されない。
リトライ上限は2回とする。
