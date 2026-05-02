# スレッディング・STA関連のルール

## STAスレッド制約

Shell API（`ShellExecuteEx`・`SHGetFileInfo`等）はCOMのSTA（Single-Threaded Apartment）を前提とするため、
専用STAスレッド経由で呼ぶ。
`Task.Run`（ThreadPool/MTA）からの呼び出しは禁止する。
新規`Thread`を生成する場合は、生成直後に`SetApartmentState(ApartmentState.STA)`を呼ぶ。
コマンド実行・ディレクトリ展開・アイコン読込・スケジューラータスク実行はすべて専用STAスレッドで動かす。

## アイコンローダーの並行度

`AsyncIconLoader`のワーカー数は8本固定とする。
`SHGetFileInfo`は高並行度で不安定になるため、`Environment.ProcessorCount`等の動的値は使わない。
`ButtonLauncherForm.Handle`の作成は`iconLoader.Load`より前に行う。
Handle未作成時に`IconLoaded`が届くと、`BeginInvoke`が失敗してアイコンが破棄される。
完了時の再描画は`btn.Parent?.Invalidate(true)`を使う。
`btn.Invalidate()`では非選択タブのボタンが再描画されない。
リトライ上限は2回とする。
