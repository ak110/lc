# CLAUDE.md: lc

## 開発手順

- dotnet, node, pnpmなどはmise経由で実行する
- リリース手順: [docs/development/development.md](docs/development/development.md) 参照
- コミット前の検証方法: `uvx pyfltr run-for-agent`
  - ドキュメントなどのみの変更の場合は省略可（pre-commitで実行されるため）
  - 修正後の再実行時は、対象ファイルや対象ツールを必要に応じて絞って実行する（最終検証はCIに委ねる前提）
    - 例: `uvx pyfltr run-for-agent --commands=dotnet-build,dotnet-test path/to/file`

### miseタスク一覧

| コマンド          | 説明                                                         |
| ----------------- | ------------------------------------------------------------ |
| `mise run setup`  | 開発環境のセットアップ（dotnet tool restore / pnpm install） |
| `mise run format` | フォーマット + 軽量lint（開発時の手動実行用。自動修正あり）  |
| `mise run test`   | 全チェック実行（これを通過すればコミット可能）               |
| `mise run build`  | リリースビルド                                               |
| `mise run clean`  | ビルド成果物の削除                                           |
| `mise run update` | 依存パッケージの更新                                         |
| `mise run docs`   | ドキュメントのローカルプレビュー（VitePress dev server）     |

## 注意点

- `*.cfg`（設定）と`*.dat`（ランタイムデータ）の分離を徹底する。頻繁に更新されるデータは`*.dat`へ
- WinForms Designer.csのマルチバイト文字を含むテーブル等はmarkdownlint (MD060) に注意
