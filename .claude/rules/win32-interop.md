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

## PIDL解放規約

`SHParseDisplayName`は絶対PIDLを新規割り当てる。呼び出し側で`Marshal.FreeCoTaskMem`する。
`SHBindToParent`の`ppidlLast`は絶対PIDL内部を指す非所有ポインタである。
独立解放するとダブルフリーになるため解放しない。
`ShellNamespaceHelper.BindToParent`はこの規約に従い、`fullPidl`のみ呼び出し側で解放させる。
