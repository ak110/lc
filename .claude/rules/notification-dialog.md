# 通知ダイアログのルール

## 非同期通知ダイアログの追跡

`ApplicationHostForm`は現在表示中の非同期通知（`NotificationForm`）を`activeNotifications`リストで追跡する。
スケジューラーのMessageBoxタスクは`Invoke`経由で`NotificationForm.ShowDialog`を呼び出し、
スケジューラーSTAスレッドは`ShowDialog`が戻るまで自然にブロックされる。
`ShowDialog`のネストメッセージループ内でも`ApplicationHostForm.WndProc`は動作するため、
表示中でも`WM_APPMSG_SHOWHIDE`を受信しホットキー操作を処理できる。

ホットキー押下で呼び出される`ApplicationHostForm.ShowHide()`は、通知が追跡中のときはCommandLauncherFormを非表示化しない。
代わりに通知Formを`Activate()`+`BringToFront()`で最前面化する。
またCommandLauncherFormの`WindowHideNoActive`によるauto-hideも同じ条件でスキップする。
具体的には`CommandLauncherForm_Deactivate`/`CommandLauncherForm_Leave`内で判定する。
`ApplicationHostForm.HasActiveNotifications`が真のときは処理を中止する。
通知をActivateした直後の`CommandLauncherForm_Deactivate`でCommandLauncherFormが再び隠れる事故を防ぐためである。

BalloonTipはOSのトレイ通知であり自動消去されるため、この追跡対象には含めない。

## 通知ダイアログのowner選定とフォーカス復元

`ApplicationHostForm.GetVisibleOwner()`はowner候補として
CommandLauncherForm / ButtonLauncherForm / `Form.ActiveForm`の順で返す。
`Form.ActiveForm`フォールバックは、CommandLauncherFormが非表示でConfigForm等のモーダルダイアログが開いている最中に
スケジューラーが発火したケースをカバーする。
この場合、NotificationFormはモーダルダイアログをownerとして`ShowDialog`するため、
閉じたときはWinFormsの標準挙動でモーダルダイアログへフォーカスが戻る。

一方、launcher内に表示フォームが一切無い状態（`HideFirst=true`での起動直後やCommandLauncherForm hide後）で
発火したときはownerが`null`になる。
`Form.ShowDialog(null)`は閉じるときのフォーカス復元先を持たないため、
Windowsがz-order上の任意ウィンドウを前面化してしまう。
これを避けるため、MessageBoxハンドラでは`NotificationForm`を表示する直前に
`WindowHelper.GetForegroundWindowHandle()`で前景HWNDを記録する。
`ShowDialog`終了後に`GetVisibleOwner()`が依然として`null`のときだけ
`WindowHelper.RestoreForegroundWindow()`で記録したHWNDに前景を戻す。

`RestoreForegroundWindow`は自プロセス所有のHWNDには何もしない。
launcher内へのフォーカス復帰はWinFormsの標準挙動に任せる責務分担にしている。
