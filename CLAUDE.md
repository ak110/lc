# CLAUDE.md: lc

## 開発手順

- dotnet, node, pnpmなどはmise経由で実行する
- コミット前の検証方法: `uvx pyfltr run-for-agent`
  - ドキュメントなどのみの変更の場合は省略可（pre-commitで実行されるため）

## 注意点

- `*.cfg`（設定）と`*.dat`（ランタイムデータ）の分離を徹底する。頻繁に更新されるデータは`*.dat`へ
- WinForms Designer.csのマルチバイト文字を含むテーブル等はmarkdownlint (MD060) に注意
