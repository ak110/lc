---
paths:
  - "src/**/*.cs"
---

# Win32 API・WinForms連携のルール

## Win32フックコールバック

`SetWindowsHookEx`のコールバックはシステム全体の入力をブロックしうるため、即時に返す。
コールバック内で`MessageBox.Show`・`Thread.Sleep`・同期I/O・長時間ループは禁止する。
UI操作は`BeginInvoke`（非同期）でUIスレッドへディスパッチする。
`Invoke`（同期）はデッドロックの恐れがある。
ホットキー／マウストリガー検知時のUP抑制フラグ
（`suppressNextLButtonUp`・`suppressNextRButtonUp`・`suppressKeyUpVK`）の更新を漏らさない。

## モーダルダイアログのTopMost伝播

親フォームが`TopMost=true`の場合、子モーダルダイアログも`TopMost`に揃えないと、z-order再評価時に親の裏に回る。
すべての子Formの`ShowDialog`は`FormsHelper.ShowDialogOver`拡張メソッド経由で呼ぶ。
ネストしたダイアログでも親から自動で伝播する。
`ShowDialogOver`は`Form`派生のみを対象とする。
`FontDialog`等の`CommonDialog`と`MessageBox`は`Form`派生でないため対象外とし、`ShowDialog(owner)`を直接使う。

## Shell IContextMenuのメッセージ転送

`IShellFolder.GetUIObjectOf`で取得した`IContextMenu`を`TrackPopupMenuEx`で表示する。
表示中は次のメッセージを転送する。
転送先は`IContextMenu3.HandleMenuMsg2`（存在すれば）または`IContextMenu2.HandleMenuMsg`とする。
対象メッセージは`WM_INITMENUPOPUP`・`WM_MENUCHAR`・`WM_DRAWITEM`・`WM_MEASUREITEM`とする。
転送しないとサブメニュー展開・オーナードロー項目・アクセラレータキーが機能しない。
転送は`ShellContextMenuInvoker`が内部で保持する`NativeWindow`派生で行う。

## ContextMenuStrip項目からShellモーダルUIを呼ぶ場合の親メニュークローズ

`ContextMenuStrip`の項目イベント（`Click`・`MouseUp`）内からShellモーダルUIを発火する場合の対処を定める。
対象のShellモーダルUIは`ShellContextMenuInvoker.Show`など`TrackPopupMenuEx`ベースの呼び出しである。
項目イベントハンドラでは先に親`ContextMenuStrip`を`Close(ToolStripDropDownCloseReason.ItemClicked)`で閉じる。
右クリック時は`ContextMenuStrip`が自動的に閉じない。
閉じずに`TrackPopupMenuEx`を呼ぶと二重のメニューモーダルループが発生する。
結果としてハング、またはShell拡張のCOM例外による異常終了が発生する。
Shell呼び出しは親メニューを閉じた後に`UiThreadDispatcher.SafeBeginInvoke`へポストする。
FIFOで並ぶメッセージキュー上で、親メニューのDispose遅延（`Closed`イベント内でポストされる）が先に処理される。
続いてShell呼び出しが実行される。
左クリック時は`ContextMenuStrip`が自動的に閉じるため`Close`呼び出しは冪等となる。
それでも意図明示のため呼び出しを省略しない。

## PIDL解放規約

`SHParseDisplayName`は絶対PIDLを新規割り当てる。呼び出し側で`Marshal.FreeCoTaskMem`する。
`SHBindToParent`の`ppidlLast`は絶対PIDL内部を指す非所有ポインタである。
独立解放するとダブルフリーになるため解放しない。
`ShellNamespaceHelper.BindToParent`はこの規約に従い、`fullPidl`のみ呼び出し側で解放させる。

## IUnknown生ポインタとRCWの同時保持

`IShellFolder.GetUIObjectOf`等は`out ppv`で生`IUnknown`ポインタを返す。
`Marshal.GetObjectForIUnknown(ppv)`でRCWを取得したら、
対応する`Marshal.Release(ppv)`は`try/finally`の`finally`側で実行する。
`GetObjectForIUnknown`が例外を送出した場合でも`ppv`が解放される構造にする。
取得したRCWは呼び出し側の`finally`ブロックで`Marshal.ReleaseComObject`により解放する。

## ShellExecuteEx失敗時のhProcess解放

`SEE_MASK_NOCLOSEPROCESS`を設定した`ShellExecuteEx`は、
`false`を返す失敗経路でも`hProcess`が非ゼロで返る場合がある。
失敗判定時は`Win32Exception`を先に構築して`GetLastError`のスナップショットを保存する。
その後`hProcess != IntPtr.Zero`なら`CloseHandle`する。
`CloseHandle`は`GetLastError`を上書きし得るため、順序を守る。

## ContextMenuStrip の Closed イベントでの Dispose 遅延

`ContextMenuStrip`の`Closed`イベント内で`Dispose`を同期実行すると、
Closed発火後にWinForms内部の後始末処理（`ToolStripManager`追跡解除など）が続く。
その処理がDisposed済みインスタンスへアクセスして`ObjectDisposedException`が発生する。
`Closed`イベント内で`Dispose`する場合は`BeginInvoke`で次のメッセージループへ遅延する。
遅延の呼び出し先はメニュー自身ではなく、生存中の`Control`（親フォームなど）の`BeginInvoke`を使う。
メニュー自身の`BeginInvoke`はハンドル未作成状態で失敗する場合がある。
