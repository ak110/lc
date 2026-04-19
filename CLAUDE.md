# CLAUDE.md: lc

## 開発手順

- dotnet, node, pnpmなどはmise経由で実行する
- コミット前の検証方法: `uv run pyfltr run-for-agent`
  - ドキュメントなどのみの変更の場合は省略可（pre-commitで実行されるため）
  - テストコードの単体実行なども極力 `uv run pyfltr run-for-agent <path>` を使う（pytestを直接呼び出さない）
    - 詳細な情報などが必要な場合に限り `uv run pytest -vv <path>` などを使用
  - 修正後の再実行時は、対象ファイルや対象ツールを必要に応じて絞って実行する（最終検証はCIに委ねる前提）
    - 例: `pyfltr run-for-agent --commands=mypy,ruff-check path/to/file`

## 注意点

- `*.cfg`（設定）と`*.dat`（ランタイムデータ）の分離を徹底する。頻繁に更新されるデータは`*.dat`へ
- WinForms Designer.csのマルチバイト文字を含むテーブル等はmarkdownlint (MD060) に注意
