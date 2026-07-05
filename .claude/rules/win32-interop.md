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
コールバック内で例外情報を`DiagnosticLog`へ記録する場合は`Task.Run`で非同期化し、
コールバック本体は即時returnする（`DiagnosticLog`の書き込みは同期I/Oのため）。
ホットキー／マウストリガー検知時のUP抑制フラグ
（`suppressNextLButtonUp`・`suppressNextRButtonUp`・`suppressKeyUpVK`）の更新を漏らさない。

## モーダルダイアログのTopMost伝播

親フォームが`TopMost=true`の場合、子モーダルダイアログも`TopMost`に揃えないと、z-order再評価時に親の裏に回る。
すべての子Formの`ShowDialog`は`FormsHelper.ShowDialogOver`拡張メソッド経由で呼ぶ。
ネストしたダイアログでも親から自動で伝播する。
`ShowDialogOver`は`Form`派生のみを対象とする。
`FontDialog`等の`CommonDialog`と`MessageBox`は`Form`派生でないため対象外とし、`ShowDialog(owner)`を直接使う。

## Shell IContextMenuの呼び出しとメッセージ転送

`IShellFolder.GetUIObjectOf`で取得した`IContextMenu`を`TrackPopupMenuEx`で表示する。
表示中は次のメッセージを転送する。
転送先は`IContextMenu3.HandleMenuMsg2`（存在すれば）または`IContextMenu2.HandleMenuMsg`とする。
対象メッセージは`WM_INITMENUPOPUP`・`WM_MENUCHAR`・`WM_DRAWITEM`・`WM_MEASUREITEM`とする。
転送しないとサブメニュー展開・オーナードロー項目・アクセラレータキーが機能しない。
転送は`ShellContextMenuInvoker`が内部で保持する`NativeWindow`派生で行う。

当該`NativeWindow`派生（`MenuMessageForwarder`）は`AssignHandle(ownerHwnd)`で`ownerHwnd`をサブクラス化する。
同一`ownerHwnd`に対する多重生成（メニュー表示中の再帰的なShellモーダルUI呼び出し等）は禁止する。
生存区間が重なると`AssignHandle`が保存する旧WNDPROCのチェーンが破損する。

`TrackPopupMenuEx`のフラグには`TPM_RIGHTBUTTON`を付与しない。
呼び出し元の右クリック（WM_RBUTTONUP直後の表示）で右ボタン残留が
最初の項目選択として認識され、ユーザー未操作のまま`InvokeCommand`が実行される。
`TPM_RETURNCMD`のみを指定し左ボタンでの項目選択に限定する。

## ContextMenuStrip項目からShellモーダルUIを呼ぶ場合の親メニュークローズ

`ContextMenuStrip`の項目の`MouseUp`イベント内からShellモーダルUIを発火する場合の対処を定める。
項目イベントで用いる`Click`と`MouseUp`の使い分けは「ContextMenuStrip項目のマウスボタン別ハンドラ設計」節に従う。
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

本節はハング系の予防策である。
AccessViolation等のCLR corrupted-state exceptionによる即クラッシュには本節の対策のみでは不十分であり、
「AccessViolationクラッシュの診断」節の運用と併用する。

## ContextMenuStrip項目のマウスボタン別ハンドラ設計

`ContextMenuStrip`の項目で左クリックと右クリックの動作を分岐する場合、左右分岐は`MouseUp`ハンドラ単独で判定する。
`MouseUp`ハンドラ内で`e.Button == MouseButtons.Left`と`e.Button == MouseButtons.Right`の分岐に統一する。
`ToolStripMenuItem`個別の`Click`ハンドラは登録しない。
.NET 10 WinFormsの`ToolStripMenuItem`はマウス右クリック時にも`Click`イベントが発火する経路
（`MouseUp`より先に発火する場合もある）を持つ。
`Click`ハンドラを併用すると右クリック時に左クリック相当の動作が実行され、二重動作を招く。
`item.Click -= clickHandler`で1回目発火後に解除する形でも初回発火自体は防げないため採用しない。
キーボードEnter操作で左クリック相当の動作を発火させたい場合は、
`ContextMenuStrip.KeyDown`イベントで`Keys.Return`または`Keys.Enter`を検知する。
検知後は選択中の項目（`ToolStripMenuItem.Selected`が真の項目）へ動作を発火する。
Shellモーダル呼び出し前段で親メニューを閉じる規約は
「ContextMenuStrip項目からShellモーダルUIを呼ぶ場合の親メニュークローズ」節と併せて適用する。

