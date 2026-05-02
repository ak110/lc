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
