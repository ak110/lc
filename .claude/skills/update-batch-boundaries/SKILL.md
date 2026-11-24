---
verified-digest: deb2f8c64223794723ff02b0ec44493a6625ea07473d60b7191d993e9b677062
name: update-batch-boundaries
description: 自己更新バッチの配布ファイルの退避・コピー・復旧を変えた時に、失敗と中断の位置を網羅するWindows上の検査を実行する。
verifies:
  - src/Launcher/Updater/UpdatePerformer.cs
---

# 自己更新バッチの復旧境界を確かめる

配布ファイルの更新に成功したらすべての対象が新版になり、復旧に成功したら更新前の内容と有無が戻る。
途中で中断したり復旧に失敗したりした場合でも、完全な新版になっているか、旧版の各ファイルがアプリのディレクトリか同じ相対パスの退避先に残る。
設定とメモはどの場合も変わらない。破れると、更新前のインストールを復旧できないか、利用者のデータを失う。

## 対象と上限

- `PerformUpdateAsync` が展開し、`GetRelativeFiles` が列挙した配布ファイルを `GenerateBatchScript` へ渡す。テストは同じ形式の一覧を直接渡し、生成したバッチを隔離ディレクトリで実行する。全通りを試すときは待機ループは実行せず、ファイル操作から始める。ダウンロードやZIPの展開・ファイル列挙自体は実行しない。
- ルートとサブディレクトリの2ファイルについて、その順序と更新前の有無を全列挙する。退避・コピーのうち最初の失敗になる各位置と、その後に到達する復旧の失敗の部分集合を `FailSteps` で注入する。注入したコマンドは状態を変更する前に失敗する。復旧時の追加ファイルの削除失敗は、生成したバッチ内の該当する削除命令だけを空操作に置き換え、直後の残存判定を実行する。
- 退避・コピー・復旧の移動、ディレクトリの作成、削除の各コマンドを1手順とする。各操作の直前で停止する実行と最後まで進む実行で、残った対象・退避・設定・メモの内容を調べる。元々なかった配布ファイルは旧版に戻す際の削除対象として扱う。
- 正規のリリースZIPには設定・メモを入れない（`.github/workflows/release.yaml` の配布物の組み立てを参照）。任意のZIPの内容の検証ではない。単一の更新、衝突しない作業領域、通常ファイル、開始時に `COPY_STARTED` が未定義の環境を前提とする。
- 個々のファイル操作の途中での電源断、親プロセスが存続する場合の待機の停止性は扱わない。

## 再実行

Windowsの一時checkoutで、アプリを起動せずに実行する。PowerShellで次を実行する。

```powershell
$env:LC_CHECK_UPDATE_BOUNDARIES = '1'
try {
    mise exec -- uvx --exclude-newer-package pyfltr=false pyfltr run --commands=dotnet-test '--dotnet-test-args=--filter FullyQualifiedName~UpdatePerformerTests'
} finally {
    Remove-Item Env:LC_CHECK_UPDATE_BOUNDARIES
}
```

失敗した場合は順序、もとの有無、注入した失敗、停止位置を出力で確認する。生成したバッチで到達する操作列なら、製品コードの欠陥として扱う。到達しない操作列ならテストを直す。
対象コードを変更したら、新しいファイル操作と分岐が探索に含まれることを確かめてから再実行する。検証後にだけ、`uvx --exclude-newer-package pyfltr=false pyfltr verification-sync --update .claude/skills/update-batch-boundaries/SKILL.md` で記録を更新する。