## ContextMenuStrip項目イベントでのApplication.DoEvents非使用

前節「ContextMenuStrip項目のマウスボタン別ハンドラ設計」で登録する項目イベント内では
`Application.DoEvents()`を呼び出さない。
Shell呼び出しと親メニュー`Closed`イベントのFIFO順序は
「ContextMenuStrip項目からShellモーダルUIを呼ぶ場合の親メニュークローズ」節が定める。
追加のメッセージポンプ進行は不要である。
`Application.DoEvents()`は副次的なマウス・キーボードイベントも処理する副作用があり、
前節の動作分岐と衝突する。

## AccessViolationクラッシュの診断

`AccessViolationException`（Windowsイベントログの例外コード0xc0000005）はCLR corrupted-state exceptionに属する。
`AppDomain.CurrentDomain.UnhandledException`および`Application.ThreadException`のいずれでも捕捉されずプロセスが即終了する。
`try/catch (Exception)`もすり抜けるため、既存の`ErrorReporter`経由の通知は機能しない。
AV再発時の診断のため、永続ログAPI`Launcher.Infrastructure.DiagnosticLog`の`Debug`を用いる。
Shell/Win32境界の疑わしい呼び出しへ`before/after`ペアで一時的に配置しAV発生ステージを特定する。
原因特定・修正が完了した時点で当該ペアは削除する。
`DiagnosticLog`の実装仕様・レベル使い分け・パス出力禁止などは`.claude/rules/logging.md`に従う。
`Program.cs`の`UnhandledException`ハンドラは捕捉できた例外を`DiagnosticLog.Error`で併記する。
`AccessViolationException`本体は捕捉せずfail-fastでプロセスを終了させる（隠すと診断価値を失う）。

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

## P/Invoke失敗検知の作法

P/Invoke宣言に対する失敗検知に`new Win32Exception()`（引数なしコンストラクタ）を用いる場合、
`[DllImport(..., SetLastError = true)]`を必ず付与する。
付与しないと`GetLastError`は当該P/Invoke失敗以外の値を返し得る。
`Win32Exception`を構築するタイミングはP/Invoke戻り値評価の直後とする。
他のP/Invoke呼び出しを挟まない位置で構築し、`GetLastError`のスナップショットを保存する。
本節は「ShellExecuteEx失敗時のhProcess解放」節と同一原則を全P/Invoke宣言へ拡張する規範である。

## ShellExecuteEx失敗時のhProcess解放

`SEE_MASK_NOCLOSEPROCESS`を設定した`ShellExecuteEx`は、
`false`を返す失敗経路でも`hProcess`が非ゼロで返る場合がある。
失敗判定時は`Win32Exception`を先に構築して`GetLastError`のスナップショットを保存する。
その後`hProcess != IntPtr.Zero`なら`CloseHandle`する。
`CloseHandle`は`GetLastError`を上書きし得るため、順序を守る。

## ContextMenuStripのClosedイベントでのDispose遅延

`ContextMenuStrip`の`Closed`イベント内で`Dispose`を同期実行すると、
Closed発火後にWinForms内部の後始末処理（`ToolStripManager`追跡解除など）が続く。
その処理がDisposed済みインスタンスへアクセスして`ObjectDisposedException`が発生する。
`Closed`イベント内で`Dispose`する場合は`BeginInvoke`で次のメッセージループへ遅延する。
遅延の呼び出し先はメニュー自身ではなく、生存中の`Control`（親フォームなど）の`BeginInvoke`を使う。
メニュー自身の`BeginInvoke`はハンドル未作成状態で失敗する場合がある。
