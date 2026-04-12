# カスタム指示（プロジェクト固有）

## 開発手順

- dotnet, node, pnpmなどはmise経由で実行する
- ドキュメントのみの変更（`*.md`や`docs/**`の更新）をコミットする場合、事前の手動`mise run test`は省略してよい。
  `git commit`時点でpre-commitフックが`pyfltr fast`を自動実行するため、Markdownのtextlint/markdownlint-cli2/prettierは確実にかかる
- コードに手を入れた変更では、失敗の早期検出のため従来どおり事前に`mise run test`を回すことを推奨する

### Claude Code向けコミット前検証

Claude Codeがコミット前に検証する際は、`mise run test`の代わりに以下を実行する。JSON Lines出力によりLLMがツール別診断を効率的に解釈できる。

```bash
uvx pyfltr run --output-format=jsonl
```

人間の開発者は従来どおり`mise run test`を使用する。

## リリース

- リリースはGitHub ActionsのReleaseワークフローで行う（詳細はdocs/development/development.md）
- バグフィックス/小規模改善 →「バグフィックス」、新機能追加 →「マイナーバージョンアップ」を選択する
- `git commit --amend` はリリースのバージョンバンプコミットと混ざるリスクがあるため、push済みコミットには使わない

## 設計上の注意点

- `*.cfg`（設定）と`*.dat`（ランタイムデータ）の分離を徹底する。頻繁に更新されるデータは`*.dat`へ
- WinForms Designer.csのマルチバイト文字を含むテーブル等はmarkdownlint (MD060) に注意
- カタカナ語の長音:「スケジューラー」「ランチャー」など末尾に長音符を付ける

## 関連ドキュメント

- @README.md
- @docs/development/architecture.md
- @docs/development/development.md
